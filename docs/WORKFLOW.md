# Prototype workflow

Follow these phases in order for every prototype. Each phase ends with a commit. Read `CLAUDE.md` first.

## Lead agent and parallel work

The owner wants results fast, so the work is split up as much as it safely can be and run at the same time.

- The agent the owner starts is the **lead** (project manager). It plans each phase, hands tasks to workers (subagents), tracks them, and reports to the owner. It does as little hands-on work as it can.
- In every phase, the lead breaks the work into the **largest set of independent tasks** and starts them all at once. It runs tasks one after another only when one truly needs another's result.
- Only three things are always sequential: the owner's GDD review (phase 1), the shared interface stubs (phase 2), and anything that needs the Unity Editor (only the integrator holds it).
- **One owner per file.** Two tasks running at the same time never edit the same file. A scene is one file, so each level area gets its own prefab, builder or additive scene (see phase 3).
- **The task brief stands alone.** Each worker gets: what to build, the files and folders it owns, the interfaces it can rely on, the GDD sections and references that apply, and how its result is checked. Workers don't need the lead's conversation.
- Each code-writing worker uses its own branch (git worktree), so parallel work never collides on disk.
- No fixed cap on workers: run as many as there are independent tasks. If there are more than about eight, start the ones the most other work depends on first, then continue in waves.
- The lead keeps `docs/WORKPLAN.md` current: every task, its owner, its status, and what it is waiting on.

## Model and effort per phase

Spend reasoning where it sets direction, not where the spec already decides. A mistake in the GDD or the architecture is built on by every worker; a mistake in one worker's code is caught by its test. The lead sets `effort` (and `model`, where the owner's setup allows it) on each subagent it starts, and asks the owner to run the lead itself at high effort on the strongest model available.

| Phase | Who | Effort | Model |
| --- | --- | --- | --- |
| 1. GDD (writer, and reference-image readers) | lead + subagents | high | strongest available |
| 2. Architecture and interface stubs | lead + one subagent | high | strongest available |
| 3. Fan-out workers | subagents | medium (default) | a faster model is fine; the task brief and the failing test carry the quality |
| 3. A worker whose task is judgment-heavy (level layout, a puzzle, anything the GDD describes in feel words) | subagent | high | strongest available |
| 4. Integrate (merging, reading failures, judging screenshots against the pitch) | integrator | high | strongest available |
| 5. Polish | integrator + subagents | high | strongest available |
| 6. Owner review (turning taste notes into tasks) | lead | high | strongest available |

- Never lower effort to save time in phases 1, 2 or 4: the owner has credits to spend and would rather pay for a correct GDD than for a second fan-out.
- If a worker fails its done check twice, re-run it at high effort before splitting the task further.
- Record the models actually used in `docs/WORKPLAN.md` next to each task, so a weak result can be traced to its settings.

## 1. GDD
- One agent expands `docs/BRIEF.md` into `docs/GDD.md` using `docs/GDD_TEMPLATE.md`, so the design reads as one voice.
- The groundwork can run in parallel first: for example, one subagent per group of reference images, each returning a written summary of what its images show and how the owner's notes apply. The GDD writer works from those summaries and still opens any image it needs.
- Before writing, it reads `docs/references/REFERENCES.md` and opens **every** image in `docs/references/` (Read tool). The owner's notes win over the agent's own reading: take what **Pay attention to** names, and never turn anything under **Ignore** into a requirement.
- It fills the GDD's **References** section with what each image contributes, in words specific enough that later agents can work from the GDD alone. It also writes each image's details into the section they shape (art direction, layout, features), naming the image.
- If an image has no notes, or the notes contradict the brief, ask the owner before writing the GDD.
- Every feature gets an acceptance test line (setup -> action -> assert).
- **Stop for owner review.** Do not start phase 2 until the owner approves the GDD.

## 2. Architecture
- One agent writes `docs/ARCHITECTURE.md` with:
  - the systems list
  - each system's public interface: the events it publishes and subscribes to, its `ConfigAsset` subclass, and its components
  - each system's owning folder under `Assets/_Project/Gameplay/<System>/`
  - the level areas, each with its own prefab, builder or additive scene, so areas can be built in parallel
- Stub every interface: event structs, config classes, empty components, and one failing test per feature. Compile, then commit before fan-out. The stubs are what lets every worker build at the same time against fixed interfaces.
- Write `docs/WORKPLAN.md`: one row per task, split as finely as the interfaces allow (systems, level areas, puzzles, UI, audio stubs). Each row gives the files and folders it owns, what it depends on, and its done check (the GDD acceptance test it must pass). Two tasks may run at the same time only if they own no files in common.

## 3. Fan-out
- The lead starts one worker per independent task in `docs/WORKPLAN.md`, all at once (or in waves, see above). Tasks are not only systems: level areas, puzzles and content are tasks too.
- Workers work from the GDD's References section. They may open an image in `docs/references/` when their system needs a detail the GDD doesn't spell out, and they still follow the owner's notes in `REFERENCES.md`.
- Each worker stays inside the files and folders its task owns (usually its own `Gameplay/<System>/` folder, or its level area's builder and prefab) and writes C#, prefab YAML, `ConfigAsset` files and tests.
- Workers never open the Editor and never call MCP (see the concurrency rule in `CLAUDE.md`).
- Need something outside your folder? Add it to `BACKLOG.md` and tell the lead, who makes it a new task.

## 4. Integrate
- Integrate as work lands, not after everyone finishes: merge and check each worker's branch as soon as it is done, while the others keep working.
- One integrator owns the Editor and the MCP session, and does the following:
  1. Merges the workers' branches.
  2. Triggers a compile (`refresh_unity`) and reads the console (`read_console`).
  3. Runs edit-mode and play-mode tests (`run_tests`, then `get_test_job`).
  4. Enters play mode from `Boot` (`manage_editor` play) and plays the 60-second pitch.
  5. Screenshots each state named in the GDD (`manage_camera` screenshot) into `docs/`.
- Return each failure to the worker who owns it, with the exact error text, then repeat until clean. Fixes for different workers run in parallel.

## 5. Polish
- Feel-only changes: no new features, no new systems.
- The lead splits the checklist below into tasks and runs those that touch different files at the same time (for example particles, audio stubs and menu tweens). The integrator checks each one in the Editor. Commit each item separately.

## 6. Owner review
- The owner gives taste notes as deltas against the GDD (for example, "apex time 0.5 s" or "fog 30 m closer").
- Notes may point at a reference ("fog like courtyard-dusk.jpg"). Re-open that image, and add any new detail to its section in `REFERENCES.md` and to the GDD so the next agent has it.
- The lead splits the notes into tasks that can run at the same time; the integrator applies config changes (mostly `Config/` assets) and updates `docs/GDD.md` to match.

## Polish checklist

- [ ] Tween on every state change
- [ ] Hit-stop / camera shake on impacts where relevant
- [ ] Particles on impacts and pickups
- [ ] Audio stubs on every action (placeholder clips allowed)
- [ ] Main menu, pause, game-over, restart all reachable
- [ ] Post-processing profile tuned to the GDD palette
- [ ] Title shown on boot
- [ ] Zero console errors or warnings in a full play-through
