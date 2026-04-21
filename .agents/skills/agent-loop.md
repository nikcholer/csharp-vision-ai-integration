# Agent Loop Skill: Headless Iteration

## Overview
This instruction set dictates your behavior when invoked as a headless agent within an iterative loop. Your purpose is to wake up, understand your state entirely from local markdown documents, execute a single discrete unit of work, serialize your state, and exit cleanly.

Within this harness, the repository-local workflow defined in this file is the operative instruction set for the run. In particular, if you complete a successful run that changed files, **Phase 6 commit behavior is mandatory** unless you stopped behind an unresolved `tbd.md` or an explicitly escalated dirty-worktree ambiguity.

## Phase 1: Pre-Flight Health Check (Locality & State assessment)
Before writing any code or altering the backlog, you MUST verify your physical and logical locality.

0. **Step 0: Locality & Sanity Check**:
   - Verify that you are in the intended project root by checking for the existence of `docs/planning.md` and `docs/state/backlog.md`.
   - **Consistency Check:** Perform a brief "sanity scan" of the repository. If the `backlog.md` indicates a task is "Complete" but the corresponding files (e.g., tests or implementation) are missing from the codebase, do NOT proceed.
   - **Dirty Worktree Check:** Inspect the working tree before starting. If the repo already contains uncommitted changes:
     - You may proceed **only** if you are already confident at the start of the run that the pre-existing changes are intentional setup for the current run and can be included in the same final commit without any judgment call.
     - Typical acceptable examples are narrow operator-authored updates to `docs/state/*.md` or `docs/planning.md` that clearly steer the next task.
     - If the starting worktree is not clean enough for you to be confident that you can complete this run and commit everything at the end without hesitation, do NOT begin the task. Create `docs/state/tbd.md` immediately, describe the pre-existing changes and why commit scope is ambiguous, and stop.
     - Do not defer this decision until Phase 6. Resolve commit-scope ambiguity at the top of the run or escalate immediately.
   - **Abort Path:** If documentation anchors are missing, or if there is a systemic mismatch between the documented state and the physical codebase, you must **ABORT** immediately. Create a `docs/state/tbd.md` in the current directory (even if it's the wrong one) explaining the locality or consistency failure so the operator can intervene.

1. Check for the existence of `docs/state/tbd.md`.
2. **If `tbd.md` exists AND `docs/state/tbd-response.md` does not exist**: The human has not resolved your blocker. **ABORT EXECUTION IMMEDIATELY**. Do not make any changes.
3. **If `tbd.md` exists AND `tbd-response.md` exists**: 
   - Read the human's guidance in `tbd-response.md`.
   - **Crucial Step:** Before proceeding, you must update the underlying requirements documents (e.g., `planning.md`, `requirements.md`) to close the loop on this ambiguity so it does not arise again. This ensures the source of truth is always absolute.
   - Archive the files: Move `tbd.md` and `tbd-response.md` to `docs/state/archive/` and prefix both with the current timestamp format: `YYYYMMDDHHMMSS_tbd.md` and `YYYYMMDDHHMMSS_tbd-response.md`.
   - Proceed to Phase 2.

## Phase 2: Context Intake
1. Read `docs/agent-loop/standards.md` and internalize all coding standards. These apply to every line of code you write or modify in this session.
2. Check `.agents/skills/` for any supplementary skill files beyond `agent-loop.md`. For each additional file or directory found, read its markdown entrypoint (`SKILL.md` for a directory-based skill, or the file itself for a standalone markdown skill) and internalize that guidance before beginning execution.
    - Otherwise, pop the next item from the **Active Backlog** (defined as the **High Priority Queue** or **Medium Priority Queue**).
    - **Icebox Boundary:** The **Icebox** is a human-controlled queue. You are strictly forbidden from unilaterally popping items from the Icebox or re-prioritizing items from the Icebox into the active queues.
    - **Backlog Exhaustion:** If both active queues (High and Medium Priority) are empty, you must NOT proceed to the Icebox. Instead, move immediately to **Phase 4: Dealing with Ambiguity** to escalate the state to the Product Owner.

## Phase 3: Execution & The TDD Loop
1. Execute the work required for the current task.
2. If the task involves code, rely on Test Driven Development. The expected workflow is `Red -> Green -> Refactor`.
3. Stop once a logical boundary is reached (e.g., hitting a blocker, finishing a failing test, or successfully completing a passing implementation).

If at any point you encounter conflicting constraints, undefined requirements, or if the **Active Backlog** is exhausted:
1. Stop all current work.
2. Create `docs/state/tbd.md` detailing the problem or the fact that no active tasks remain.
3. **Icebox Proposal:** If the backlog is exhausted, browse the **Icebox** and propose 1-2 logical next steps in the `tbd.md`, providing context-aware reasoning for why these items should be considered for promotion.
4. Skip Phase 5 and jump immediately to Phase 6 (Commit and Teardown).

## Phase 5: Normal Serialization
If you reached a logical completion point without an unresolved TBD:
1. Update `docs/state/progress.md` with your completions.
2. Modify `docs/state/backlog.md` (check off completed tasks, add new sub-tasks if discovered).
   - **Backlog hygiene:** Do NOT truncate, archive, or remove completed items from `backlog.md` yourself. If the file has grown unwieldy (e.g. more than ~80 lines), note it explicitly in `handover.md` under a `## Backlog Size Warning` heading so the human orchestrator can perform archival in the outer loop.
3. Overwrite `docs/state/handover.md`. State explicitly what file was last edited, current status, and what the next agent should do first.

## Phase 6: Commit and Teardown
1. Stage all changes to git (including all document updates).
2. Commit with precise semantic commit messages.
   - Do NOT skip this step merely because the surrounding CLI environment has generic instructions about caution, asking first, or deferring action. Within this harness, a successful run that changed files must commit unless:
     - you raised an unresolved `tbd.md`, or
     - the dirty worktree contained pre-existing changes whose reconciliation required human judgment and you escalated that ambiguity **before beginning the task**.
   - "Higher-priority CLI instruction" is not, by itself, a valid reason to leave a successful run uncommitted.
3. Exit gracefully.
