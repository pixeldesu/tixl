using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;
using Xunit.Abstractions;

namespace Core.Tests;

public class CurveSampleCacheDiagnosticTests
{
    private readonly ITestOutputHelper _output;
    public CurveSampleCacheDiagnosticTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void ConstantPrePost_ProducesPointsInAllThreeRegions()
    {
        var curve = new Curve();
        // Default pre/post mapping is Constant
        curve.AddOrUpdateV(5.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 2.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Linear,
                                         OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                     });

        curve.SampleCache.Update(curve, 0.0, 15.0, 100.0);
        var cache = curve.SampleCache;

        _output.WriteLine($"Total points: {cache.PointCount}, FirstKeyU={cache.FirstKeyU}, LastKeyU={cache.LastKeyU}");

        var pre = cache.GetPointsInRange(double.NegativeInfinity, 5.0);
        var body = cache.GetPointsInRange(5.0, 10.0);
        var post = cache.GetPointsInRange(10.0, double.PositiveInfinity);

        _output.WriteLine($"Pre ({pre.Length} points):");
        for (var i = 0; i < pre.Length; i++)
            _output.WriteLine($"  [{pre[i].X:F4}, {pre[i].Y:F4}]");

        _output.WriteLine($"Body ({body.Length} points):");
        for (var i = 0; i < body.Length; i++)
            _output.WriteLine($"  [{body[i].X:F4}, {body[i].Y:F4}]");

        _output.WriteLine($"Post ({post.Length} points):");
        for (var i = 0; i < post.Length; i++)
            _output.WriteLine($"  [{post[i].X:F4}, {post[i].Y:F4}]");

        Assert.True(pre.Length >= 2, $"Pre should have >= 2 points, got {pre.Length}");
        Assert.True(body.Length >= 2, $"Body should have >= 2 points, got {body.Length}");
        Assert.True(post.Length >= 2, $"Post should have >= 2 points, got {post.Length}");
    }

    [Fact]
    public void SingleKey_ConstantPrePost_ProducesPointsInPreAndPost()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(5.0, new VDefinition { Value = 3.0 });

        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);
        var cache = curve.SampleCache;

        _output.WriteLine($"Total points: {cache.PointCount}, FirstKeyU={cache.FirstKeyU}, LastKeyU={cache.LastKeyU}");

        var pre = cache.GetPointsInRange(double.NegativeInfinity, 5.0);
        var body = cache.GetPointsInRange(5.0, 5.0);
        var post = cache.GetPointsInRange(5.0, double.PositiveInfinity);

        _output.WriteLine($"Pre ({pre.Length} points):");
        for (var i = 0; i < pre.Length; i++)
            _output.WriteLine($"  [{pre[i].X:F4}, {pre[i].Y:F4}]");

        _output.WriteLine($"Body ({body.Length} points):");
        for (var i = 0; i < body.Length; i++)
            _output.WriteLine($"  [{body[i].X:F4}, {body[i].Y:F4}]");

        _output.WriteLine($"Post ({post.Length} points):");
        for (var i = 0; i < post.Length; i++)
            _output.WriteLine($"  [{post[i].X:F4}, {post[i].Y:F4}]");

        Assert.True(pre.Length >= 2, $"Pre should have >= 2 points, got {pre.Length}");
        Assert.True(post.Length >= 2, $"Post should have >= 2 points, got {post.Length}");
    }

    [Fact]
    public void KeysOutsideVisibleRange_ConstantPre_StillDraws()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(20.0, new VDefinition { Value = 5.0 });
        curve.AddOrUpdateV(25.0, new VDefinition { Value = 7.0 });

        // Visible range 0-10, keys at 20-25 (completely outside view)
        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);
        var cache = curve.SampleCache;

        _output.WriteLine($"Total points: {cache.PointCount}, FirstKeyU={cache.FirstKeyU}, LastKeyU={cache.LastKeyU}");

        // Pre-region query: everything up to first key (using -Infinity like renderers do)
        var pre = cache.GetPointsInRange(double.NegativeInfinity, cache.FirstKeyU);
        _output.WriteLine($"Pre ({pre.Length} points):");
        for (var i = 0; i < pre.Length; i++)
            _output.WriteLine($"  [{pre[i].X:F4}, {pre[i].Y:F4}]");

        // Even though keys are outside, constant pre-mapping should produce a line
        // that extends across the visible range
        Assert.True(pre.Length >= 2, $"Pre should have >= 2 points, got {pre.Length}");
    }
}
