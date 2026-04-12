using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class CurveSampleCacheTests
{
    [Fact]
    public void LinearSegment_ProducesMinimalPoints()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 10.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Linear,
                                         OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                     });

        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);

        // Linear segment should produce just 2 points (endpoints)
        Assert.True(curve.SampleCache.PointCount <= 4,
                    $"Linear segment should be sparse, got {curve.SampleCache.PointCount} points");
    }

    [Fact]
    public void ConstantSegment_ProducesStepPoints()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 5.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 8.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Constant,
                                         OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                     });

        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);

        // Constant: should have step-pair points, minimal count
        Assert.True(curve.SampleCache.PointCount <= 6,
                    $"Constant segment should be sparse, got {curve.SampleCache.PointCount} points");
    }

    [Fact]
    public void SmoothSegment_ProducesAdaptivePoints()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 10.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                         OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                     });

        // At 100 pixels/unit, 10 units = 1000px, target 5px spacing → ~200 points
        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);

        Assert.True(curve.SampleCache.PointCount > 10,
                    $"Smooth segment should have many points, got {curve.SampleCache.PointCount}");
    }

    [Fact]
    public void CacheIsReusedWhenUnchanged()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition { Value = 0.0 });
        curve.AddOrUpdateV(1.0, new VDefinition { Value = 1.0 });

        curve.SampleCache.Update(curve, 0.0, 1.0, 100.0);
        var firstCount = curve.SampleCache.PointCount;

        // Second call with same params should reuse cache
        curve.SampleCache.Update(curve, 0.0, 1.0, 100.0);
        Assert.Equal(firstCount, curve.SampleCache.PointCount);
    }

    [Fact]
    public void CacheInvalidatesOnEdit()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition { Value = 0.0 });
        curve.AddOrUpdateV(1.0, new VDefinition { Value = 1.0 });

        curve.SampleCache.Update(curve, 0.0, 1.0, 100.0);
        var firstCount = curve.SampleCache.PointCount;

        // Edit: add a new keyframe
        curve.AddOrUpdateV(0.5, new VDefinition
                                    {
                                        Value = 5.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });

        curve.SampleCache.Update(curve, 0.0, 1.0, 100.0);

        // Cache should rebuild with different point count
        Assert.NotEqual(firstCount, curve.SampleCache.PointCount);
    }

    [Fact]
    public void CacheInvalidatesOnZoomChange()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 10.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                         OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                     });

        curve.SampleCache.Update(curve, 0.0, 10.0, 50.0);
        var lowZoomCount = curve.SampleCache.PointCount;

        // Double zoom → should rebuild with more points
        curve.SampleCache.Update(curve, 0.0, 10.0, 200.0);
        var highZoomCount = curve.SampleCache.PointCount;

        Assert.True(highZoomCount > lowZoomCount,
                    $"Higher zoom should produce more points: low={lowZoomCount}, high={highZoomCount}");
    }

    [Fact]
    public void CacheSurvivesSmallPan()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition { Value = 0.0 });
        curve.AddOrUpdateV(10.0, new VDefinition { Value = 10.0 });

        curve.SampleCache.Update(curve, 2.0, 8.0, 100.0);
        var firstCount = curve.SampleCache.PointCount;

        // Small pan within margin — cache should be reused (same PointCount)
        curve.SampleCache.Update(curve, 2.5, 8.5, 100.0);
        Assert.Equal(firstCount, curve.SampleCache.PointCount);
    }

    [Fact]
    public void GetPointsInRange_ReturnsSubset()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(10.0, new VDefinition
                                     {
                                         Value = 10.0,
                                         InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                         OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                     });

        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);
        var fullRange = curve.SampleCache.GetPointsInRange(0.0, 10.0);
        var halfRange = curve.SampleCache.GetPointsInRange(3.0, 7.0);

        Assert.True(halfRange.Length < fullRange.Length,
                    $"Half range ({halfRange.Length}) should be smaller than full ({fullRange.Length})");
        Assert.True(halfRange.Length > 0);
    }

    [Fact]
    public void EmptyCurve_ProducesNoPoints()
    {
        var curve = new Curve();
        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);

        Assert.Equal(0, curve.SampleCache.PointCount);
    }

    [Fact]
    public void SingleKey_ProducesPoints()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(5.0, new VDefinition { Value = 42.0 });

        curve.SampleCache.Update(curve, 0.0, 10.0, 100.0);

        Assert.True(curve.SampleCache.PointCount > 0);
    }
}
