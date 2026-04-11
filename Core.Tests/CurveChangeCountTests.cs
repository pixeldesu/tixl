using T3.Core.Animation;
using T3.Core.DataTypes;
using Xunit;

namespace Core.Tests;

public class CurveChangeCountTests
{
    [Fact]
    public void AddOrUpdateVIncrementsChangeCountOnce()
    {
        var curve = new Curve();
        var initial = curve.ChangeCount;

        curve.AddOrUpdateV(1.0, new VDefinition { Value = 5.0 });

        Assert.Equal(initial + 1, curve.ChangeCount);
    }

    [Fact]
    public void UpdateCurveValuesFloatIncrementsChangeCountOnce()
    {
        var curves = new[] { new Curve() };
        curves[0].AddOrUpdateV(0.0, new VDefinition { Value = 0.0 });
        var initial = curves[0].ChangeCount;

        Curve.UpdateCurveValues(curves, 0.0, new float[] { 1.0f });

        Assert.Equal(initial + 1, curves[0].ChangeCount);
    }

    [Fact]
    public void UpdateCurveValuesIntIncrementsChangeCountOnce()
    {
        var curves = new[] { new Curve() };
        curves[0].AddOrUpdateV(0.0, new VDefinition
                                        {
                                            Value = 0.0,
                                            InType = VDefinition.Interpolation.Constant,
                                            OutType = VDefinition.Interpolation.Constant
                                        });
        var initial = curves[0].ChangeCount;

        Curve.UpdateCurveValues(curves, 0.0, new int[] { 2 });

        Assert.Equal(initial + 1, curves[0].ChangeCount);
    }

    [Fact]
    public void RemoveKeyframeIncrementsChangeCountOnce()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(1.0, new VDefinition { Value = 5.0 });
        var initial = curve.ChangeCount;

        curve.RemoveKeyframeAt(1.0);

        Assert.Equal(initial + 1, curve.ChangeCount);
    }

    [Fact]
    public void MoveKeyIncrementsChangeCountOnce()
    {
        var curve = new Curve();
        curve.AddOrUpdateV(1.0, new VDefinition { Value = 5.0 });
        var initial = curve.ChangeCount;

        curve.MoveKey(1.0, 2.0);

        Assert.Equal(initial + 1, curve.ChangeCount);
    }
}
