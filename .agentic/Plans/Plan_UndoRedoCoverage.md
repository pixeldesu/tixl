# Plan: Undo/Redo Coverage Gaps

This document inventories all user-facing model mutations that bypass the `ICommand`/`UndoRedoStack` system. Each gap is categorized by severity and estimated effort. The intent is to drive follow-up work to close these gaps -- ideally test-driven with the command-level integration test framework.

## Gap Inventory

### CRITICAL -- Users definitely expect Ctrl+Z to work here

#### 1. Insert/Remove Keyframe via UI buttons (no undo)

**Files:**
- `Editor/UiModel/InputsAndTypes/InputValueUi.cs:343-350` -- explicit TODO: `// TODO: this should use Undo/Redo commands`
- `Editor/UiModel/InputsAndTypes/InputValueUi.cs:380,389` -- same pattern
- `Editor/Gui/Windows/TimeLine/DopeSheetArea.cs:322-327` -- `InsertNewKeyframe()` calls `AnimationOperations` directly

**What happens:** Clicking the keyframe toggle button in the parameter UI or pressing the Insert Keyframe shortcut in the timeline directly calls `AnimationOperations.InsertKeyframeToCurves()` / `RemoveKeyframeFromCurves()` without wrapping in a command.

**Fix:** Wrap in existing `AddKeyframesCommand` / `DeleteKeyframeCommand`. These commands already exist for curve editor drag operations -- they just aren't used in these code paths.

**Effort:** LOW (1-2 hours per call site, ~4 call sites)

---

#### 2. Duplicate Keyframes in Timeline (no undo)

**Files:**
- `Editor/Gui/Windows/TimeLine/DopeSheetArea.cs:62-63` -- `DuplicateSelectedKeyframes()`
- `Editor/Gui/Windows/TimeLine/TimelineCurveEditArea.cs:57` -- same

**What happens:** Ctrl+D in dope sheet or curve editor duplicates keyframes. Calls `FlagAsModified()` but never creates a command.

**Fix:** Create a `DuplicateKeyframesCommand` or use `MacroCommand` wrapping individual `AddKeyframesCommand` calls.

**Effort:** LOW-MEDIUM (half day)

---

#### 3. Delete Keyframes in Timeline (no undo)

**Files:**
- `Editor/Gui/Windows/TimeLine/DopeSheetArea.cs:859-864` -- `DeleteSelectedElements()`
- `Editor/Gui/Windows/TimeLine/TimelineCurveEditArea.cs:350-354` -- same

**What happens:** Delete key in timeline directly calls `AnimationOperations.DeleteSelectedKeyframesFromAnimationParameters()`.

**Fix:** Wrap in `DeleteKeyframeCommand` calls within a `MacroCommand`.

**Effort:** LOW (half day)

---

#### 4. Insert Keyframe with Increment (no undo)

**File:** `Editor/Gui/Windows/TimeLine/DopeSheetArea.cs:75-83`

**What happens:** The "Insert Keyframe with Increment" action directly inserts keyframes with a value offset of +1. No command.

**Fix:** Same as #1 -- wrap in `AddKeyframesCommand`.

**Effort:** LOW (1 hour)

---

#### 5. Set Current Value as Default (no undo)

**Files:**
- `Editor/UiModel/InputsAndTypes/InputValueUi.cs:245-250` -- explicit TODO: `// Todo: Implement Undo/Redo Command`
- `Editor/UiModel/InputsAndTypes/InputValueUi.cs:477-482` -- same
- `Editor/Gui/InputUi/CombinedInputs/GradientInputUi.cs:35`

**What happens:** Context menu "Set as Default" directly calls `input.SetCurrentValueAsDefault()` + `InvalidateInputDefaultInInstances()`. Irreversible.

**Fix:** Create `SetInputDefaultCommand` that stores original default value and restores it on undo.

**Effort:** MEDIUM (new command class, ~half day)

---

#### 6. Variation Create/Update/Remove (no undo)

**Files:**
- `Editor/Gui/Interaction/Variations/VariationHandling.cs:104-134` -- `CreateOrUpdateSnapshotVariation()` -- explicit TODO at line 136: `// TODO: Implement undo/redo!`
- `Editor/Gui/Interaction/Variations/VariationHandling.cs:136-156` -- `RemoveInstancesFromVariations()`
- `Editor/Gui/Interaction/Variations/Model/SymbolVariationPool.cs:454,163` -- direct dict mutations

**What happens:** Creating snapshots, updating presets, and removing instances from variations all mutate the `VariationPool` directly and save to disk. No undo.

**Fix:** Extend variation commands. `AddPresetOrVariationCommand` and `DeleteVariationCommand` exist but don't cover all mutation paths. Need:
- `UpdateVariationCommand` (for snapshot updates)
- Wrap `RemoveInstancesFromVariations` in a command that stores removed entries

**Effort:** MEDIUM-HIGH (1-2 days, variation pool persistence adds complexity)

---

#### 7. Curve Tangent Handle Editing (no undo)

**File:** `Editor/Gui/Interaction/WithCurves/CurvePoint.cs:62-113`

**What happens:** Dragging tangent handles directly mutates `VDefinition` properties (`InType`, `InEditMode`, `InTangentAngle`, `BrokenTangents`, `OutType`, etc.) without creating a command.

**Fix:** Use the in-flight command pattern (like `ChangeKeyframesCommand` already does for keyframe position dragging). Capture VDefinition state on mouse-down, apply on mouse-up via command.

**Effort:** MEDIUM (half day -- pattern already exists, just not applied here)

---

#### 8. Parameter Extraction (no undo)

**Files:**
- `Editor/Gui/Graph/Interaction/ParameterExtraction.cs:70` -- explicit TODO: `// Todo - make this undoable - currently not implemented with the new extraction system`
- `Editor/Gui/MagGraph/Ui/MagGraphView.cs:201` -- `// Todo: This should use undo/redo`

**What happens:** "Extract as connected operator" (right-click an input, extract it as a separate node with connection) executes multiple sub-operations (add child, add connection, set value) without wrapping them in a `MacroCommand`.

**Fix:** Wrap the sequence in a `MacroCommand`. Individual operations already use commands internally -- they just need to be grouped.

**Effort:** MEDIUM (half day)

---

#### 9. Edit Symbol Description / Links (no undo)

**Files:**
- `Editor/Gui/Graph/Dialogs/EditSymbolDescriptionDialog.cs:30,54,64,69`

**What happens:** Editing a symbol's description text, adding/removing documentation links directly mutates `symbolUi.Description` and `symbolUi.Links` dictionary.

**Fix:** Create `ChangeSymbolDescriptionCommand` that stores old description + links snapshot.

**Effort:** LOW-MEDIUM (half day, new command)

---

#### 10. Edit Node Comment (no undo)

**File:** `Editor/Gui/Graph/Dialogs/EditCommentDialog.cs:44`

**What happens:** Editing a node's comment directly sets `symbolChildUi.Comment = comment`.

**Fix:** Create `ChangeCommentCommand` (trivial -- store old comment, set new, reverse on undo).

**Effort:** LOW (1-2 hours)

---

#### 11. Snapshot Enable/Disable Toggle (no undo)

**Files:**
- `Editor/Gui/MagGraph/Interaction/GraphContextMenu.cs:229-253`
- `Editor/Gui/Graph/Legacy/GraphView.cs:655-679`

**What happens:** "Enable/Disable for Snapshots" in context menu directly sets `child.EnabledForSnapshots` and then calls `VariationHandling.RemoveInstancesFromVariations()` (which also has no undo -- see #6).

**Fix:** Create `ChangeSnapshotEnabledCommand`. The variation removal part depends on fixing #6 first.

**Effort:** MEDIUM (depends on #6)

---

#### 12. Playback Settings Changes (no undo)

**Files:**
- `Editor/Gui/Windows/TimeLine/PlaybackSettingsPopup.cs:271`
- `Editor/Gui/Windows/TimeLine/TimeControls.cs:197,391`

**What happens:** Changing BPM, sync mode, and other playback settings directly mutates `PlaybackSettings` and calls `FlagAsModified()`.

**Fix:** Create `ChangePlaybackSettingsCommand` or consider these "project settings" that don't need undo. This is debatable -- BPM changes are often experimental and users may want to undo them.

**Effort:** LOW-MEDIUM (half day if deemed necessary)

---

#### 13. StructuredList Input Editing (no undo)

**File:** `Editor/Gui/InputUi/CombinedInputs/StructuredListInputUi.cs:35` -- explicit TODO: `// TODO: Implement proper edit flags and Undo`

**What happens:** Editing structured list inputs (add/remove/reorder items) directly mutates the list without commands.

**Fix:** Create `ChangeStructuredListCommand` that snapshots the list state.

**Effort:** MEDIUM (new command, list serialization)

---

#### 14. Split Clip at Time (partial undo)

**File:** `Editor/Gui/Windows/TimeLine/TimeClips/LayersArea.cs:276-370`

**What happens:** "Cut at time" creates a copy of the clip and adjusts time ranges. Individual sub-operations use commands (CopySymbolChildrenCommand, ChangeSymbolChildNameCommand, MoveTimeClipsCommand) but some mutations happen outside commands (lines 332-334: direct TimeRange/SourceRange assignment on the NEW clip).

**Also:** Comment at line 273: `/// This command is incomplete and likely to lead to inconsistent data`

**Fix:** Wrap the entire operation in a `MacroCommand` and ensure all TimeRange mutations go through `MoveTimeClipsCommand`.

**Effort:** MEDIUM (1 day -- complex multi-step operation)

---

#### 15. Tour Point Editing (no undo)

**Files:**
- `Editor/Gui/Graph/Dialogs/EditTourPointsPopup.cs:166`
- `Editor/Gui/Graph/Dialogs/TourDataMarkdownExport.cs:241,245,319`

**What happens:** Creating/editing tour points and markdown export directly mutates `SymbolUi.Description` and tour point properties.

**Fix:** Lower priority -- tour editing is an authoring tool, not a frequent undo target. Could wrap in command if desired.

**Effort:** LOW (but low priority)

---

### LOWER PRIORITY -- Internal or edge-case mutations

#### 16. NodeActions.cs -- Command executed without UndoRedoStack

**File:** `Editor/UiModel/Modification/NodeActions.cs:236`
- FIXME comment: `cmd.Do(); // FIXME: Shouldn't this be UndoRedoQueue.AddAndExecute() ?`

**What happens:** A command is created and executed via `.Do()` directly instead of going through `UndoRedoStack.AddAndExecute()`, meaning it won't appear in the undo history.

**Fix:** Change `cmd.Do()` to `UndoRedoStack.AddAndExecute(cmd)`.

**Effort:** TRIVIAL (1 line change, but need to verify context)

---

#### 17. Auto-Layout / RecursivelyAlignChildren (no undo)

**File:** `Editor/Gui/Graph/Legacy/Interaction/NodeGraphLayouting.cs:58-60`

**What happens:** Auto-layout directly sets `childUi.PosOnCanvas` for all children.

**Fix:** Wrap in `ModifyCanvasElementsCommand`. Need to capture all positions before layout, apply layout, then store in command.

**Effort:** MEDIUM (half day)

---

#### 18. MagGraphLayout FlagAsModified without command

**File:** `Editor/Gui/MagGraph/Model/MagGraphLayout.cs:55`

**What happens:** `FlagAsModified()` called during layout size adjustments. These are internal layout recalculations, not user-initiated.

**Fix:** Likely not needed -- this is view-layer computation, not model mutation.

**Effort:** N/A

---

#### 19. FloatVectorInputValueUi direct FlagAsModified

**File:** `Editor/Gui/InputUi/VectorInputs/FloatVectorInputValueUi.cs:179`

**What happens:** `Parent?.FlagAsModified()` called from input UI code. Need to verify if this is inside a command flow or not.

**Fix:** Investigate -- may be a false positive if called within `ChangeInputValueCommand` flow.

**Effort:** INVESTIGATE (1 hour)

---

#### 20. SymbolLibrary namespace change

**File:** `Editor/Gui/Windows/SymbolLib/SymbolLibrary.cs:698`

**What happens:** `FlagAsModified()` called. Verify if wrapped in `ChangeSymbolNamespaceCommand`.

**Fix:** Investigate.

**Effort:** INVESTIGATE (1 hour)

---

## Summary Table

| # | Gap | Severity | Effort | Depends On |
|---|-----|----------|--------|------------|
| 1 | Insert/Remove Keyframe via UI | CRITICAL | LOW | -- |
| 2 | Duplicate Keyframes | CRITICAL | LOW-MED | -- |
| 3 | Delete Keyframes in Timeline | CRITICAL | LOW | -- |
| 4 | Insert Keyframe with Increment | CRITICAL | LOW | -- |
| 5 | Set Value as Default | CRITICAL | MEDIUM | -- |
| 6 | Variation CRUD | CRITICAL | MED-HIGH | -- |
| 7 | Curve Tangent Editing | CRITICAL | MEDIUM | -- |
| 8 | Parameter Extraction | CRITICAL | MEDIUM | -- |
| 9 | Edit Symbol Description/Links | MEDIUM | LOW-MED | -- |
| 10 | Edit Node Comment | MEDIUM | LOW | -- |
| 11 | Snapshot Enable Toggle | MEDIUM | MEDIUM | #6 |
| 12 | Playback Settings | LOW-MED | LOW-MED | -- |
| 13 | StructuredList Editing | MEDIUM | MEDIUM | -- |
| 14 | Split Clip at Time | MEDIUM | MEDIUM | -- |
| 15 | Tour Point Editing | LOW | LOW | -- |
| 16 | NodeActions cmd.Do() bypass | LOW | TRIVIAL | -- |
| 17 | Auto-Layout positions | LOW | MEDIUM | -- |
| 18 | MagGraphLayout internal | N/A | N/A | -- |
| 19 | FloatVectorInput (investigate) | ? | INVESTIGATE | -- |
| 20 | SymbolLibrary (investigate) | ? | INVESTIGATE | -- |

## Recommended Implementation Order

**Batch 1 -- Quick wins, highest user impact (2-3 days):**
- #1, #2, #3, #4 (all keyframe operations -- same pattern, commands already exist)
- #10 (edit comment -- trivial new command)
- #16 (NodeActions bypass -- one-line fix)

**Batch 2 -- Important gaps (2-3 days):**
- #5 (set as default -- new command)
- #7 (curve tangent editing -- use existing in-flight pattern)
- #8 (parameter extraction -- wrap in MacroCommand)
- #9 (symbol description -- new command)

**Batch 3 -- Complex features (3-5 days):**
- #6 (variation system -- needs careful design around persistence)
- #11 (snapshot toggle -- depends on #6)
- #13 (structured list -- new command with list snapshot)
- #14 (split clip -- fix existing partial implementation)

**Batch 4 -- Lower priority (as needed):**
- #12, #15, #17, #19, #20

## Testing Strategy

Each undo/redo fix should include a corresponding integration test in the command test project (see AUTOMATIC_TEST_PLAN.md Phase 1):

```csharp
[Fact]
public void InsertKeyframe_Undo_RemovesKeyframe()
{
    // Arrange: create symbol with animated input
    // Act: execute InsertKeyframeCommand
    // Assert: keyframe exists at time T
    // Act: undo
    // Assert: keyframe no longer exists at time T
}
```

This ensures that fixing undo/redo gaps doesn't introduce regressions, and that the fixes remain stable as the codebase evolves.
