#nullable enable
using System;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using T3.Core.DataTypes;
using T3.Serialization;

namespace T3.Core.Animation;

public sealed class VDefinition
{
    public enum KeyInterpolation
    {
        Constant = 0,
        Linear,
        Smooth,
        Cubic,
        Horizontal,
        Tangent,
    }

    public int UniqueId { get; } = Interlocked.Increment(ref _nextId);

    /// <summary>
    /// Set by <see cref="Curve"/> when this key is added to its table.
    /// Cleared when removed. Property setters use this to auto-invalidate the curve's cache.
    /// </summary>
    internal Curve? ParentCurve;

    private double _u;
    public double U
    {
        get => _u;
        set => _u = Math.Round(value, Curve.TimePrecision);
    }

    public double Value
    {
        get => _value;
        set { _value = value; ParentCurve?.NotifyChanged(); }
    }

    public KeyInterpolation InInterpolation
    {
        get => _inInterpolation;
        set { _inInterpolation = value; ParentCurve?.NotifyChanged(); }
    }

    public KeyInterpolation OutInterpolation
    {
        get => _outInterpolation;
        set { _outInterpolation = value; ParentCurve?.NotifyChanged(); }
    }

    public double InTangentAngle
    {
        get => _inTangentAngle;
        set { _inTangentAngle = value; ParentCurve?.NotifyChanged(); }
    }

    public double OutTangentAngle
    {
        get => _outTangentAngle;
        set { _outTangentAngle = value; ParentCurve?.NotifyChanged(); }
    }

    public bool Weighted
    {
        get => _weighted;
        set { _weighted = value; ParentCurve?.NotifyChanged(); }
    }

    public bool BrokenTangents
    {
        get => _brokenTangents;
        set { _brokenTangents = value; ParentCurve?.NotifyChanged(); }
    }

    /// <summary>Influence multiplier for the incoming tangent. Default 1.0 = full segment width.</summary>
    public float TensionIn
    {
        get => _tensionIn;
        set { _tensionIn = value; ParentCurve?.NotifyChanged(); }
    }

    /// <summary>Influence multiplier for the outgoing tangent. Default 1.0 = full segment width.</summary>
    public float TensionOut
    {
        get => _tensionOut;
        set { _tensionOut = value; ParentCurve?.NotifyChanged(); }
    }

    public VDefinition Clone()
    {
        return new VDefinition()
                   {
                       _value = _value,
                       _u = _u,
                       _inInterpolation = _inInterpolation,
                       _outInterpolation = _outInterpolation,
                       _inTangentAngle = _inTangentAngle,
                       _outTangentAngle = _outTangentAngle,
                       _weighted = _weighted,
                       _brokenTangents = _brokenTangents,
                       _tensionIn = _tensionIn,
                       _tensionOut = _tensionOut,
                       // ParentCurve intentionally NOT copied — clone is independent
                   };
    }

    public void CopyValuesFrom(VDefinition def)
    {
        _value = def._value;
        _u = def._u;
        _inInterpolation = def._inInterpolation;
        _outInterpolation = def._outInterpolation;
        _inTangentAngle = def._inTangentAngle;
        _outTangentAngle = def._outTangentAngle;
        _weighted = def._weighted;
        _brokenTangents = def._brokenTangents;
        _tensionIn = def._tensionIn;
        _tensionOut = def._tensionOut;
        ParentCurve?.NotifyChanged();
    }

    internal void Read(JToken jsonV)
    {
        _value = jsonV.Value<double>(nameof(Value));
        _inTangentAngle = jsonV.ReadValueSafe(nameof(InTangentAngle), 0.0);
        _outTangentAngle = jsonV.ReadValueSafe(nameof(OutTangentAngle), 0.0);
        _weighted = jsonV.ReadValueSafe(nameof(Weighted), false);
        _brokenTangents = jsonV.ReadValueSafe(nameof(BrokenTangents), false);
        _tensionIn = (float)jsonV.ReadValueSafe(nameof(TensionIn), 1.0);
        _tensionOut = (float)jsonV.ReadValueSafe(nameof(TensionOut), 1.0);

        // New format: unified InInterpolation / OutInterpolation
        if (jsonV[nameof(InInterpolation)] != null)
        {
            _inInterpolation = jsonV[nameof(InInterpolation)].GetEnumValue(KeyInterpolation.Linear);
            _outInterpolation = jsonV[nameof(OutInterpolation)].GetEnumValue(KeyInterpolation.Linear);
        }
        else
        {
            // Legacy format: convert InType+InEditMode / OutType+OutEditMode
            ReadLegacyInterpolation(jsonV);
        }
    }

    private void ReadLegacyInterpolation(JToken jsonV)
    {
        var inTypeStr = jsonV["InType"]?.Value<string>() ?? "Linear";
        var outTypeStr = jsonV["OutType"]?.Value<string>() ?? "Linear";
        var inEditStr = jsonV["InEditMode"]?.Value<string>() ?? "Linear";
        var outEditStr = jsonV["OutEditMode"]?.Value<string>() ?? "Linear";

        _inInterpolation = ConvertLegacy(inTypeStr, inEditStr);
        _outInterpolation = ConvertLegacy(outTypeStr, outEditStr);
    }

    private static KeyInterpolation ConvertLegacy(string interpolationType, string editMode)
    {
        return interpolationType switch
        {
            "Constant" => KeyInterpolation.Constant,
            "Linear" => KeyInterpolation.Linear,
            "Spline" => editMode switch
            {
                "Smooth" => KeyInterpolation.Smooth,
                "Cubic" => KeyInterpolation.Cubic,
                "Horizontal" => KeyInterpolation.Horizontal,
                "Tangent" => KeyInterpolation.Tangent,
                "Linear" => KeyInterpolation.Linear,
                _ => KeyInterpolation.Smooth
            },
            _ => KeyInterpolation.Linear
        };
    }

    internal void Write(JsonTextWriter writer)
    {
        writer.WriteValue(nameof(Value), _value);
        writer.WriteObject(nameof(InInterpolation), _inInterpolation);
        writer.WriteObject(nameof(OutInterpolation), _outInterpolation);

        if (_inTangentAngle != 0.0)
            writer.WriteValue(nameof(InTangentAngle), _inTangentAngle);

        if (_outTangentAngle != 0.0)
            writer.WriteValue(nameof(OutTangentAngle), _outTangentAngle);

        if (_weighted)
            writer.WriteValue(nameof(Weighted), true);

        if (_brokenTangents)
            writer.WriteValue(nameof(BrokenTangents), true);

        // ReSharper disable CompareOfFloatsByEqualityOperator
        if (_tensionIn != 1.0f)
            writer.WriteValue(nameof(TensionIn), _tensionIn);

        if (_tensionOut != 1.0f)
            writer.WriteValue(nameof(TensionOut), _tensionOut);
        // ReSharper restore CompareOfFloatsByEqualityOperator
    }

    private double _value;
    private KeyInterpolation _inInterpolation = KeyInterpolation.Linear;
    private KeyInterpolation _outInterpolation = KeyInterpolation.Linear;
    private double _inTangentAngle;
    private double _outTangentAngle;
    private bool _weighted;
    private bool _brokenTangents;
    private float _tensionIn = 1.0f;
    private float _tensionOut = 1.0f;
    private static int _nextId;
}
