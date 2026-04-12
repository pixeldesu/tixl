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
        Assert.Equal(original.InInterpolation, clone.InInterpolation);
        Assert.Equal(original.OutInterpolation, clone.OutInterpolation);
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
    public void CloneGetsNewUniqueId()
    {
        var original = CreateNonDefaultVDefinition();
        var clone = original.Clone();

        Assert.NotEqual(original.UniqueId, clone.UniqueId);
    }

    [Fact]
    public void UniqueIdsAreUnique()
    {
        var a = new VDefinition();
        var b = new VDefinition();
        var c = new VDefinition();

        Assert.NotEqual(a.UniqueId, b.UniqueId);
        Assert.NotEqual(b.UniqueId, c.UniqueId);
        Assert.NotEqual(a.UniqueId, c.UniqueId);
    }

    [Fact]
    public void CopyValuesFromCopiesAllProperties()
    {
        var source = CreateNonDefaultVDefinition();
        var target = new VDefinition();

        target.CopyValuesFrom(source);

        Assert.Equal(source.U, target.U);
        Assert.Equal(source.Value, target.Value);
        Assert.Equal(source.InInterpolation, target.InInterpolation);
        Assert.Equal(source.OutInterpolation, target.OutInterpolation);
        Assert.Equal(source.InTangentAngle, target.InTangentAngle);
        Assert.Equal(source.OutTangentAngle, target.OutTangentAngle);
        Assert.Equal(source.Weighted, target.Weighted);
        Assert.Equal(source.BrokenTangents, target.BrokenTangents);
    }

    [Fact]
    public void CopyValuesFromDoesNotCopyUniqueId()
    {
        var source = CreateNonDefaultVDefinition();
        var target = new VDefinition();
        var originalTargetId = target.UniqueId;

        target.CopyValuesFrom(source);

        Assert.Equal(originalTargetId, target.UniqueId);
        Assert.NotEqual(source.UniqueId, target.UniqueId);
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
                   InInterpolation = VDefinition.KeyInterpolation.Smooth,
                   OutInterpolation = VDefinition.KeyInterpolation.Horizontal,
                   InTangentAngle = 1.5,
                   OutTangentAngle = 2.7,
                   Weighted = true,
                   BrokenTangents = true
               };
    }
}
