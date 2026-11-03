<!--
Sync Impact Report
- Version: 1.0.0 -> 2.0.0 (MAJOR: the project language requirement is reversed)
- Reason: the team switched the project language from Romanian to English.
- Modified principles:
  - II. Object-Oriented, UI-Independent Engine: `OpResult` / `LoadResult` messages are in English.
  - III. Clear, Usable Avalonia Interface (MVVM): visible text, documentation and messages are in English.
- Modified sections: Architecture (`nr-inmatriculare.dfa.json` -> `license-plate.dfa.json`),
  Contracts and Ownership (`docs/plan/contracte/` -> `docs/plan/contracts/`),
  Workflow (example branch `p3/s-02-banda` -> `p3/s-02-tape`).
- Migration: contracts, plan and task files translated and renamed; contract messages and example
  ids translated; Romanian messages in `Dfa.Core` and its tests replaced.
- Follow-up TODOs: the theory docs (`docs/theory/`) are linked but not in the repository yet.
-->

# RoPlate Constitution

## Core Principles

### I. SOLID Design Is Mandatory

All production code MUST follow the SOLID principles. Responsibilities MUST be cohesive,
dependencies MUST be abstractions where substitution or isolation is required (for example
`INavigationService` and `IDialogService`), and changes MUST avoid forcing unrelated consumers
to depend on unused behavior. Exceptions require a written rationale in the change review. This
keeps the DFA engine, the editor, the simulator, and the RoPlate application maintainable as
they grow in parallel.

### II. Object-Oriented, UI-Independent Engine

Engine code MUST use OOP principles where behavior has state, lifecycle, or domain
responsibility. Classes MUST have clear responsibilities, encapsulation MUST protect
invariants (for example, determinism of δ is guaranteed by its `TransitionKey` dictionary), and
inheritance MUST be used only when a true substitutable relationship exists. `Dfa.Core` MUST
remain a class library with no reference to Avalonia or any other UI framework, so it can be
tested and reused without the interface. Operations that can fail because of user input MUST
return `OpResult` / `LoadResult` with an English message ready for display; exceptions are
reserved for programming errors.

### III. Clear, Usable Avalonia Interface (MVVM)

The interface MUST follow MVVM with `CommunityToolkit.Mvvm`: logic lives in ViewModels, and
code-behind is allowed only for drawing and mouse handling (`DfaCanvas`, `InputTape`). The UI
MUST modify the automaton only through `DfaDocument`. User flows MUST follow established UX
principles: clear hierarchy, predictable navigation, accessible controls, visible operation
states, actionable error messages, keyboard support, and no freezing on invalid data. States
and verdicts MUST NOT be conveyed by color alone (use thickness, double circles, ✓ / ✗).
All visible text, documentation and messages MUST be in English, and messages defined in the
contracts MUST be used verbatim. UI changes MUST be checked at the supported window sizes.

### IV. Small, Reusable, Non-Duplicated Code

Production C# source files MUST remain at or below 250 lines of code, excluding blank lines and
comments. XAML views SHOULD stay similarly focused and be split into user controls when they
grow. Shared behavior MUST have one authoritative implementation; duplicated logic MUST be
extracted or deliberately justified in review. Code MUST favor focused classes and simple
composition over oversized files or speculative abstractions. Assess this limit after normal
formatting. Developers MUST NOT compress multiple statements, remove useful whitespace, or
otherwise squeeze code to meet the limit. This limit is a design signal, not permission to split
code into meaningless fragments; when a cohesive class exceeds the limit after formatting,
refactor by responsibility or document a review exception.

### V. .NET Coding Conventions

All C# code MUST follow the standard .NET naming and layout conventions: `PascalCase` for
types, members, and constants, `camelCase` for locals and parameters, `_camelCase` for private
fields, one public type per file, and file-scoped namespaces matching the folder structure.
Class, method, and identifier names MUST be in English, as in the specification. Nullable
reference types MUST be enabled in every project, and new compiler or analyzer warnings MUST be
fixed rather than suppressed unless a written rationale is given in the change review.

### VI. Human-Readable Formatting

All source code MUST be formatted according to the repository's `.editorconfig` and pass
`dotnet format --verify-no-changes`. XAML and `.dfa.json` files MUST use consistent indentation
and remain readable by hand. Code MUST use clear structure, meaningful names, consistent
whitespace, and line wrapping that allows a reviewer to understand it without manual
reformatting. Formatting exceptions MUST be documented in the change review. Consistent
formatting keeps the codebase readable and prevents style-only differences from obscuring
behavior changes.

## Architecture and Scope Constraints

The solution MUST keep the layout defined in `docs/plan/README.md`: `src/Dfa.Core` (engine,
class library), `src/Dfa.App` (Avalonia application on .NET 10), and `tests/Dfa.Tests` (xUnit).
Dependencies flow one way: `Dfa.App` and `Dfa.Tests` reference `Dfa.Core`, never the reverse.
Example automata, including `license-plate.dfa.json`, MUST live in `Dfa.Core/Examples/` as
embedded resources so tests can load them without the UI. Objects are composed manually in
`MainViewModel`; no DI container is introduced without a contract change. Loading a file MUST
never throw on malformed input: errors are reported through `LoadResult`. Processing is
local-only, and source files are never modified unless the user saves.

### Contracts and Ownership

The contracts in `docs/plan/contracts/` (engine API, `.dfa.json` format, UI) are the
authoritative interface between team members. Public signatures, behavior rules, and messages
MUST match them. A contract changes only through a pull request that also updates the
corresponding `.md` file and is approved by the other two team members. Every folder has one
owner as listed in the plan; others modify it only through a pull request approved by that
owner. Until a dependency is ready, consumers work against stubs or data built in code.

## Development Workflow and Quality Gates

`main` MUST always build and pass all tests. Each task is developed on its own branch
(`p1/c-03-dfarun`, `p2/e-02-canvas`, `p3/s-02-tape`) and merged through a small pull request
reviewed by at least one other team member. Before opening a pull request, `dotnet build`,
`dotnet test`, and `dotnet format --verify-no-changes` MUST succeed. Every change MUST identify
its affected layer and verify the relevant constitution principles. New `Dfa.Core` behavior,
bug fixes, JSON reading and writing, and every example automaton (including the RoPlate
automaton A1) MUST have automated tests in `tests/Dfa.Tests` (xUnit + Shouldly), and line
coverage of `Dfa.Core` MUST stay at or above 70%. UI changes MUST include usability and
accessibility checks appropriate to the affected workflow. Reviews MUST reject unexplained
duplication, files over the size limit, missing tests for engine behavior, deviations from the
contracts, and UI changes that regress usability. Every commit MUST carry its real author so
each member's contribution is visible.

## Governance

This constitution supersedes conflicting project practices. Amendments MUST update this file,
prepare a Sync Impact Report for review, state the reason for the change, and preserve or
document any required migration. Record the report in the change review. Versioning follows
semantic versioning: MAJOR for incompatible governance changes or principle removals, MINOR for
new or materially expanded principles or sections, and PATCH for clarifications and
non-semantic wording changes.

Each change review MUST assess compliance with the constitution and record justified
exceptions. The team MUST review this constitution whenever architecture, the UI framework,
testing policy, or quality gates materially change. Unresolved governance gaps MUST be recorded
as explicit TODOs rather than silently ignored.

**Version**: 2.0.0 | **Ratified**: 2026-10-31 | **Last Amended**: 2026-11-01
