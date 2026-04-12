using ImGuiNET;
using T3.Core.Animation;
using T3.Core.DataTypes.Vector;
using T3.Editor.Gui.Styling;
using T3.Editor.Gui.UiHelpers;

namespace T3.Editor.Gui.Interaction.WithCurves;

internal static class CurvePoint
{
    public static void Draw(in Guid compositionSymbolId, VDefinition vDef, ScalableCanvas curveEditCanvas, bool isSelected, CurveEditing curveEditing, bool isNeighborOfSelected = false,
                            VDefinition? prevKey = null, VDefinition? nextKey = null)
    {
        _drawList = ImGui.GetWindowDrawList();
        _curveEditCanvas = curveEditCanvas;
        _vDef = vDef;

        // Compute neighbor info for snapping and proportional handle length
        _neighborAngleIn = null;
        _neighborAngleOut = null;
        _segmentWidthIn = 1.0;
        _segmentWidthOut = 1.0;

        if (prevKey != null)
        {
            _neighborAngleIn = Math.PI / 2 - Math.Atan2(vDef.U - prevKey.U, vDef.Value - prevKey.Value);
            _segmentWidthIn = Math.Abs(vDef.U - prevKey.U);
        }
        if (nextKey != null)
        {
            _neighborAngleOut = Math.PI / 2 - Math.Atan2(vDef.U - nextKey.U, vDef.Value - nextKey.Value);
            _segmentWidthOut = Math.Abs(vDef.U - nextKey.U);
        }

        var pCenter = _curveEditCanvas.TransformPosition(new Vector2((float)vDef.U, (float)vDef.Value));
        var pTopLeft = pCenter - _controlSizeHalf;

        if (isSelected)
        {
            UpdateTangentVectors();
            DrawTangentGuides(pCenter);
            DrawTangentHandle(pCenter, TangentSide.In);
            DrawTangentHandle(pCenter, TangentSide.Out);
        }
        else if (isNeighborOfSelected)
        {
            UpdateTangentVectors();
            DrawTangentGuides(pCenter);
            DrawTangentHandle(pCenter, TangentSide.In, dimmed: true);
            DrawTangentHandle(pCenter, TangentSide.Out, dimmed: true);
        }

        // Keyframe interaction
        ImGui.SetCursorScreenPos(pTopLeft);
        ImGui.InvisibleButton("key" + vDef.GetHashCode(), _controlSize);
        DrawUtils.DebugItemRect();

        Icons.DrawIconOnLastItem(isSelected
                                     ? Icon.CurveKeyframeSelected
                                     : Icon.CurveKeyframe, Color.White);

        curveEditing?.HandleCurvePointDragging(compositionSymbolId, _vDef, isSelected);
    }

    private enum TangentSide { In, Out }

    private static void DrawTangentHandle(Vector2 pCenter, TangentSide side, bool dimmed = false)
    {
        var isIn = side == TangentSide.In;
        var handleOffset = isIn ? _leftTangentInScreen : _rightTangentInScreen;
        var handleCenter = pCenter + handleOffset;

        var lineColor = dimmed ? _tangentHandleColor.Fade(0.25f) : _tangentHandleColor;
        var knobColor = dimmed ? UiColors.Text.Fade(0.4f) : UiColors.Text;

        var buttonId = isIn ? "keyLT" + _vDef.GetHashCode() : "keyRT" + _vDef.GetHashCode();
        var cursorOffset = isIn ? Vector2.Zero : _fixOffset;

        ImGui.SetCursorPos(handleCenter - _tangentHandleSizeHalf - _curveEditCanvas.WindowPos + cursorOffset);
        ImGui.InvisibleButton(buttonId, _tangentHandleSize);
        var isHovered = ImGui.IsItemHovered();
        if (isHovered)
            ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);

        // Dragging
        if (ImGui.IsItemActive() && ImGui.IsMouseDragging(0, 0f))
        {
            HandleTangentDrag(pCenter, side);
        }
        else if (ImGui.IsItemDeactivated())
        {
            _isDraggingTangent = false;
        }

        _drawList.AddRectFilled(handleCenter - _tangentSizeHalf, handleCenter + _tangentSize,
                                isHovered ? UiColors.ForegroundFull : knobColor);
        _drawList.AddLine(pCenter, handleCenter, lineColor);
    }

    private static void HandleTangentDrag(Vector2 pCenter, TangentSide side)
    {
        var isIn = side == TangentSide.In;

        // Set this side to Tangent mode
        if (isIn)
            _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
        else
            _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;

        _vDef.Weighted = true;

        // Compute angle from mouse position
        var vectorInCanvas = _curveEditCanvas.InverseTransformDirection(ImGui.GetMousePos() - pCenter);
        var rawAngle = isIn
                           ? Math.PI / 2 - Math.Atan2(-vectorInCanvas.X, -vectorInCanvas.Y)
                           : -Math.PI / 2 - Math.Atan2(vectorInCanvas.X, vectorInCanvas.Y);

        // Compute tension from screen distance, proportional to segment width
        var segmentWidth = isIn ? _segmentWidthIn : _segmentWidthOut;
        var segmentPixels = (float)segmentWidth * Math.Abs(_curveEditCanvas.Scale.X);
        var refLength = segmentPixels / 3.0f * T3Ui.UiScaleFactor;
        var screenDistance = (ImGui.GetMousePos() - pCenter).Length();
        var rawTension = Math.Clamp(screenDistance / Math.Max(refLength, 1f), 0.05f, 3.0f);

        // Apply snapping (unless Shift held)
        double finalAngle;
        float finalTension;

        if (ImGui.GetIO().KeyShift)
        {
            finalAngle = rawAngle;
            finalTension = rawTension;
        }
        else
        {
            var neighborAngle = isIn ? _neighborAngleIn : _neighborAngleOut;
            var snaps = ApplyTangentSnaps(ImGui.GetMousePos(), pCenter,
                                          rawAngle, rawTension, neighborAngle, segmentWidth,
                                          out finalAngle, out finalTension);

            if ((snaps & TangentSnaps.DefaultLength) != 0)
                _vDef.Weighted = false;

            if ((snaps & (TangentSnaps.Horizontal | TangentSnaps.Linear)) != 0)
            {
                _lastAngleSnaps = snaps & (TangentSnaps.Horizontal | TangentSnaps.Linear);
                _lastAngleSnapAngle = finalAngle;
                _lastSnapCenter = pCenter;
                _lastAngleSnapTime = ImGui.GetTime();
            }
        }

        // Apply to this side
        if (isIn)
        {
            _vDef.InTangentAngle = (float)finalAngle;
            _vDef.TensionIn = finalTension;
        }
        else
        {
            _vDef.OutTangentAngle = (float)finalAngle;
            _vDef.TensionOut = finalTension;
        }

        // Track drag state
        _isDraggingTangent = true;
        _draggedKeyId = _vDef.UniqueId;
        _activeDragAngle = finalAngle;
        _activeDragSegmentWidth = segmentWidth;

        // Ctrl breaks tangents
        if (ImGui.GetIO().KeyCtrl)
            _vDef.BrokenTangents = true;

        // Mirror angle (but not tension) to opposite side if tangents are linked
        if (!_vDef.BrokenTangents)
        {
            if (isIn)
            {
                _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;
                _rightTangentInScreen = -_leftTangentInScreen;
                _vDef.OutTangentAngle = _vDef.InTangentAngle + (float)Math.PI;
            }
            else
            {
                _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
                _leftTangentInScreen = -_rightTangentInScreen;
                _vDef.InTangentAngle = _vDef.OutTangentAngle + (float)Math.PI;
            }
        }
        else
        {
            // Promote the other side from Linear if needed
            var otherInterpolation = isIn ? _vDef.OutInterpolation : _vDef.InInterpolation;
            if (otherInterpolation == VDefinition.KeyInterpolation.Linear)
            {
                if (isIn)
                    _vDef.OutInterpolation = VDefinition.KeyInterpolation.Tangent;
                else
                    _vDef.InInterpolation = VDefinition.KeyInterpolation.Tangent;
            }
        }
    }

    #region Tangent display

    private static void UpdateTangentVectors()
    {
        if (_curveEditCanvas == null)
            return;

        _leftTangentInScreen = ComputeScreenTangent(_vDef.InTangentAngle, _vDef.TensionIn, _segmentWidthIn);
        _rightTangentInScreen = ComputeScreenTangent(_vDef.OutTangentAngle, _vDef.TensionOut, _segmentWidthOut);
    }

    private static Vector2 ComputeScreenTangent(double angle, float tension, double segmentWidth)
    {
        var normVector = new Vector2((float)-Math.Cos(angle), (float)Math.Sin(angle));
        var screenDir = new Vector2(normVector.X * _curveEditCanvas.Scale.X,
                                    -_curveEditCanvas.TransformDirection(normVector).Y);

        var len = screenDir.Length();
        if (len < 0.001f)
            return screenDir;

        // Reference length = 1/3 of the SEGMENT width (not 1/3 of one time unit)
        var segmentPixels = (float)segmentWidth * Math.Abs(_curveEditCanvas.Scale.X);
        var refLength = segmentPixels / 3.0f;
        var targetLength = Math.Max(refLength * tension, 5f) * T3Ui.UiScaleFactor;
        return screenDir * (targetLength / len);
    }

    #endregion

    #region Snapping

    [Flags]
    private enum TangentSnaps
    {
        None = 0,
        Horizontal = 1,
        Linear = 2,
        DefaultLength = 4,
    }

    private const float SnapThresholdPx = 7f;

    private static TangentSnaps ApplyTangentSnaps(
        Vector2 mouseScreenPos, Vector2 keyScreenPos,
        double rawAngle, float rawTension,
        double? neighborAngle, double segmentWidth,
        out double snappedAngle, out float snappedTension)
    {
        snappedAngle = rawAngle;
        snappedTension = rawTension;
        var snaps = TangentSnaps.None;
        var mouseOffset = mouseScreenPos - keyScreenPos;

        // 1. Horizontal — perpendicular pixel distance to horizontal line
        if (Math.Abs(mouseOffset.Y) < SnapThresholdPx)
        {
            snappedAngle = Math.Round(snappedAngle / Math.PI) * Math.PI;
            snaps |= TangentSnaps.Horizontal;
        }

        // 2. Linear (toward neighbor) — perpendicular pixel distance to neighbor line
        if (neighborAngle.HasValue && (snaps & TangentSnaps.Horizontal) == 0)
        {
            var lineDir = GetScreenDirForAngle(neighborAngle.Value);
            var lineLen = lineDir.Length();
            if (lineLen > 0.001f)
            {
                var perpDist = Math.Abs(mouseOffset.X * lineDir.Y - mouseOffset.Y * lineDir.X) / lineLen;
                if (perpDist < SnapThresholdPx)
                {
                    snappedAngle = neighborAngle.Value;
                    snaps |= TangentSnaps.Linear;
                }
            }
        }

        // 3. Default length (tension=1.0) — distance from mouse to reference radius (proportional to segment)
        var segmentPixels = (float)segmentWidth * Math.Abs(_curveEditCanvas.Scale.X);
        var refLength = segmentPixels / 3.0f * T3Ui.UiScaleFactor;
        if (Math.Abs(mouseOffset.Length() - refLength) < SnapThresholdPx)
        {
            snappedTension = 1.0f;
            snaps |= TangentSnaps.DefaultLength;
        }

        return snaps;
    }

    #endregion

    #region Tangent guides

    private static void DrawTangentGuides(Vector2 pCenter)
    {
        if (_draggedKeyId != _vDef.UniqueId)
            return;

        const float guideExtent = 4000f;

        // 1. Tangent extension line — always visible while dragging
        if (_isDraggingTangent && _curveEditCanvas != null)
        {
            var extensionColor = UiColors.ForegroundFull.Fade(0.03f);
            var screenDir = GetScreenDirForAngle(_activeDragAngle);
            if (screenDir.Length() > 0.001f)
            {
                screenDir *= guideExtent / screenDir.Length();
                _drawList.AddLine(pCenter - screenDir, pCenter + screenDir, extensionColor, 1f);
            }

            // Default length dots — proportional to active segment
            var segPixels = (float)_activeDragSegmentWidth * Math.Abs(_curveEditCanvas.Scale.X);
            var refLength = segPixels / 3.0f * T3Ui.UiScaleFactor;
            if (screenDir.Length() > 0.001f)
            {
                var dotDir = screenDir * (refLength / guideExtent);
                var dotColor = UiColors.ForegroundFull.Fade(0.1f);
                _drawList.AddCircleFilled(pCenter + dotDir, 3f, dotColor);
                _drawList.AddCircleFilled(pCenter - dotDir, 3f, dotColor);
            }
        }

        // 2. Snap angle guides — orange, with fade-out
        var elapsed = ImGui.GetTime() - _lastAngleSnapTime;
        if (elapsed < SnapFadeDuration && _lastAngleSnaps != TangentSnaps.None)
        {
            var opacity = (float)Math.Max(0, 1.0 - elapsed / SnapFadeDuration);
            var snapColor = UiColors.StatusAnimated.Fade(0.24f * opacity);

            if ((_lastAngleSnaps & TangentSnaps.Horizontal) != 0)
            {
                _drawList.AddLine(
                    _lastSnapCenter - new Vector2(guideExtent, 0),
                    _lastSnapCenter + new Vector2(guideExtent, 0),
                    snapColor, 1f);
            }

            if ((_lastAngleSnaps & TangentSnaps.Linear) != 0)
            {
                var screenDir = GetScreenDirForAngle(_lastAngleSnapAngle);
                if (screenDir.Length() > 0.001f)
                {
                    screenDir *= guideExtent / screenDir.Length();
                    _drawList.AddLine(_lastSnapCenter - screenDir, _lastSnapCenter + screenDir, snapColor, 1f);
                }
            }
        }
    }

    private static Vector2 GetScreenDirForAngle(double angle)
    {
        var dir = new Vector2((float)-Math.Cos(angle), (float)Math.Sin(angle));
        return new Vector2(dir.X * _curveEditCanvas.Scale.X,
                           -_curveEditCanvas.TransformDirection(dir).Y);
    }

    #endregion

    #region State

    private const double SnapFadeDuration = 0.8;

    // Active drag state — tied to a specific keyframe via UniqueId
    private static bool _isDraggingTangent;
    private static double _activeDragAngle;
    private static double _activeDragSegmentWidth;
    private static int _draggedKeyId;

    // Angle snap fade state
    private static TangentSnaps _lastAngleSnaps = TangentSnaps.None;
    private static double _lastAngleSnapAngle;
    private static Vector2 _lastSnapCenter;
    private static double _lastAngleSnapTime;

    private static ScalableCanvas _curveEditCanvas;
    private static VDefinition _vDef;
    private static ImDrawListPtr _drawList;

    private static Vector2 _leftTangentInScreen;
    private static Vector2 _rightTangentInScreen;
    private static double? _neighborAngleIn;
    private static double? _neighborAngleOut;

    #endregion

    #region Style constants

    private static readonly Vector2 _controlSize = new(21, 21);
    private static readonly Vector2 _controlSizeHalf = _controlSize * 0.5f;
    private static readonly Vector2 _fixOffset = new(1, 7);
    private static readonly Color _tangentHandleColor = new(0.3f);
    private static readonly Vector2 _tangentHandleSize = new(15, 15);
    private static readonly Vector2 _tangentHandleSizeHalf = _tangentHandleSize * 0.5f;
    private static Vector2 _tangentSize => new(3 * T3Ui.UiScaleFactor);
    private static readonly Vector2 _tangentSizeHalf = _tangentSize * 0.5f;

    #endregion
}
