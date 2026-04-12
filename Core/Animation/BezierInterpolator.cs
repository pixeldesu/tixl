using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace T3.Core.Animation;

/// <summary>
/// Evaluates a 2D cubic Bezier curve segment with root finding.
/// Used for Tangent-mode keyframes with custom tension (weighted handles).
/// For Smooth/Cubic/Horizontal modes, <see cref="SplineInterpolator"/> is used instead
/// (mathematically identical at tension=1.0, but faster without root finding).
/// </summary>
internal static class BezierInterpolator
{
    /// <summary>
    /// Evaluates the curve value at time <paramref name="u"/> for a segment
    /// defined by two keyframes with Bezier control points.
    /// </summary>
    public static double Interpolate(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, double u)
    {
        var keyA = a.Value;
        var keyB = b.Value;
        var segmentWidth = b.Key - a.Key;

        if (segmentWidth <= 0)
            return keyA.Value;

        // Compute Hermite tangent magnitudes (same as SplineInterpolator)
        var slopeA = SlopFromAngle(keyA.OutTangentAngle);
        var slopeB = SlopFromAngle(keyB.InTangentAngle);
        var m0 = slopeA * segmentWidth * keyA.TensionOut;
        var m1 = slopeB * segmentWidth * keyB.TensionIn;

        // Convert Hermite tangents to Bezier control points
        // Relationship: m = 3 * (P1 - P0) for cubic Bezier ↔ Hermite
        double p0x = a.Key, p0y = keyA.Value;
        double p1x = a.Key + segmentWidth * keyA.TensionOut / 3.0;
        double p1y = keyA.Value + m0 / 3.0;
        double p2x = b.Key - segmentWidth * keyB.TensionIn / 3.0;
        double p2y = keyB.Value - m1 / 3.0;
        double p3x = b.Key, p3y = keyB.Value;

        // Root find: solve BezierX(t) = u for parameter t
        var t = FindParameterForTime(u, p0x, p1x, p2x, p3x);

        // Evaluate Y at found parameter
        return EvalCubic(t, p0y, p1y, p2y, p3y);
    }

    /// <summary>
    /// Determines whether a segment between two keyframes requires Bezier evaluation
    /// (as opposed to faster Hermite). A segment needs Bezier when at least one endpoint
    /// has Tangent interpolation with non-default tension (Weighted=true).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SegmentNeedsBezier(VDefinition a, VDefinition b)
    {
        // Only use Bezier when explicitly weighted with non-default tension
        return (a.Weighted && a.OutInterpolation == VDefinition.KeyInterpolation.Tangent
                           // ReSharper disable once CompareOfFloatsByEqualityOperator
                           && a.TensionOut != 1.0f)
               || (b.Weighted && b.InInterpolation == VDefinition.KeyInterpolation.Tangent
                              // ReSharper disable once CompareOfFloatsByEqualityOperator
                              && b.TensionIn != 1.0f);
    }

    /// <summary>
    /// Finds the Bezier parameter t where BezierX(t) = targetX.
    /// Uses Newton-Raphson with linear initial guess and bisection fallback.
    /// </summary>
    private static double FindParameterForTime(double targetX, double p0x, double p1x, double p2x, double p3x)
    {
        // Linear initial guess
        var t = (targetX - p0x) / (p3x - p0x);
        t = Math.Clamp(t, 0.0, 1.0);

        // Newton-Raphson iterations
        for (var i = 0; i < MaxNewtonIterations; i++)
        {
            var x = EvalCubic(t, p0x, p1x, p2x, p3x);
            var residual = x - targetX;

            if (Math.Abs(residual) < Tolerance)
                return t;

            var dx = EvalCubicDerivative(t, p0x, p1x, p2x, p3x);

            if (Math.Abs(dx) < 1e-12)
                break; // Derivative too small, fall through to bisection

            var newT = t - residual / dx;

            // Clamp to valid range
            newT = Math.Clamp(newT, 0.0, 1.0);

            if (Math.Abs(newT - t) < Tolerance)
                return newT;

            t = newT;
        }

        // Bisection fallback (robust but slower)
        return BisectionFallback(targetX, p0x, p1x, p2x, p3x);
    }

    private static double BisectionFallback(double targetX, double p0x, double p1x, double p2x, double p3x)
    {
        double lo = 0, hi = 1;

        for (var i = 0; i < MaxBisectionIterations; i++)
        {
            var mid = (lo + hi) * 0.5;
            var x = EvalCubic(mid, p0x, p1x, p2x, p3x);

            if (Math.Abs(x - targetX) < Tolerance)
                return mid;

            if (x < targetX)
                lo = mid;
            else
                hi = mid;
        }

        return (lo + hi) * 0.5;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double EvalCubic(double t, double p0, double p1, double p2, double p3)
    {
        var u = 1.0 - t;
        return u * u * u * p0 + 3.0 * u * u * t * p1 + 3.0 * u * t * t * p2 + t * t * t * p3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double EvalCubicDerivative(double t, double p0, double p1, double p2, double p3)
    {
        var u = 1.0 - t;
        return 3.0 * u * u * (p1 - p0) + 6.0 * u * t * (p2 - p1) + 3.0 * t * t * (p3 - p2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double SlopFromAngle(double angle)
    {
        var slope = Math.Tan(angle);
        return Math.Abs(slope) < 1e-10 ? 0.0 : slope;
    }

    private const int MaxNewtonIterations = 8;
    private const int MaxBisectionIterations = 30;
    private const double Tolerance = 1e-8;
}
