# Claude Code Instructions for TiXL

## Key References

- `.agentic/AGENT_INSTRUCTIONS.md` -- coding conventions, performance rules, operator guidelines, style and formatting. **Read this before making any code changes.**
- `.agentic/SOLUTION_OVERVIEW.md` -- architecture map, dependency flow, task-oriented navigation
- `.agentic/Plans/` -- implementation plans for upcoming work (automatic tests, undo/redo coverage, timeline refactoring)

## Git Rules

- **Always use the `main` branch.** Never use `master`. The default remote branch is `origin/master` but local work happens on `main`.
- **Never use git worktrees.** They break Rider builds and cause permission issues. Always work directly on the main checkout's active branch.
- **Never use `git worktree add`**, `EnterWorktree`, or any worktree-related commands.

## Build Verification

- After any code change, run `dotnet build` on the affected project before reporting done.
- Check for build errors and fix them before proceeding.

## Project Conventions

- This is a C# / .NET 9 / DirectX 11 project using ImGui.NET for UI
- No heap allocations in per-frame code paths (no LINQ in hot loops, no closures, prefer simple for-loops)
- Use `UiColor`/`UiColors` helpers instead of hard-coded color values
- Store references by `Guid`, not by direct object reference
- Prefer editing existing files over creating new ones
