using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;
using Xunit.Abstractions;

namespace Core.Tests;

/// <summary>
/// Tests that expose issues with smooth tangent computation,
/// specifically where straight segments get bent by overshoot clamping.
/// </summary>
public class SmoothTangentTests
{
    private readonly ITestOutputHelper _output;

    public SmoothTangentTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ThreeSmoothCollinearKeys_ShouldBeStraight()
    {
        // Three smooth keys on y=x should produce a perfectly straight line.
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 0.0));
        curve.AddOrUpdateV(0.5, MakeSmooth(0.5, 0.5));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 1.0));

        DumpTangents(curve, "ThreeSmoothCollinear y=x");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var sampled = curve.GetSampledValue(u);
            _output.WriteLine($"  u={u:F2}  expected={u:F4}  actual={sampled:F4}  error={Math.Abs(sampled - u):E2}");
            Assert.Equal(u, sampled, 3); // tolerance 0.001
        }
    }

    [Fact]
    public void ThreeSmoothCollinearKeys_Steep_ShouldBeStraight()
    {
        // y = 10*x: steeper slope, still collinear
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 0.0));
        curve.AddOrUpdateV(0.5, MakeSmooth(0.5, 5.0));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 10.0));

        DumpTangents(curve, "ThreeSmoothCollinear y=10x");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var expected = u * 10.0;
            var sampled = curve.GetSampledValue(u);
            _output.WriteLine($"  u={u:F2}  expected={expected:F4}  actual={sampled:F4}  error={Math.Abs(sampled - expected):E2}");
            Assert.Equal(expected, sampled, 2); // tolerance 0.01
        }
    }

    [Fact]
    public void FourSmoothCollinearKeys_ShouldBeStraight()
    {
        // Four evenly spaced smooth keys on y=x
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 0.0));
        curve.AddOrUpdateV(0.333, MakeSmooth(0.333, 0.333));
        curve.AddOrUpdateV(0.667, MakeSmooth(0.667, 0.667));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 1.0));

        DumpTangents(curve, "FourSmoothCollinear y=x");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var sampled = curve.GetSampledValue(u);
            _output.WriteLine($"  u={u:F2}  expected={u:F4}  actual={sampled:F4}  error={Math.Abs(sampled - u):E2}");
            Assert.Equal(u, sampled, 3);
        }
    }

    [Fact]
    public void ThreeSmoothCollinearKeys_UnevenSpacing_ShouldBeStraight()
    {
        // Collinear points with uneven spacing: [0,0], [0.1, 0.1], [1.0, 1.0]
        // This is a common failure case for tangent clamping heuristics.
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 0.0));
        curve.AddOrUpdateV(0.1, MakeSmooth(0.1, 0.1));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 1.0));

        DumpTangents(curve, "ThreeSmoothCollinear uneven [0,0]-[0.1,0.1]-[1,1]");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var sampled = curve.GetSampledValue(u);
            _output.WriteLine($"  u={u:F2}  expected={u:F4}  actual={sampled:F4}  error={Math.Abs(sampled - u):E2}");
            Assert.Equal(u, sampled, 2); // tolerance 0.01
        }
    }

    [Fact]
    public void ThreeSmoothCollinearKeys_Descending_ShouldBeStraight()
    {
        // Descending collinear: y = 1 - x
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 1.0));
        curve.AddOrUpdateV(0.5, MakeSmooth(0.5, 0.5));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 0.0));

        DumpTangents(curve, "ThreeSmoothCollinear descending y=1-x");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var expected = 1.0 - u;
            var sampled = curve.GetSampledValue(u);
            _output.WriteLine($"  u={u:F2}  expected={expected:F4}  actual={sampled:F4}  error={Math.Abs(sampled - expected):E2}");
            Assert.Equal(expected, sampled, 3);
        }
    }

    [Fact]
    public void ThreeSmoothKeys_ConstantValue_ShouldBeFlat()
    {
        // All keys at same value — must remain flat
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, MakeSmooth(0.0, 5.0));
        curve.AddOrUpdateV(0.5, MakeSmooth(0.5, 5.0));
        curve.AddOrUpdateV(1.0, MakeSmooth(1.0, 5.0));

        DumpTangents(curve, "ThreeSmooth constant y=5");

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var sampled = curve.GetSampledValue(u);
            Assert.Equal(5.0, sampled, 4);
        }
    }

    private static VDefinition MakeSmooth(double time, double value)
    {
        return new VDefinition
               {
                   U = time,
                   Value = value,
                   InInterpolation = VDefinition.KeyInterpolation.Smooth,
                   OutInterpolation = VDefinition.KeyInterpolation.Smooth,
               };
    }

    private void DumpTangents(Curve curve, string label)
    {
        _output.WriteLine($"--- {label} ---");
        foreach (var kvp in curve.Table)
        {
            var v = kvp.Value;
            _output.WriteLine($"  U={v.U:F4}  Value={v.Value:F4}  " +
                              $"InAngle={v.InTangentAngle:F4} ({Math.Atan(Math.Tan(v.InTangentAngle)):F4} slope={Math.Tan(v.InTangentAngle):F4})  " +
                              $"OutAngle={v.OutTangentAngle:F4} ({Math.Atan(Math.Tan(v.OutTangentAngle)):F4} slope={Math.Tan(v.OutTangentAngle):F4})");
        }
    }
}
