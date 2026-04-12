using System;
using System.Collections.Generic;
using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;
using Xunit.Abstractions;

namespace Core.Tests;

/// <summary>
/// Verifies that cubic Bezier evaluation with correct conversion factors
/// produces identical results to the current Hermite evaluation.
/// This is critical for migration: existing Smooth/Cubic animations must not change shape.
/// </summary>
public class HermiteBezierComparisonTests
{
    private readonly ITestOutputHelper _output;
    public HermiteBezierComparisonTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void BezierMatchesHermite_SmoothKeys()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });

        // Get the Hermite tangent angles after auto-computation
        var keyA = curve.Table.Values[0];
        var keyB = curve.Table.Values[1];

        _output.WriteLine($"KeyA: OutAngle={keyA.OutTangentAngle:F6}, slope={Math.Tan(keyA.OutTangentAngle):F6}");
        _output.WriteLine($"KeyB: InAngle={keyB.InTangentAngle:F6}, slope={Math.Tan(keyB.InTangentAngle):F6}");

        var segmentWidth = keyB.U - keyA.U;

        // Current Hermite: m = tan(angle) * segmentWidth * tension
        var m0 = Math.Tan(keyA.OutTangentAngle) * segmentWidth * keyA.TensionOut;
        var m1 = Math.Tan(keyB.InTangentAngle) * segmentWidth * keyB.TensionIn;

        // Bezier conversion: P1 = P0 + m0/3, P2 = P3 - m1/3
        // (m is the Hermite tangent, so the Bezier handle offset in value = m/3,
        //  and in time = segmentWidth/3)
        double p0x = keyA.U, p0y = keyA.Value;
        double p1x = keyA.U + segmentWidth / 3.0;
        double p1y = keyA.Value + m0 / 3.0;
        double p2x = keyB.U - segmentWidth / 3.0;
        double p2y = keyB.Value - m1 / 3.0;
        double p3x = keyB.U, p3y = keyB.Value;

        _output.WriteLine($"Bezier: P0=({p0x:F4},{p0y:F4}) P1=({p1x:F4},{p1y:F4}) P2=({p2x:F4},{p2y:F4}) P3=({p3x:F4},{p3y:F4})");

        var maxError = 0.0;
        for (double u = 0.0; u <= 1.0; u += 0.01)
        {
            var hermiteValue = curve.GetSampledValue(u);

            // Bezier evaluation with root finding
            var t = FindBezierT(u, p0x, p1x, p2x, p3x);
            var bezierValue = EvalBezier(t, p0y, p1y, p2y, p3y);

            var error = Math.Abs(hermiteValue - bezierValue);
            maxError = Math.Max(maxError, error);

            if (error > 1e-6)
                _output.WriteLine($"  u={u:F2}: hermite={hermiteValue:F8}, bezier={bezierValue:F8}, error={error:E2}");
        }

        _output.WriteLine($"Max error: {maxError:E4}");
        Assert.True(maxError < 1e-6, $"Hermite and Bezier should match. Max error: {maxError:E4}");
    }

    [Fact]
    public void BezierMatchesHermite_AsymmetricSmooth()
    {
        // Three keys with different values — Smooth auto-tangents produce non-trivial slopes
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(0.5, new VDefinition
                                    {
                                        Value = 2.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 0.5,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });

        // Test both segments
        var maxError = CompareSegments(curve);
        _output.WriteLine($"Max error across all segments: {maxError:E4}");
        Assert.True(maxError < 1e-6, $"Max error: {maxError:E4}");
    }

    [Fact]
    public void BezierMatchesHermite_ManualTangent()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 0.6, // ~34 degrees
                                        TensionOut = 1.0f,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = 0.3,
                                        TensionIn = 1.0f,
                                    });

        var maxError = CompareSegments(curve);
        _output.WriteLine($"Max error: {maxError:E4}");
        Assert.True(maxError < 1e-6, $"Max error: {maxError:E4}");
    }

    private double CompareSegments(Curve curve)
    {
        var maxError = 0.0;
        var keys = curve.Table.Keys;
        var values = curve.Table.Values;

        for (var seg = 0; seg < curve.Table.Count - 1; seg++)
        {
            var keyA = values[seg];
            var keyB = values[seg + 1];
            var segmentWidth = keyB.U - keyA.U;

            var slopeA = SafeTan(keyA.OutTangentAngle);
            var slopeB = SafeTan(keyB.InTangentAngle);

            var m0 = slopeA * segmentWidth * keyA.TensionOut;
            var m1 = slopeB * segmentWidth * keyB.TensionIn;

            double p0x = keyA.U, p0y = keyA.Value;
            double p1x = keyA.U + segmentWidth / 3.0;
            double p1y = keyA.Value + m0 / 3.0;
            double p2x = keyB.U - segmentWidth / 3.0;
            double p2y = keyB.Value - m1 / 3.0;
            double p3x = keyB.U, p3y = keyB.Value;

            for (double u = keyA.U; u <= keyB.U; u += segmentWidth * 0.01)
            {
                var hermiteValue = curve.GetSampledValue(u);
                var t = FindBezierT(u, p0x, p1x, p2x, p3x);
                var bezierValue = EvalBezier(t, p0y, p1y, p2y, p3y);

                var error = Math.Abs(hermiteValue - bezierValue);
                maxError = Math.Max(maxError, error);
            }
        }

        return maxError;
    }

    private static double SafeTan(double angle)
    {
        var slope = Math.Tan(angle);
        return Math.Abs(slope) < 1e-10 ? 0.0 : slope;
    }

    // --- Bezier math ---

    private static double EvalBezier(double t, double p0, double p1, double p2, double p3)
    {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }

    private static double EvalBezierDerivative(double t, double p0, double p1, double p2, double p3)
    {
        var u = 1 - t;
        return 3 * u * u * (p1 - p0) + 6 * u * t * (p2 - p1) + 3 * t * t * (p3 - p2);
    }

    private static double FindBezierT(double targetX, double p0x, double p1x, double p2x, double p3x)
    {
        // Newton-Raphson
        var t = (targetX - p0x) / (p3x - p0x); // Initial guess: linear
        t = Math.Clamp(t, 0, 1);

        for (var i = 0; i < 20; i++)
        {
            var x = EvalBezier(t, p0x, p1x, p2x, p3x);
            var dx = EvalBezierDerivative(t, p0x, p1x, p2x, p3x);

            if (Math.Abs(dx) < 1e-12)
                break;

            var newT = t - (x - targetX) / dx;
            if (Math.Abs(newT - t) < 1e-12)
                return Math.Clamp(newT, 0, 1);

            t = Math.Clamp(newT, 0, 1);
        }

        return t;
    }
}
