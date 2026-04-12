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

    private double _u;
    public double U
    {
        get => _u;
        set => _u = Math.Round(value, Curve.TimePrecision);
    }

    public double Value { get; set; } = 0.0;
    public KeyInterpolation InInterpolation { get; set; } = KeyInterpolation.Linear;
    public KeyInterpolation OutInterpolation { get; set; } = KeyInterpolation.Linear;

    public double InTangentAngle { get; set; }
    public double OutTangentAngle { get; set; }
    public bool Weighted { get; set; }
    public bool BrokenTangents { get; set; }

    public VDefinition Clone()
    {
        return new VDefinition()
                   {
                       Value = Value,
                       U = U,
                       InInterpolation = InInterpolation,
                       OutInterpolation = OutInterpolation,
                       InTangentAngle = InTangentAngle,
                       OutTangentAngle = OutTangentAngle,
                       Weighted = Weighted,
                       BrokenTangents = BrokenTangents
                   };
    }

    public void CopyValuesFrom(VDefinition def)
    {
        Value = def.Value;
        U = def.U;
        InInterpolation = def.InInterpolation;
        OutInterpolation = def.OutInterpolation;
        InTangentAngle = def.InTangentAngle;
        OutTangentAngle = def.OutTangentAngle;
        Weighted = def.Weighted;
        BrokenTangents = def.BrokenTangents;
    }

    internal void Read(JToken jsonV)
    {
        Value = jsonV.Value<double>(nameof(Value));
        InTangentAngle = jsonV.Value<double>(nameof(InTangentAngle));
        OutTangentAngle = jsonV.Value<double>(nameof(OutTangentAngle));
        Weighted = jsonV.ReadValueSafe(nameof(Weighted), false);
        BrokenTangents = jsonV.ReadValueSafe(nameof(BrokenTangents), false);

        // New format: unified InInterpolation / OutInterpolation
        if (jsonV[nameof(InInterpolation)] != null)
        {
            InInterpolation = jsonV[nameof(InInterpolation)].GetEnumValue(KeyInterpolation.Linear);
            OutInterpolation = jsonV[nameof(OutInterpolation)].GetEnumValue(KeyInterpolation.Linear);
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

        InInterpolation = ConvertLegacy(inTypeStr, inEditStr);
        OutInterpolation = ConvertLegacy(outTypeStr, outEditStr);
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
        writer.WriteValue(nameof(Value), Value);
        writer.WriteObject(nameof(InInterpolation), InInterpolation);
        writer.WriteObject(nameof(OutInterpolation), OutInterpolation);
        writer.WriteValue(nameof(InTangentAngle), InTangentAngle);
        writer.WriteValue(nameof(OutTangentAngle), OutTangentAngle);
        writer.WriteValue(nameof(Weighted), Weighted);
        writer.WriteValue(nameof(BrokenTangents), BrokenTangents);
    }

    private static int _nextId;
}
