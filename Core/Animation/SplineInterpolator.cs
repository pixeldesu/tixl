using System;
using System.Collections.Generic;

namespace T3.Core.Animation;

internal static class SplineInterpolator
{
    internal static void UpdateTangents(SortedList<double, VDefinition> table)
    {
        var count = table.Count;
        if (count <= 1)
            return;

        var keys = table.Keys;
        var values = table.Values;

        // First key: start tangent
        var curKey = keys[0];
        var curDef = values[0];
        var nextKey = keys[1];
        var nextDef = values[1];

        curDef.OutTangentAngle = CalcStartTangent(curKey, curDef, nextKey, nextDef);
        curDef.InTangentAngle = curDef.OutTangentAngle - Math.PI;

        // Middle keys
        for (int i = 1; i < count - 1; ++i)
        {
            var prevKey = keys[i - 1];
            var prevDef = values[i - 1];
            curKey = keys[i];
            curDef = values[i];
            nextKey = keys[i + 1];
            nextDef = values[i + 1];

            if (NeedsTangentComputation(curDef))
            {
                curDef.InTangentAngle = CalcInTangent(prevKey, prevDef, curKey, curDef, nextKey, nextDef);
                curDef.OutTangentAngle = CalcOutTangent(prevKey, prevDef, curKey, curDef, nextKey, nextDef);
            }
        }

        // Last key: end tangent
        var prevLastKey = keys[count - 2];
        var prevLastDef = values[count - 2];
        var lastKey = keys[count - 1];
        var lastDef = values[count - 1];

        lastDef.InTangentAngle = CalcEndTangent(prevLastKey, prevLastDef, lastKey, lastDef);
        lastDef.OutTangentAngle = lastDef.InTangentAngle - Math.PI;
    }

    private static bool NeedsTangentComputation(VDefinition def)
    {
        return def.InInterpolation is not (VDefinition.KeyInterpolation.Constant or VDefinition.KeyInterpolation.Linear)
               || def.OutInterpolation is not (VDefinition.KeyInterpolation.Constant or VDefinition.KeyInterpolation.Linear);
    }

    /// <summary>
    /// Cubic Hermite spline interpolation between two keys.
    /// See http://en.wikipedia.org/wiki/Monotone_cubic_interpolation
    /// </summary>
    public static double Interpolate(KeyValuePair<double, VDefinition> a, KeyValuePair<double, VDefinition> b, double u)
    {
        double t = (u - a.Key) / (b.Key - a.Key);

        double tangentLength = b.Key - a.Key;
        var p0 = a.Value.Value;
        var m0 = Math.Tan(a.Value.OutTangentAngle) * tangentLength;
        var p1 = b.Value.Value;
        var m1 = Math.Tan(b.Value.InTangentAngle) * tangentLength;

        var t2 = t * t;
        var t3 = t2 * t;
        return (2 * t3 - 3 * t2 + 1) * p0 + (t3 - 2 * t2 + t) * m0 + (-2 * t3 + 3 * t2) * p1 + (t3 - t2) * m1;
    }

    private static double CalcStartTangent(double aKey, VDefinition aDef, double bKey, VDefinition bDef)
    {
        switch (aDef.OutInterpolation)
        {
            case VDefinition.KeyInterpolation.Tangent:
                return aDef.OutTangentAngle;

            case VDefinition.KeyInterpolation.Linear:
            case VDefinition.KeyInterpolation.Smooth:
            case VDefinition.KeyInterpolation.Cubic:
                return Math.PI / 2 - Math.Atan2(aKey - bKey, aDef.Value - bDef.Value);

            case VDefinition.KeyInterpolation.Horizontal:
            default:
                return Math.PI;
        }
    }

    private static double CalcEndTangent(double aKey, VDefinition aDef, double bKey, VDefinition bDef)
    {
        switch (bDef.InInterpolation)
        {
            case VDefinition.KeyInterpolation.Tangent:
                return bDef.InTangentAngle;

            case VDefinition.KeyInterpolation.Linear:
            case VDefinition.KeyInterpolation.Smooth:
            case VDefinition.KeyInterpolation.Cubic:
                return Math.PI / 2 - Math.Atan2(bKey - aKey, bDef.Value - aDef.Value);

            case VDefinition.KeyInterpolation.Horizontal:
            default:
                return 0;
        }
    }

    private const double TANGENT_CLAMP_RATIO = 1.5;

    private static double CalcInTangent(double prevKey, VDefinition prevDef,
                                        double curKey, VDefinition curDef,
                                        double nextKey, VDefinition nextDef)
    {
        switch (curDef.InInterpolation)
        {
            case VDefinition.KeyInterpolation.Tangent:
                return curDef.InTangentAngle;

            case VDefinition.KeyInterpolation.Smooth:
                var angle = Math.PI / 2 - Math.Atan2(nextKey - prevKey, nextDef.Value - prevDef.Value);

                double thirdToPrev = (prevKey - curKey) / TANGENT_CLAMP_RATIO;
                double thirdToNext = (nextKey - curKey) / TANGENT_CLAMP_RATIO;

                // Avoid overshooting toward next keyframe
                if (prevDef.Value > nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToNext) < nextDef.Value)
                {
                    angle = Math.PI + Math.PI / 2 - Math.Atan2(-thirdToNext, Math.Max(0, curDef.Value - nextDef.Value));
                }
                else if (prevDef.Value < nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToNext) > nextDef.Value)
                {
                    angle = Math.PI + Math.PI / 2 - Math.Atan2(-thirdToNext, Math.Min(0, curDef.Value - nextDef.Value));
                }
                // Avoid overshooting toward previous keyframe
                else if (prevDef.Value > nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToPrev) > prevDef.Value)
                {
                    angle = Math.PI + Math.PI / 2 - Math.Atan2(thirdToPrev, Math.Max(0, -curDef.Value + prevDef.Value));
                }
                else if (prevDef.Value < nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToPrev) < prevDef.Value)
                {
                    angle = Math.PI + Math.PI / 2 - Math.Atan2(thirdToPrev, Math.Min(0, -curDef.Value + prevDef.Value));
                }

                return angle;

            case VDefinition.KeyInterpolation.Cubic:
                return Math.PI / 2 - Math.Atan2(nextKey - prevKey, nextDef.Value - prevDef.Value);

            case VDefinition.KeyInterpolation.Linear:
                return Math.PI / 2 - Math.Atan2(curKey - prevKey, curDef.Value - prevDef.Value);

            case VDefinition.KeyInterpolation.Horizontal:
            default:
                return 0;
        }
    }

    private static double CalcOutTangent(double prevKey, VDefinition prevDef,
                                         double curKey, VDefinition curDef,
                                         double nextKey, VDefinition nextDef)
    {
        switch (curDef.OutInterpolation)
        {
            case VDefinition.KeyInterpolation.Tangent:
                return curDef.OutTangentAngle;

            case VDefinition.KeyInterpolation.Smooth:
                double thirdToNext = (nextKey - curKey) / TANGENT_CLAMP_RATIO;
                double thirdToPrev = (prevKey - curKey) / TANGENT_CLAMP_RATIO;

                var angle = Math.PI / 2 - Math.Atan2(prevKey - nextKey, prevDef.Value - nextDef.Value);

                // Avoid overshoot toward next keyframe
                if (prevDef.Value > nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToNext) < nextDef.Value)
                {
                    angle = Math.PI / 2 - Math.Atan2(-thirdToNext, Math.Max(0, curDef.Value - nextDef.Value));
                }
                else if (prevDef.Value < nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToNext) > nextDef.Value)
                {
                    angle = Math.PI / 2 - Math.Atan2(-thirdToNext, Math.Min(0, curDef.Value - nextDef.Value));
                }
                // Avoid overshooting toward prev keyframe
                else if (prevDef.Value > nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToPrev) > prevDef.Value)
                {
                    angle = Math.PI / 2 - Math.Atan2(thirdToPrev, Math.Max(0, -curDef.Value + prevDef.Value));
                }
                else if (prevDef.Value < nextDef.Value && (curDef.Value + Math.Tan(angle) * thirdToPrev) < prevDef.Value)
                {
                    angle = Math.PI / 2 - Math.Atan2(thirdToPrev, Math.Min(0, -curDef.Value + prevDef.Value));
                }

                return angle;

            case VDefinition.KeyInterpolation.Cubic:
                return Math.PI / 2 - Math.Atan2(prevKey - nextKey, prevDef.Value - nextDef.Value);

            case VDefinition.KeyInterpolation.Linear:
                return Math.PI / 2 - Math.Atan2(curKey - nextKey, curDef.Value - nextDef.Value);

            case VDefinition.KeyInterpolation.Horizontal:
            default:
                return Math.PI;
        }
    }
}
