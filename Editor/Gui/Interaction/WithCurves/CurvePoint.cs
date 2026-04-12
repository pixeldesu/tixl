using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction.WithCurves;

internal static class CurvePoint
{
    public static void Draw(in Guid compositionSymbolId, VDefinition vDef, ScalableCanvas curveEditCanvas, bool isSelected, CurveEditing curveEditing, bool isNeighborOfSelected = false)
    {
        _drawList = ImGui.GetWindowDrawList();
        _curveEditCanvas = curveEditCanvas;
        _vDef = vDef;

        var pCenter = _curveEditCanvas.TransformPosition(new Vector2((float)vDef.U, (float)vDef.Value));
        var pTopLeft = pCenter - _controlSizeHalf;

        if (isSelected)
        {
            UpdateTangentVectors();
            DrawLeftTangent(pCenter);
            DrawRightTangent(pCenter);
        }
        else if (isNeighborOfSelected)
        {
            UpdateTangentVectors();
            DrawNeighborTangents(pCenter);
        }

        // Interaction
        ImGui.SetCursorScreenPos(pTopLeft );
        ImGui.InvisibleButton("key" + vDef.GetHashCode(), _controlSize);

        // Debug Visualization
        // _drawList.AddRect(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), Color.Green);
        // _drawList.AddCircle(pCenter, 3, Color.Red);
        DrawUtils.DebugItemRect();

        Icons.DrawIconOnLastItem(isSelected 
                                     ? Icon.CurveKeyframeSelected 
                                     : Icon.CurveKeyframe, Color.White);
        

        curveEditing?.HandleCurvePointDragging(compositionSymbolId, _vDef, isSelected);
    }

    private static void DrawLeftTangent(Vector2 pCenter)
    {
        var leftTangentCenter = pCenter + _leftTangentInScreen;
            
        ImGui.SetCursorPos(leftTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos);
        ImGui.InvisibleButton("keyLT" + _vDef.GetHashCode(), _tangentHandleSize);
        DrawUtils.DebugItemRect();
        var isHovered = ImGui.IsItemHovered();
        if (isHovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        _drawList.AddRectFilled(leftTangentCenter - _tangentSizeHalf, leftTangentCenter + _tangentSize,
                                isHovered ? UiColors.ForegroundFull : UiColors.Text);
        _drawList.AddLine(pCenter, leftTangentCenter, _tangentHandleColor);

        // Dragging
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
        {
            _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
            _vDef.Weighted = true;

            var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
            _vDef.InTangentAngle = (float)(Math.PI / 2 - Math.Atan2(-vectorInCanvas.X, -vectorInCanvas.Y));

            // Tension from canvas-space X offset: the handle's time-axis extent
            // maps directly to the Bezier control point offset (tension = 3 * |dx| / segmentWidth).
            // Without knowing the exact segment width here, we use the absolute canvas dx
            // scaled to a reference of 1/3 time unit (the default Bezier influence at tension=1.0).
            if (ImGui.GetIO().KeyShift)
            {
                _vDef.TensionIn = 1.0f;
            }
            else
            {
                var screenDistance = (ImGui.GetMousePos() - pCenter).Length();
                var refLength = Math.Abs(_curveEditCanvas.Scale.X) / 3.0f * T3Ui.UiScaleFactor;
                _vDef.TensionIn = Math.Clamp(screenDistance / Math.Max(refLength, 1f), 0.05f, 3.0f);
            }

            if (ImGui.GetIO().KeyCtrl)
                _vDef.BrokenTangents = true;

            if (!_vDef.BrokenTangents)
            {
                _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;
                _vDef.TensionOut = _vDef.TensionIn;
                _rightTangentInScreen = new Vector2(-_leftTangentInScreen.X, -_leftTangentInScreen.Y);
                _vDef.OutTangentAngle = _vDef.InTangentAngle + Math.PI;
            }
            else if (_vDef.OutInterpolation == VDefinition.KeyInterpolation.Linear)
            {
                _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;
            }
        }
    }

    private static void DrawRightTangent(Vector2 pCenter)
    {
        var rightTangentCenter = pCenter + _rightTangentInScreen;
        ImGui.SetCursorPos(rightTangentCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos + _fixOffset);
        ImGui.InvisibleButton("keyRT" + _vDef.GetHashCode(), _tangentHandleSize);
        var isHovered = ImGui.IsItemHovered();
        if (isHovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        _drawList.AddRectFilled(rightTangentCenter - _tangentSizeHalf, rightTangentCenter + _tangentSize,
                                isHovered ? UiColors.ForegroundFull : UiColors.Text);
        _drawList.AddLine(pCenter, rightTangentCenter, _tangentHandleColor);

        // Dragging
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
        {
            _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;
            _vDef.Weighted = true;

            var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
            _vDef.OutTangentAngle = (float)(-Math.PI / 2 - Math.Atan2(vectorInCanvas.X, vectorInCanvas.Y));

            if (ImGui.GetIO().KeyShift)
            {
                _vDef.TensionOut = 1.0f;
            }
            else
            {
                var screenDistance = (ImGui.GetMousePos() - pCenter).Length();
                var refLength = Math.Abs(_curveEditCanvas.Scale.X) / 3.0f * T3Ui.UiScaleFactor;
                _vDef.TensionOut = Math.Clamp(screenDistance / Math.Max(refLength, 1f), 0.05f, 3.0f);
            }

            if (ImGui.GetIO().KeyCtrl)
                _vDef.BrokenTangents = true;

            if (!_vDef.BrokenTangents)
            {
                _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
                _vDef.TensionIn = _vDef.TensionOut;
                _leftTangentInScreen = new Vector2(-_rightTangentInScreen.X, -_rightTangentInScreen.Y);
                _vDef.InTangentAngle = _vDef.OutTangentAngle + Math.PI;
            }
            else if (_vDef.InInterpolation == VDefinition.KeyInterpolation.Linear)
            {
                _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
            }
        }
    }

    /// <summary>
    /// Draws tangent handles for a non-selected keyframe that is adjacent to a selected one.
    /// Visual-only, no interaction — helps the user understand the curve shape.
    /// </summary>
    private static void DrawNeighborTangents(Vector2 pCenter)
    {
        var dimmedColor = _tangentHandleColor.Fade(0.3f);
        var dimmedKnob = UiColors.Text.Fade(0.3f);

        var leftCenter = pCenter + _leftTangentInScreen;
        _drawList.AddLine(pCenter, leftCenter, dimmedColor);
        _drawList.AddRectFilled(leftCenter - _tangentSizeHalf, leftCenter + _tangentSize, dimmedKnob);

        var rightCenter = pCenter + _rightTangentInScreen;
        _drawList.AddLine(pCenter, rightCenter, dimmedColor);
        _drawList.AddRectFilled(rightCenter - _tangentSizeHalf, rightCenter + _tangentSize, dimmedKnob);
    }

    /// <summary>
    /// Update tangent orientation after changing the scale of the CurveEditor
    /// </summary>
    private static void UpdateTangentVectors()
    {
        if (_curveEditCanvas == null)
            return;

        // Use the original angle→direction convention: direction = (-cos(a), sin(a))
        // Then scale to screen and apply tension.
        var normVector = new Vector2((float)-Math.Cos(_vDef.InTangentAngle),
                                     (float)Math.Sin(_vDef.InTangentAngle));

        var screenDir = new Vector2(normVector.X * _curveEditCanvas.Scale.X,
                                    -_curveEditCanvas.TransformDirection(normVector).Y);
        _leftTangentInScreen = NormalizeTangentToScreen(screenDir, _vDef.TensionIn);

        normVector = new Vector2((float)-Math.Cos(_vDef.OutTangentAngle),
                                 (float)Math.Sin(_vDef.OutTangentAngle));

        screenDir = new Vector2(normVector.X * _curveEditCanvas.Scale.X,
                                -_curveEditCanvas.TransformDirection(normVector).Y);
        _rightTangentInScreen = NormalizeTangentToScreen(screenDir, _vDef.TensionOut);
    }

    /// <summary>
    /// Normalizes a screen-space direction vector to a reference length scaled by tension.
    /// At tension=1.0, the handle is 1/3 of a time-unit in screen pixels (matching Bezier default).
    /// </summary>
    private static Vector2 NormalizeTangentToScreen(Vector2 screenDir, float tension)
    {
        var len = screenDir.Length();
        if (len < 0.001f)
            return screenDir;

        // Reference: 1/3 time-unit in screen pixels
        var oneThirdUnitInPixels = Math.Abs(_curveEditCanvas.Scale.X) / 3.0f;
        var targetLength = Math.Max(oneThirdUnitInPixels * tension, 5f) * T3Ui.UiScaleFactor;

        return screenDir * (targetLength / len);
    }

    private static ScalableCanvas _curveEditCanvas;
    private static VDefinition _vDef;
    private static ImDrawListPtr _drawList;

    private static Vector2 _leftTangentInScreen;
    private static Vector2 _rightTangentInScreen;

    // Look & style
    private static readonly Vector2 _controlSize = new(21, 21);
    private static readonly Vector2 _controlSizeHalf = _controlSize * 0.5f;

    private static readonly Vector2 _fixOffset = new(1, 7);  // Sadly there is a magic vertical offset probably caused by border or padding
        
    private static readonly Color _tangentHandleColor = new(0.1f);

    private static readonly Vector2 _tangentHandleSize = new(21, 21);
    private static readonly Vector2 _tangentHandleSizeHalf = _tangentHandleSize * 0.5f;
    private static Vector2 _tangentSize => new(3* T3Ui.UiScaleFactor) ;
    private static readonly Vector2 _tangentSizeHalf = _tangentSize * 0.5f;

    private static readonly string _keyframeIcon = "" + (char)(int)Icon.CurveKeyframe;
    private static readonly string _keyframeIconSelected = "" + (char)(int)Icon.CurveKeyframeSelected;


}