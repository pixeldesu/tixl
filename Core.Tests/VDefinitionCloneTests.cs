using T3.Core.Animation;
using Xunit;

namespace Core.Tests;

public class VDefinitionCloneTests
{
    [Fact]
    public void CloneCopiesAllProperties()
    {
        var original = CreateNonDefaultVDefinition();

        var clone = original.Clone();

        Assert.Equal(original.U, clone.U);
        Assert.Equal(original.Value, clone.Value);
        Assert.Equal(original.InType, clone.InType);
        Assert.Equal(original.OutType, clone.OutType);
        Assert.Equal(original.InEditMode, clone.InEditMode);
        Assert.Equal(original.OutEditMode, clone.OutEditMode);
        Assert.Equal(original.InTangentAngle, clone.InTangentAngle);
        Assert.Equal(original.OutTangentAngle, clone.OutTangentAngle);
        Assert.Equal(original.Weighted, clone.Weighted);
        Assert.Equal(original.BrokenTangents, clone.BrokenTangents);
    }

    [Fact]
    public void CloneIsIndependent()
    {
        var original = CreateNonDefaultVDefinition();
        var clone = original.Clone();

        original.BrokenTangents = false;
        original.Weighted = false;
        original.Value = 999.0;

        Assert.True(clone.BrokenTangents);
        Assert.True(clone.Weighted);
        Assert.Equal(42.0, clone.Value);
    }

    [Fact]
    public void CopyValuesFromCopiesAllProperties()
    {
        var source = CreateNonDefaultVDefinition();
        var target = new VDefinition();

        target.CopyValuesFrom(source);

        Assert.Equal(source.U, target.U);
        Assert.Equal(source.Value, target.Value);
        Assert.Equal(source.InType, target.InType);
        Assert.Equal(source.OutType, target.OutType);
        Assert.Equal(source.InEditMode, target.InEditMode);
        Assert.Equal(source.OutEditMode, target.OutEditMode);
        Assert.Equal(source.InTangentAngle, target.InTangentAngle);
        Assert.Equal(source.OutTangentAngle, target.OutTangentAngle);
        Assert.Equal(source.Weighted, target.Weighted);
        Assert.Equal(source.BrokenTangents, target.BrokenTangents);
    }

    [Fact]
    public void CopyValuesFromUpdatesExistingValues()
    {
        var source = new VDefinition { BrokenTangents = true, Weighted = true };
        var target = new VDefinition();

        target.CopyValuesFrom(source);
        Assert.True(target.BrokenTangents);
        Assert.True(target.Weighted);

        source.BrokenTangents = false;
        source.Weighted = false;
        target.CopyValuesFrom(source);
        Assert.False(target.BrokenTangents);
        Assert.False(target.Weighted);
    }

    private static VDefinition CreateNonDefaultVDefinition()
    {
        return new VDefinition
               {
                   U = 10.5,
                   Value = 42.0,
                   InType = VDefinition.Interpolation.Spline,
                   OutType = VDefinition.Interpolation.Constant,
                   InEditMode = VDefinition.EditMode.Smooth,
                   OutEditMode = VDefinition.EditMode.Horizontal,
                   InTangentAngle = 1.5,
                   OutTangentAngle = 2.7,
                   Weighted = true,
                   BrokenTangents = true
               };
    }
}
