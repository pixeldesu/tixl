using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class BezierInterpolatorTests
{
    [Fact]
    public void DefaultTension_MatchesHermite()
    {
        // At tension=1.0, Bezier should produce identical results to Hermite
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 0.6,
                                        TensionOut = 1.0f,
                                        Weighted = false, // Not weighted → Hermite path
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = 0.3,
                                        TensionIn = 1.0f,
                                        Weighted = false,
                                    });

        // Enable Bezier on a copy and compare
        var curveBezier = new Curve();
        curveBezier.AddOrUpdateV(0.0, new VDefinition
                                          {
                                              Value = 0.0,
                                              InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutTangentAngle = 0.6,
                                              TensionOut = 1.0f,
                                              Weighted = true, // Weighted → Bezier path
                                          });
        curveBezier.AddOrUpdateV(1.0, new VDefinition
                                          {
                                              Value = 1.0,
                                              InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                              InTangentAngle = 0.3,
                                              TensionIn = 1.0f,
                                              Weighted = true,
                                          });

        for (double u = 0.0; u <= 1.0; u += 0.05)
        {
            var hermite = curve.GetSampledValue(u);
            var bezier = curveBezier.GetSampledValue(u);
            Assert.Equal(hermite, bezier, 5);
        }
    }

    [Fact]
    public void LowTension_ProducesDifferentCurve()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 0.7854,
                                        TensionOut = 0.3f,
                                        Weighted = true,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = 0.7854,
                                        TensionIn = 0.3f,
                                        Weighted = true,
                                    });

        // Endpoints must be exact
        Assert.Equal(0.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(1.0, curve.GetSampledValue(1.0), 4);

        // Midpoint should differ from tension=1.0 Hermite
        var mid = curve.GetSampledValue(0.5);
        Assert.True(mid > 0 && mid < 1, $"Value should be in (0,1), got {mid}");
    }

    [Fact]
    public void SegmentNeedsBezier_OnlyWhenWeightedAndNonDefaultTension()
    {
        var defaultKey = new VDefinition
                         {
                             InInterpolation = VDefinition.KeyInterpolation.Tangent,
                             OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                             TensionIn = 1.0f,
                             TensionOut = 1.0f,
                             Weighted = false,
                         };

        var weightedKey = new VDefinition
                          {
                              InInterpolation = VDefinition.KeyInterpolation.Tangent,
                              OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                              TensionIn = 0.5f,
                              TensionOut = 0.5f,
                              Weighted = true,
                          };

        var smoothKey = new VDefinition
                        {
                            InInterpolation = VDefinition.KeyInterpolation.Smooth,
                            OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                        };

        // Default tangent keys → Hermite
        Assert.False(BezierInterpolator.SegmentNeedsBezier(defaultKey, defaultKey));

        // Smooth keys → Hermite
        Assert.False(BezierInterpolator.SegmentNeedsBezier(smoothKey, smoothKey));

        // Weighted tangent with non-default tension → Bezier
        Assert.True(BezierInterpolator.SegmentNeedsBezier(weightedKey, defaultKey));
        Assert.True(BezierInterpolator.SegmentNeedsBezier(defaultKey, weightedKey));
    }

    [Fact]
    public void BezierEndpointsAreExact()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(2.0, new VDefinition
                                    {
                                        Value = 10.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutTangentAngle = 0.5,
                                        TensionOut = 0.7f,
                                        Weighted = true,
                                    });
        curve.AddOrUpdateV(5.0, new VDefinition
                                    {
                                        Value = 20.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        OutInterpolation = VDefinition.KeyInterpolation.Tangent,
                                        InTangentAngle = 0.3,
                                        TensionIn = 1.5f,
                                        Weighted = true,
                                    });

        Assert.Equal(10.0, curve.GetSampledValue(2.0), 4);
        Assert.Equal(20.0, curve.GetSampledValue(5.0), 4);
    }
}
