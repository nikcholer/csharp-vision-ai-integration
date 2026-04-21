# Outer Loop Playbook

The **outer loop** is everything that happens between individual agent runs: the human product owner, the orchestration chat, and any tooling that kicks off the next CLI invocation. This document records the recurring responsibilities of the outer-loop operator.

> **Note for agents:** This document describes duties performed by the human orchestrator between runs. It does not modify or supersede any phase in `skill.md`.

---

## First-Time Bootstrap

If you are starting from a new project idea, use `init-trial.ps1` to create a scaffolded trial repo and work there.

If you are taking over an existing repository, prefer adopting the harness in place. Do not copy the target codebase into a wrapper folder unless you know the project is location-independent and want an isolated clone.

For an existing repo, bootstrap it like this before the first run:
1. Copy `docs/agent-loop/skill.md` from this harness repo into `.agents/skills/agent-loop.md` in the target repo.
2. Copy `docs/agent-loop/outer-loop-playbook.md` into `docs/agent-loop/` in the target repo.
3. Copy `docs/agent-loop/standards.md` into `docs/agent-loop/` in the target repo.
4. Populate `docs/agent-loop/standards.md` with the target project's actual standards.
5. If useful, consult `docs/agent-loop/standards.sample.md` as a reference example rather than as a default.
6. Copy the files from `docs/agent-loop/templates/` into the target repo.
7. Create `docs/state/` and `docs/state/archive/`.
8. Seed `docs/state/handover.md`, `docs/state/backlog.md`, and `docs/state/progress.md` from the matching templates.
9. Create `docs/planning.md` from the planning template and fill it with the current architecture, supplied artefacts, constraints, current known priorities, and out-of-scope items.
10. Review the seeded backlog and handover files so the first run starts from intentional context rather than generic placeholders.

If `docs/` or `.agents/` already exists in the target repo, merge these files into the existing structure rather than renaming project folders or moving the codebase.

---

## State Archival

**Trigger:** The agent flags `## Backlog Size Warning` in `handover.md`, or the active `backlog.md` exceeds roughly 80 lines.

**Why the agent does not do this itself:** The agent is scoped to a single atomic task. Rewriting its own state files is a meta-operation that risks silently discarding context. The human orchestrator has better visibility of what is truly done and safe to archive.

### Backlog Procedure

1. Open `docs/state/backlog.md`.
2. Identify all fully-completed sections. These are sections where every item, including all sub-items, is marked `[x]`. Partially-completed sections stay in the active backlog.
3. Create or append to `docs/state/backlog-archive.md`. Prepend each archived section with a datestamp:
   ```markdown
   ## [Archived: YYYY-MM-DD] High Priority Queue
   ...
   ```
4. Delete the archived sections from `backlog.md`, keeping only the active or incomplete sections.
5. Commit the pair of changes together:
   ```
   chore(state): archive completed backlog sections [YYYY-MM-DD]
   ```

### Progress Procedure

Archive `docs/state/progress.md` on the same trigger as backlog archival so the active progress log stays scoped to the current sprint.

1. Open `docs/state/progress.md`.
2. Identify completed sprint sections that are no longer part of the active workstream.
3. Create or append to `docs/state/progress-archive.md`. Prepend each archived sprint section with a datestamp:
   ```markdown
   ## [Archived: YYYY-MM-DD] Current Sprint / Milestone
   ...
   ```
4. Delete the archived sprint sections from `progress.md`, leaving only the current sprint's entries in the active file.
5. If backlog archival is happening in the same pass, commit all state-file changes together:
   ```
   chore(state): archive completed backlog and progress sections [YYYY-MM-DD]
   ```

If only `progress.md` needs archival, commit the pair of progress-file changes together:
```
chore(state): archive completed progress sections [YYYY-MM-DD]
```

---

## Steering the Next Sprint

At any point between runs, the human orchestrator may:

- inject new backlog items directly into `backlog.md` under a new section labelled `(Added by Human)`,
- redirect the next run by editing `## Primary Immediate Next Step` in `handover.md`,
- add standards or design guidance by editing `docs/agent-loop/standards.md`,
- write a TBD response by creating `docs/state/tbd-response.md` if the agent has paused for human input.

---

## Starting an Agent Run

Run the following from the root of the target repo.

### OpenAI Codex

```powershell
codex exec -m gpt-5.4 --dangerously-bypass-approvals-and-sandbox "Read .agents/skills/agent-loop.md and execute the next run strictly from the repository's local markdown state."
```

### Gemini CLI

```powershell
gemini -m gemini-2.5-pro-preview --prompt "Read .agents/skills/agent-loop.md and execute the next run strictly from the repository's local markdown state." --approval-mode yolo
```

Equivalent short form:

```powershell
gemini -m gemini-2.5-pro-preview -p "Read .agents/skills/agent-loop.md and execute the next run strictly from the repository's local markdown state." -y
```

### Aider

To run Aider non-interactively (`--yes` to auto-approve edits and commits, `-m` to execute a single prompt and exit), use the following syntax:

```powershell
aider -m "Read .agents/skills/agent-loop.md and execute the next run strictly from the repository's local markdown state." --yes --model <provider>/<model>
```

> **Note on Custom Endpoints:** For custom API routes (e.g., Together.ai, OpenRouter), it is strongly recommended to configure your endpoint strictly within a localized `.aider.conf.yml` file rather than mutating your shell environment. If you do override `OPENAI_API_BASE` and `OPENAI_API_KEY` in your terminal, ensure you unset both immediately afterward (`Remove-Item Env:\OPENAI_API_BASE, Env:\OPENAI_API_KEY`). Leaving them exported will silently hijack OpenAI traffic when using other CLI tools. Use `--no-show-model-warnings` to suppress browser pop-ups for unrecognized models.

### OpenCode

If you prefer a Node.js-based agent framework, configure your credentials interactively (`opencode auth login`), set your default model, and execute purely headlessly:

```powershell
opencode run -m "<provider>/<model_name>" "Read .agents/skills/agent-loop.md and execute the next run strictly from the repository's local markdown state."
```


### Operating Rule

Use whichever provider is appropriate for the next run. The provider choice is not part of the harness state model. The important invariant is that the next agent reads the same local markdown context and performs the next bounded task.

After any run:
- check `git status`,
- confirm the state files reflect the completed work,
- verify that the commit actually landed before starting the next loop.

If a provider fails to commit automatically, commit manually with a precise semantic message before continuing.

If an agent claims it left a successful run uncommitted due only to a generic "higher-priority CLI instruction," treat that as a harness-execution failure rather than a valid outcome. The runtime skill requires a commit unless the run stopped behind an unresolved `tbd.md` or an explicitly escalated dirty-worktree ambiguity.

### Dirty Worktree Rule

Before starting a run, prefer a clean working tree.

If you intentionally leave uncommitted changes for the next run, keep them narrow and obviously related to the next task. Examples include backlog steering, handover edits, or other operator-authored state updates.

The runtime skill now requires the agent to make that judgment at the **start** of the run, not at the end. If the worktree is not clean enough for the agent to be confident it can commit the run cleanly, it should stop immediately and publish the concern in `docs/state/tbd.md`.

Operationally, that means:
- prefer starting every run from a clean `git status`,
- if you must leave uncommitted operator edits behind, keep them limited to obvious state/planning guidance,
- if the repo is dirty in any way that would make the final commit scope debatable, expect the agent to abort early via `tbd.md`.

---

## Project-Specific Gotchas

Capture environment-specific failures in the target repo once they have repeated often enough to deserve standing operator guidance.

Examples:
- shell or encoding quirks,
- provider-specific tool approval behaviour,
- fragile generated files,
- slow or flaky verification commands,
- rollback cautions for sensitive migrations or stateful systems.

Avoid turning this harness repo into a catalogue of one stack's historical edge cases. Keep target-project gotchas with the target project.

---

## Periodic Standards Review

After several trial projects, review the populated `docs/agent-loop/standards.md` files and archived backlog patterns across those repos. If a new class of debt appears repeatedly, codify it into your house standards or use it to seed future project-specific standards files.

---

## Milestone Commits

At the end of a natural milestone, tag the repo:
```
git tag -a v0.x.0 -m "Milestone: <short description>"
```

This gives a clean rollback point and makes the git log readable as a project narrative.

---

## Rollback After a Bad Agent Run

If an agent run produces incorrect code, corrupts state files, or moves the backlog in the wrong direction, recover the repo before starting the next loop.

### Use `git revert` When History Must Stay Intact

Use `git revert` when the bad agent commit has already been pushed, shared, or is part of history you want to preserve as an auditable trail.

1. Identify the bad commit:
   ```
   git log --oneline -n 10
   ```
2. Revert it with a new commit:
   ```
   git revert <bad-commit>
   ```
3. If the bad run spans multiple consecutive commits, revert the range in one pass:
   ```
   git revert --no-commit <oldest-bad-commit>^..<newest-bad-commit>
   git commit -m "revert: undo bad agent run"
   ```

### Use `git reset --hard` When Discarding Local History Is Safe

Use `git reset --hard` only when the bad agent commit is still local, no one else depends on it, and you want to erase it completely rather than record a compensating commit.

1. Confirm the last known good commit:
   ```
   git log --oneline -n 10
   ```
2. Reset the branch back to that commit:
   ```
   git reset --hard <good-commit>
   ```

### Restore `handover.md` Before Restarting the Loop

The next run should inherit the last good context, not the bad run's summary.

- If you used `git reset --hard`, `docs/state/handover.md` is restored automatically with the rest of the tree.
- If you used `git revert`, verify that `docs/state/handover.md` now reflects the restored context. If it is semantically stale, rewrite it before continuing.
- If needed, restore only the handover from a known-good commit:
  ```
  git checkout <good-commit> -- docs/state/handover.md
  ```

### Re-Establish the Baseline Before the Next Run

- Rerun the target repo's normal verification command or commands before starting another loop.
- If the bad run touched only markdown state, rerunning all checks may be optional, but still recommended when there is any doubt.
- Do not resume the loop until the repo is both clean in `git status` and the relevant baseline checks are green.
