# Prototype workflow

Follow these phases in order for every prototype. Each phase ends with a commit. Read `CLAUDE.md` first.

## 1. GDD
- One agent expands `docs/BRIEF.md` into `docs/GDD.md` using `docs/GDD_TEMPLATE.md`.
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
- Stub every interface: event structs, config classes, empty components, and one failing test per feature. Compile, then commit before fan-out.

## 3. Fan-out
- One worker per system, four to six workers maximum.
- Workers work from the GDD's References section. They may open an image in `docs/references/` when their system needs a detail the GDD doesn't spell out, and they still follow the owner's notes in `REFERENCES.md`.
- Each worker stays inside its own `Gameplay/<System>/` folder and writes C#, prefab YAML, `ConfigAsset` files and tests.
- Workers never open the Editor and never call MCP (see the concurrency rule in `CLAUDE.md`).
- Need something outside your folder? Add it to `BACKLOG.md` and tell the integrator.

## 4. Integrate
- One integrator owns the Editor and the MCP session, and does the following:
  1. Merges the workers' branches.
  2. Triggers a compile (`refresh_unity`) and reads the console (`read_console`).
  3. Runs edit-mode and play-mode tests (`run_tests`, then `get_test_job`).
  4. Enters play mode from `Boot` (`manage_editor` play) and plays the 60-second pitch.
  5. Screenshots each state named in the GDD (`manage_camera` screenshot) into `docs/`.
- Return each failure to the worker who owns it, with the exact error text, then repeat until clean.

## 5. Polish
- One agent makes feel-only changes: no new features, no new systems.
- Work through the checklist below and commit each item separately.

## 6. Owner review
- The owner gives taste notes as deltas against the GDD (for example, "apex time 0.5 s" or "fog 30 m closer").
- Notes may point at a reference ("fog like courtyard-dusk.jpg"). Re-open that image, and add any new detail to its section in `REFERENCES.md` and to the GDD so the next agent has it.
- The integrator applies the notes, mostly by editing `Config/` assets, and updates `docs/GDD.md` to match.

## Polish checklist

- [ ] Tween on every state change
- [ ] Hit-stop / camera shake on impacts where relevant
- [ ] Particles on impacts and pickups
- [ ] Audio stubs on every action (placeholder clips allowed)
- [ ] Main menu, pause, game-over, restart all reachable
- [ ] Post-processing profile tuned to the GDD palette
- [ ] Title shown on boot
- [ ] Zero console errors or warnings in a full play-through
