<!--
Sync Impact Report
- Version change: unversioned -> 1.0.0
- Modified principles:
  - Template Principle 1 -> I. Code Quality & Maintainability
  - Template Principle 2 -> II. Consistent Naming
  - Template Principle 3 -> III. Mandatory Automated Testing
  - Template Principle 4 -> IV. Documentation Completes the Feature
  - Template Principle 5 -> V. Secure Vertical Slices
- Added sections:
  - Engineering Constraints
  - Delivery Workflow
- Removed sections:
  - None
- Templates requiring updates:
  - ✅ /tmp/workspace/dilillo/Barberslop/.specify/templates/plan-template.md
  - ✅ /tmp/workspace/dilillo/Barberslop/.specify/templates/spec-template.md
  - ✅ /tmp/workspace/dilillo/Barberslop/.specify/templates/tasks-template.md
- Follow-up TODOs:
  - None
-->
# Barberslop Constitution

## Core Principles

### I. Code Quality & Maintainability
All production code MUST be clean, readable, and maintainable. Contributors MUST
prefer simple designs, apply SOLID principles when they improve modularity, and
avoid unnecessary abstraction, duplication, and incidental complexity.

Rationale: maintainable code reduces defects, keeps onboarding costs low, and
allows safe iteration as the project evolves.

### II. Consistent Naming
Variables and functions MUST use camelCase. Classes, types, and other named
constructs that represent types or components MUST use PascalCase. Names MUST be
descriptive, consistent with the surrounding domain, and stable across a slice.

Rationale: consistent naming lowers cognitive load and makes code review,
refactoring, and cross-slice navigation predictable.

### III. Mandatory Automated Testing
Every feature MUST include automated tests that cover its expected behavior and
edge cases. Feature work is not complete unless the relevant automated test suite
passes and the feature achieves at least 90% coverage of the delivered behavior,
or a stricter repository threshold if one is defined later.

Rationale: mandatory automated testing prevents regressions and makes the
project's non-negotiable quality bar measurable.

### IV. Documentation Completes the Feature
Every public API MUST include clear docstrings and at least one usage example in
the most appropriate documentation surface. Behavior changes that affect users or
integrators MUST update the related documentation in the same change.

Rationale: accurate documentation is part of the shipped interface, not an
optional follow-up task.

### V. Secure Vertical Slices
Features MUST use Vertical Slice architecture where applicable so that each slice
contains its own behavior, boundaries, and tests. All inputs MUST be validated,
all outputs MUST be sanitized where needed, and security-sensitive work MUST
follow current OWASP best practices.

Rationale: vertical slices preserve cohesion, while explicit security controls
reduce avoidable vulnerabilities at the feature boundary.

## Engineering Constraints

- **Performance**: Contributors MUST optimize for clarity first and only pursue
  performance work when justified by measurable need. Performance changes MUST
  state the metric or scenario they protect.
- **Versioning**: Public releases, packages, and contract changes MUST follow
  semantic versioning using `MAJOR.MINOR.PATCH`.
- **Development Environment**: New scripts, tools, and contributor workflows MUST
  be runnable in a Microsoft Windows desktop operating system environment. Changes
  MUST not require unsupported POSIX-only tooling when a contributor workflow is
  expected to run locally.

## Delivery Workflow

- Specifications, plans, and task lists MUST explicitly address testing,
  documentation, security, architecture, performance, versioning impact, and
  Windows compatibility.
- Pull requests MUST provide evidence of automated testing and document any API,
  security, or versioning implications introduced by the change.
- Any exception to this constitution MUST be documented in the relevant plan or
  pull request and approved by a maintainer before merge.

## Governance

This constitution is the highest-priority project policy for Barberslop and
supersedes conflicting local practices or unchecked convenience decisions.

Amendments MUST be made in the same change set as any required updates to related
templates, automation guidance, or contributor-facing documentation. Every
amendment MUST include a clear rationale and a sync impact report.

Versioning policy for this constitution follows semantic versioning:
- **MAJOR**: Remove a principle, materially weaken a requirement, or redefine
  governance in a backward-incompatible way.
- **MINOR**: Add a principle, add a mandatory section, or materially expand
  project obligations.
- **PATCH**: Clarify wording, improve structure, or make non-semantic edits.

Compliance review is required for every pull request. Reviewers MUST verify that
new work satisfies the constitution's quality, naming, testing, documentation,
security, architecture, versioning, performance, and Windows-environment rules.

**Version**: 1.0.0 | **Ratified**: 2026-05-26 | **Last Amended**: 2026-05-26
