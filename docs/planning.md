# Target Architecture / Product Requirements

## Domain Overview

Describe the product or system being built, who it serves, and the main workflow the agent must support.

## Data Sources / Requirements

Paste or summarize the raw schema, API contracts, sample payloads, business rules, and any must-have behaviours that define the scope.

## Success Criteria

**Define what "Done" looks like for the next set of runs.**
- What UI elements must be visible?
- What API endpoints must return specific data?
- What side-effects (database, filesystem) must be verified?

## Manual Verification Steps

**List the exact steps the human operator should take to verify the agent's work.**
1. Open `http://localhost:3000`.
2. Perform action X.
3. Verify outcome Y.

## Technical Constraints

List required environments, deployment constraints, coding standards beyond the default harness, performance limits, and any forbidden approaches.

## Preferred Stack

Specify the preferred languages, frameworks, libraries, databases, and tooling. If something is optional, say so explicitly.

## Skills

List optional harness skills to activate using one bullet per skill name from `~/.agents/skills/`.
Leave this section empty if no extra skills are needed.
Example format: `- frontend-design`

## Out of Scope

List features, integrations, migrations, or polish work that the agent should avoid during this run.
