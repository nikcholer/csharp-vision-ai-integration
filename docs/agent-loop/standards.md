# Project Standards Placeholder

Populate this file inside each target repository before relying on it as an active standards source.

This scaffold is intentionally neutral. The harness runtime will read `docs/agent-loop/standards.md`, but the content should come from the project you are about to run, not from this harness repo's old sample rules.

## Purpose

Use this file to record project-specific delivery and coding rules that should constrain every agent run.

Good inputs include:
- house style guides,
- vendor or framework best-practice documents,
- architecture decision records,
- performance and security requirements,
- recurring debt patterns discovered in earlier trial repos.

## Recommended Sections

### 1. Non-Negotiable Constraints

List rules the agent must never violate.

Examples:
- required deployment/runtime targets,
- forbidden libraries or architectural patterns,
- security, privacy, or compliance boundaries,
- invariants around data migration or backward compatibility.

### 2. Coding Standards

List standards that are specific to this stack or codebase.

Examples:
- typing requirements,
- linting and formatting expectations,
- naming conventions,
- module layout rules,
- test coverage expectations,
- API or UI performance limits.

### 3. Verification Expectations

Document the baseline checks that should pass before a task is considered complete.

Examples:
- unit or integration test commands,
- lint/typecheck/build commands,
- smoke-test steps,
- manual verification steps for sensitive workflows.

### 4. Known Pitfalls

Capture repeated failure modes that future runs should avoid.

Examples:
- environment quirks,
- provider-specific CLI limitations,
- fragile files or generated artefacts,
- repo-specific rollback cautions.

## Optional Reference

If you want a concrete example of a populated standards file, see [standards.sample.md](standards.sample.md).
