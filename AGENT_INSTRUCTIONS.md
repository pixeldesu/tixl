# Agent Instructions for TiXL

## Mission
Contribute to **TiXL** with a focus on:
1. Correctness
2. Realtime performance
3. Code consistency with the existing codebase

## References
Use these sources first when behavior or conventions are unclear:
- Main documentation: https://github.com/tixl3d/tixl/wiki
- How TiXL works: https://github.com/tixl3d/tixl/wiki/help.HowTixlWorks
- Operator conventions: https://github.com/tixl3d/tixl/wiki/dev.OperatorConventions

If documentation and implementation differ, follow local code patterns in the affected project unless a task explicitly asks for broader refactoring.

## Solution Structure (Key Projects)
- `Core/` - Shared functionality between Editor and Player
- `Editor/` - Main user interface
- `Player/` - Exported project playback
- `Operators/Lib/` - Primary operators
- `Operators/TypeOperators/` - Operators for creating base types
- `Operators/Examples/` - Example operators and setups

Keep changes scoped to the smallest project boundary that solves the task.

## Realtime Performance Constraints (Critical)
TiXL uses realtime rendering and Dear ImGui. UI refresh is synchronized with output rendering, so slower frame times reduce UI responsiveness.

For methods called once per frame (e.g. operator update, Editor draw methods):
- Avoid heap allocations
- Avoid LINQ
- Prefer explicit loops and reusable buffers

Allocations are acceptable for explicit user-triggered actions.

## State and Resource Handling
- Avoid storing long-lived direct references to instances/resources.
- Prefer storing and resolving by `Guid`.
- Be careful with stale references across reloads and graph changes.

## Operator Rules
When changing operators, follow:
- https://github.com/tixl3d/tixl/wiki/dev.OperatorConventions

Also:
- Keep operator evaluation paths allocation-free
- Match existing naming and slot conventions
- Avoid hidden side effects unless explicitly intended

## Code Formatting and Style
- Put `return` statements on their own line (not inline after `if`)
- Place private fields and private enums at the end of classes
- Prefix private fields with `_`
- Prefer slightly longer, descriptive names when clarity improves (e.g. `faceIndex` over `i`)

## UI Implementation Guidelines
- Use `UiColor`/`UiColors` utilities instead of hard-coded float color vectors
- Use fonts sparingly; default to `Normal` and `Small`
- Prefer `CustomComponents` and `FormInputs` helpers for ImGui layout/input tasks before adding custom widget code

## Review and Quality Expectations
- Point out obvious problems, misleading code, incorrect implementations, and typos
- Fix spelling mistakes in touched comments on the fly
- Add parameter documentation only when parameter purpose is not obvious from the name

## Change Workflow
### Before editing
1. Check whether the target code runs every frame
2. Read nearby code for local conventions
3. Confirm correct project boundary (`Core`, `Editor`, `Operators`, etc.)

### During editing
1. Keep diffs minimal and targeted
2. Avoid opportunistic refactors unless requested
3. Preserve behavior unless task explicitly changes behavior
4. Respect existing contracts and nullability assumptions

### After editing
1. Build/check impacted projects
2. Verify no obvious regressions in hot paths
3. Call out assumptions and risks

## Review Checklist
- [ ] No new allocations or LINQ in per-frame paths
- [ ] No new long-lived direct references where `Guid` should be used
- [ ] Operator changes follow operator conventions
- [ ] Style rules above are followed
- [ ] Diff remains focused and minimal
- [ ] Any frame-time/UI-responsiveness risk is explicitly mentioned

## Communication Expectations
When reporting changes:
- Explain what changed and why
- Mention performance impact (or confirm none expected)
- Highlight tradeoffs and residual risks
- Keep follow-up suggestions actionable

