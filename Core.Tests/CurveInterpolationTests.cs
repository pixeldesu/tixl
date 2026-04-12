using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class CurveInterpolationTests
{
    [Fact]
    public void LinearToLinear_InterpolatesLinearly()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        Assert.Equal(0.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(0.25, curve.GetSampledValue(0.25), 4);
        Assert.Equal(0.5, curve.GetSampledValue(0.5), 4);
        Assert.Equal(0.75, curve.GetSampledValue(0.75), 4);
        Assert.Equal(1.0, curve.GetSampledValue(1.0), 4);
    }

    [Fact]
    public void LinearToLinear_NegativeSlope()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 10.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        Assert.Equal(10.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(5.0, curve.GetSampledValue(0.5), 4);
        Assert.Equal(0.0, curve.GetSampledValue(1.0), 4);
    }

    [Fact]
    public void CollinearSmoothKeys_FormStraightLine()
    {
        // Three points on y=x: [0,0] -> [0.5,0.5] -> [1,1]
        // With smooth interpolation, collinear points should produce a straight line
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 0.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(0.5, new VDefinition
                                    {
                                        Value = 0.5,
                                        InInterpolation = VDefinition.KeyInterpolation.Smooth,
                                        OutInterpolation = VDefinition.KeyInterpolation.Smooth,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        // Sample at multiple points — should all lie on y=x
        Assert.Equal(0.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(0.125, curve.GetSampledValue(0.125), 4);
        Assert.Equal(0.25, curve.GetSampledValue(0.25), 4);
        Assert.Equal(0.5, curve.GetSampledValue(0.5), 4);
        Assert.Equal(0.75, curve.GetSampledValue(0.75), 4);
        Assert.Equal(0.875, curve.GetSampledValue(0.875), 4);
        Assert.Equal(1.0, curve.GetSampledValue(1.0), 4);
    }

    [Fact]
    public void ConstantToConstant_HoldsFirstValue()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 3.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 7.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });

        // Should hold 3.0 for everything before the second key
        Assert.Equal(3.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(3.0, curve.GetSampledValue(0.25), 4);
        Assert.Equal(3.0, curve.GetSampledValue(0.5), 4);
        Assert.Equal(3.0, curve.GetSampledValue(0.999), 4);
        // At exactly the second key, should snap to 7.0
        Assert.Equal(7.0, curve.GetSampledValue(1.0), 4);
    }

    [Fact]
    public void ConstantToConstant_MultipleKeys()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 1.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 5.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });
        curve.AddOrUpdateV(2.0, new VDefinition
                                    {
                                        Value = 9.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Constant,
                                        OutInterpolation = VDefinition.KeyInterpolation.Constant,
                                    });

        Assert.Equal(1.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(1.0, curve.GetSampledValue(0.5), 4);
        Assert.Equal(5.0, curve.GetSampledValue(1.0), 4);
        Assert.Equal(5.0, curve.GetSampledValue(1.5), 4);
        Assert.Equal(9.0, curve.GetSampledValue(2.0), 4);
    }

    [Fact]
    public void BeforeFirstKey_ReturnsFirstValue()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 42.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        Assert.Equal(42.0, curve.GetSampledValue(0.0), 4);
        Assert.Equal(42.0, curve.GetSampledValue(-10.0), 4);
    }

    [Fact]
    public void AfterLastKey_ReturnsLastValue()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(0.0, new VDefinition
                                    {
                                        Value = 3.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });
        curve.AddOrUpdateV(1.0, new VDefinition
                                    {
                                        Value = 7.0,
                                        InInterpolation = VDefinition.KeyInterpolation.Linear,
                                        OutInterpolation = VDefinition.KeyInterpolation.Linear,
                                    });

        Assert.Equal(7.0, curve.GetSampledValue(2.0), 4);
        Assert.Equal(7.0, curve.GetSampledValue(100.0), 4);
    }
}
