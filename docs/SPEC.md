# Unity Prototype-Kit: Build Spec for the Setup Agent

Nov 2, 2026 · @Griffin

## Inputs to confirm before starting

The owner fills in this table before handoff. If any row is still blank, ask once, then use the default in the right-hand column and record it in `docs/DECISIONS.md`.

| Input | Value | Default if blank |
| --- | --- | --- |
| Unity version | **6000.6.4f1** | Latest LTS installed on the machine (Unity 6 LTS preferred) |
| Repo location / remote | Local: `C:\Unity Projects\<name>`. Remote: a new GitHub repo under `GriffJamesK` | `~/Projects/unity-prototype-kit`, local git only |
| 3D, 2D, or both | 3D | 3D (URP). 2D camera rig variant is optional |
| Target platform | Windows PC: a standalone 64-bit build | Windows standalone; keep settings platform-neutral |
| Unity MCP server in use (name + how it is launched) | **MCP for Unity**, package `com.coplaydev.unity-mcp` (v10.3.0), added in Packages/manifest.json. It’s registered in `.mcp.json` as **UnityMCP** at `[http://127.0.0.1:8080/mcp](http://127.0.0.1:8080/mcp)`. To launch it, in Unity open **Window > MCP for Unity > Connect tab > Start Server** (transport: HTTP Local). It runs through `uvx`; Python is installed via uv, so ignore the setup wizard showing Python as red. | Detect from the existing Claude Code config; do not install a new one |
| MCP tool names for: run tests, read console, enter/exit play mode, screenshot | **Read console:** `read_console`. **Enter/exit Play:** `manage_editor` (play/stop actions). **Run tests:** `run_tests`, then `get_test_job` to read the result. **Screenshot:** no dedicated tool; call ScreenCapture.CaptureScreenshot(path) through execute\_code, then read the PNG from disk. Also handy: `execute_code` (run any editor C#), `refresh_unity` (recompile) and `manage_scene` (save the scene). | Read from the MCP server's tool list and record them in `CLAUDE.md` |
| Input: new Input System only, or also legacy | New Input System only, no legacy. | New Input System only |
| Existing conventions to carry over (naming, folder habits) | • **Folders:** use the layout in the Repository layout section (it supersedes the previous project's `Assets/Scripts/<Area>/`; game code goes in `Assets/_Project/Gameplay/<System>/`). Carried over on top of it: `_Project/Editor/` for builders and editor tools, `_Project/Shaders/`, `_Project/Generated/` for script-made meshes, and a root `Tools/` folder for offline tools such as the compile check. `Logs/` and `Temp/` stay gitignored.<br>• **Big classes:** split into `partial` files named `ClassName.Area.cs` (for example `Player.Jump.cs`) at about 800 lines. Fields and Unity messages stay in the main file.<br>• **Test hooks:** static debug classes named `<System>Debug` with methods like `Report()`, called through MCP.<br>• **Self-installing systems:** runtime systems set themselves up with `[RuntimeInitializeOnLoadMethod]`, so they don’t need to be placed in the scene.<br>• **Comments:** a `<summary>` on classes and non-trivial methods saying *why*, in plain English.<br>• **Root files:** `CLAUDE.md` (the project guide) and `BACKLOG.md` (pending requests, plus finished items under Done with dates). | None; use the conventions in this spec |

## Task

Build a reusable Unity template repository ("prototype-kit") that future agents clone to go from a one-paragraph brief to a playable, polished prototype with minimal setup. Everything in this spec is a requirement unless marked optional. Where this spec is silent, choose the simplest option that works with the confirmed Unity version, and record the choice in `docs/DECISIONS.md`.

**Done means:** a fresh clone opens in the Unity Editor with zero console errors or warnings, compiles, passes all included tests, and a new agent can read `CLAUDE.md` alone and know how to build a prototype in it.

Work in this order: confirm inputs → project + packages → folder layout and asmdefs → Core library → scenes/prefabs/materials/UI → tests → `CLAUDE.md` and `docs/` → run the acceptance checklist → commit.

## Repository layout

Create exactly this structure. Every folder under `Assets/_Project` that holds C# gets its own asmdef (next section).

```
unity-prototype-kit/
  CLAUDE.md
  BACKLOG.md                  # pending requests; finished items under Done with dates
  .gitignore                  # standard Unity gitignore + .claude/ worktree junk
  .gitattributes              # LFS for .png .wav .fbx .psd
  .mcp.json                   # UnityMCP registration (HTTP local)
  Tools/                      # offline tools, e.g. compile check script
  docs/
    GDD_TEMPLATE.md
    WORKFLOW.md
    DECISIONS.md
  Assets/
    _Project/
      Core/                   # engine-agnostic helpers (asmdef: Kit.Core)
      Gameplay/               # per-prototype systems, one subfolder each (asmdef: Kit.Gameplay)
      UI/                     # canvas, menus (asmdef: Kit.UI)
      Input/                  # .inputactions + generated wrapper (asmdef: Kit.Input)
      Editor/                 # builders, editor tools (asmdef: Kit.Editor, editor-only)
      Config/                 # ScriptableObject tunables (.asset files)
      Prefabs/
        Camera/
        Player/
        UI/
      Materials/
      Shaders/
      Generated/              # meshes and assets produced by scripts
      Scenes/
        Boot.unity
        Sandbox.unity
      Settings/               # URP assets, Volume profiles
      Tests/
        EditMode/             # asmdef: Kit.Tests.EditMode
        PlayMode/             # asmdef: Kit.Tests.PlayMode
    ThirdParty/               # anything imported, never edited
  Packages/manifest.json      # includes com.coplaydev.unity-mcp
  ProjectSettings/
```

Rules: `_Project` prefix keeps project assets sorted above packages. Nothing goes at `Assets/` root. Never hand-edit `.meta` files; let the Editor generate them and commit them.

## Packages and project settings

Install via `Packages/manifest.json` (pin to the versions that ship with the confirmed Unity version):

| Package | Purpose | Required |
| --- | --- | --- |
| `com.unity.render-pipelines.universal` | URP | Yes |
| `com.unity.inputsystem` | New Input System | Yes |
| `com.unity.test-framework` | Edit/play-mode tests | Yes |
| `com.unity.textmeshpro` (or built-in UGUI text in Unity 6) | UI text | Yes |
| `com.unity.cinemachine` | Camera rigs | Yes |
| `com.unity.probuilder` | Blockout geometry | Optional |

Project settings to apply:

- Active Input Handling: **Input System Package (New)** only.
- Graphics: URP asset from `Assets/_Project/Settings/` assigned in Graphics and Quality settings. One URP asset, one Renderer, with post-processing enabled.
- Color space: Linear.
- Scripting backend: Mono (fast iteration); API compatibility .NET Standard 2.1.
- Enter Play Mode Options: enable, with Domain Reload and Scene Reload **off** (fast play-mode iteration). `Core` must therefore avoid static state, or reset it via `[RuntimeInitializeOnLoadMethod]`.
- Version control: Visible Meta Files, Asset Serialization: Force Text.

## Assembly definitions

One asmdef per folder below, with references flowing one way. A worker agent owning `Gameplay` must never need to touch `Core` or `UI`.

| asmdef | Folder | References | Notes |
| --- | --- | --- | --- |
| `Kit.Core` | `Core/` | Unity only | No references to other project asmdefs |
| `Kit.Input` | `Input/` | `Unity.InputSystem` | Holds the generated `KitActions` wrapper |
| `Kit.Gameplay` | `Gameplay/` | `Kit.Core`, `Kit.Input`, `Unity.Cinemachine` | Per-prototype code; subfolders per system |
| `Kit.UI` | `UI/` | `Kit.Core` | Communicates with gameplay only via the event bus |
| `Kit.Tests.EditMode` | `Tests/EditMode/` | all above + `UnityEngine.TestRunner`, `UnityEditor.TestRunner` | Editor-only platform |
| `Kit.Tests.PlayMode` | `Tests/PlayMode/` | all above + test runner | Define constraint `UNITY_INCLUDE_TESTS` |

Turn on **Auto Referenced: off** for all non-test asmdefs so stray scripts cannot bypass the graph.

## Core helper library (`Kit.Core`)

Implement each class below with XML doc comments and at least one edit-mode test. Keep every class under 200 lines; no third-party dependencies.

| Class | Namespace | Required API |
| --- | --- | --- |
| `EventBus` | `Kit.Core.Events` | `Publish<T>(T evt)`, `Subscribe<T>(Action<T>)`, `Unsubscribe<T>(Action<T>)`; struct events; cleared on play-mode enter |
| `ObjectPool<T>` | `Kit.Core.Pooling` | `Get()`, `Release(T)`, prewarm count; `PooledBehaviour` base with `OnSpawned`/`OnDespawned` |
| `Tween` | `Kit.Core.Tweening` | Static `To(float from, float to, float duration, Ease ease, Action<float> onUpdate)` returning a cancellable handle; `Ease` enum with Linear, OutQuad, OutBack, OutElastic, InOutSine; coroutine-based, no allocations per frame |
| `Juice` | `Kit.Core.Feel` | `Punch(Transform, Vector3 strength, float duration)`, `Shake(Camera, float amplitude, float duration)`, `HitStop(float seconds)` (timeScale dip) |
| `Timer` | `Kit.Core` | `After(float seconds, Action)`, `Every(float seconds, Action)`, returns handle; driven by a single `TimerRunner` MonoBehaviour |
| `StateMachine<TState>` | `Kit.Core.FSM` | Enum-keyed; `Enter`/`Exit`/`Tick` callbacks per state; `Transition(TState)` fires an `EventBus` event |
| `ConfigAsset` | `Kit.Core.Config` | Abstract `ScriptableObject` base; convention: every tunable number lives in a `ConfigAsset` subclass, never as a serialized field on a MonoBehaviour |
| `GameEvents` | `Kit.Core.Events` | Prebuilt structs: `GameStarted`, `GamePaused(bool)`, `GameOver(bool won)`, `PlayerDamaged(float)`, `ScoreChanged(int)` |
| `DebugOverlay` | `Kit.Core.Debugging` | Toggle with backquote; shows FPS, timeScale, current FSM states; `Log(string key, object value)` |

All runtime helpers must survive Enter Play Mode with Domain Reload off: reset statics in a `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]`.

## Scenes, prefabs, materials, UI

**Scenes**

- `Boot.unity`: scene 0 in Build Settings; loads `Sandbox` additively. Holds only what the owner may want to inspect and edit by hand: the `GameManager` (with its `ConfigAsset` reference), the `UI_Canvas` prefab, and the active camera rig. Invisible plumbing (`EventBus` runner, `TimerRunner`, Tween runner, `DebugOverlay`) is **not** placed here; it self-installs via `[RuntimeInitializeOnLoadMethod]` so every scene works when played directly.
- `Sandbox.unity`: 50×50 m ground plane, a directional light with soft shadows, a global Volume with the default profile, three colored primitive "landmarks" for scale, and a `PlayerSpawn` empty.

**Prefabs** (all variants of a base, saved under `Prefabs/`)

- `CameraRig_ThirdPerson`, `CameraRig_FPS`, `CameraRig_TopDown`: Cinemachine-based, each exposing a `CameraConfig` ScriptableObject (distance, damping, FOV). Only one is active in `Sandbox` by default (third-person).
- `Player_Capsule`: `CharacterController`-driven, reads `KitActions.Move/Jump/Interact`, tunables in `PlayerConfig` (speed 6 m/s, jump height 1.5 m, gravity -25). 1.8 m tall.
- `UI_Canvas`: Screen Space Overlay, `CanvasScaler` at 1920×1080 reference, containing `MainMenu`, `PauseMenu`, `GameOver`, and `HUD` panels, each toggled by `GameEvents`. Buttons: Play, Restart, Quit.

**Materials** (`Materials/`, all URP Lit unless stated)

- `Mat_Flat_Red/Blue/Green/Yellow/White/Gray/Black` (Unlit)
- `Mat_Lit_Warm`, `Mat_Lit_Cool` (Lit, mid-gray albedo with tint)
- `Mat_Emissive_Cyan`, `Mat_Emissive_Orange` (emission 2.0, bloom-visible)
- `Mat_Transparent_Glass` (Lit, transparent, alpha 0.3)
- `Mat_Grid_Checker` (procedural checker via Shader Graph, 1 m cells, for scale reference)

**Post-processing profile** (`Settings/Volume_Default.asset`): Bloom (intensity 0.4, threshold 1.0), Tonemapping ACES, Vignette 0.25, Color Adjustments (contrast +10, saturation +5). Fog in Lighting settings: linear, 20–120 m, matching skybox tint.

**Input** (`Input/KitActions.inputactions`, generate C# wrapper): action maps `Player` (Move, Look, Jump, Interact, Sprint) and `UI` (Pause, Submit, Cancel). Bind keyboard+mouse and gamepad.

## Tests

Tests exist so future agents have a working pattern to copy and so the acceptance checklist is mechanical.

- Edit mode (`Tests/EditMode/`): one test per `Kit.Core` class (EventBus publish/subscribe, pool get/release count, Tween reaches `to` at `duration`, StateMachine fires enter/exit, Timer fires after N seconds using a manual tick).
- Play mode (`Tests/PlayMode/`): `SandboxBootsCleanly` loads `Boot`, waits 2 frames, asserts zero errors in the log; `PlayerMovesOnInput` injects a `Move` vector via `InputTestFixture` and asserts the capsule's position changed.
- `Tests/README.md`: a 10-line template showing how to write both kinds, and the exact MCP or CLI command to run them (fill from the confirmed inputs). Include a batch-mode fallback: `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml`.

All tests must pass before the kit is considered done.

## `CLAUDE.md` and `docs/`

These files are the real product of the kit: they are what every future agent reads first. Write them for an agent, in imperative voice, no background.

**`CLAUDE.md`** (root, under 150 lines) must contain, in this order:

1. One paragraph: what this repo is and the definition of a finished prototype (playable in 60 s, menu + restart, zero console errors, polish checklist done).
2. Folder map (copy from this spec) and the asmdef graph with the one-way rule.
3. Conventions: `ConfigAsset` for every tunable; `EventBus` for cross-system talk; `PooledBehaviour` for anything spawned more than 10 times; never edit `.meta`; never add packages without noting it in `docs/DECISIONS.md`; prefabs over scene objects; the carried-over conventions from the inputs table (partial files per area at \~800 lines, static \<System>Debug hooks with Report(), self-installing systems via RuntimeInitializeOnLoadMethod, summary comments that say why, BACKLOG.md kept current); one system per subfolder in `Gameplay/`.
4. Exact MCP tool names and CLI fallbacks for: compile check, run edit-mode tests, run play-mode tests, enter/exit play mode, read console, take screenshot. Filled from the confirmed inputs.
5. The Unity concurrency rule: only one process may hold the project open. Worker agents write files only; one integrator agent owns the Editor/MCP session.
6. Pointer to `docs/WORKFLOW.md` and `docs/GDD_TEMPLATE.md`.

**`docs/GDD_TEMPLATE.md`**: the template an agent fills from a brief. Section headings with a one-line instruction under each, and an example value:

```markdown
# <Prototype name>
## 60-second pitch
What the player sees and does in the first minute. This is the definition of done.
## Core loop
Numbered steps. End with the single feel adjective that matters most (snappy / weighty / floaty).
## Controls, camera, scale
Camera rig variant; move speed m/s; jump height m; player height m; unit = 1 m.
## Art direction (placeholders)
Palette, materials from Materials/, fog/bloom settings, time of day.
## Features with acceptance criteria
- F1: <behavior>. Test: <setup> -> <action> -> <assert>.
## Non-goals
What is explicitly out, so agents do not build it.
## Polish checklist
Copied from WORKFLOW.md, ticked by the polish agent.
```

**`docs/WORKFLOW.md`**: the phase plan agents follow on every prototype:

1. **GDD**: one agent expands the brief into `docs/GDD.md` using the template; stops for owner review.
2. **Architecture**: one agent writes `docs/ARCHITECTURE.md`: systems list, each system's public interface (events, configs, components), owning folder under `Gameplay/`. Stubs every interface and commits before fan-out.
3. **Fan-out**: one worker per system, each confined to its folder and writing C#, prefab YAML, `ConfigAsset` files and tests. Workers never open the Editor. Four to six workers maximum.
4. **Integrate**: one integrator with MCP merges, triggers a compile, reads the console, runs tests, enters play mode, screenshots each state in the GDD, and returns failures to the owning worker with the error text.
5. **Polish**: one agent, feel-only changes, works through the checklist below and commits each item separately.
6. **Owner review**: owner gives taste notes as deltas against the GDD ("apex time 0.5 s"); integrator applies them.

Polish checklist (verbatim in `WORKFLOW.md`):

- [ ] Tween on every state change
- [ ] Hit-stop / camera shake on impacts where relevant
- [ ] Particles on impacts and pickups
- [ ] Audio stubs on every action (placeholder clips allowed)
- [ ] Main menu, pause, game-over, restart all reachable
- [ ] Post-processing profile tuned to the GDD palette
- [ ] Title shown on boot
- [ ] Zero console errors or warnings in a full play-through

**`docs/DECISIONS.md`**: dated one-line entries for every choice made where this spec was silent.

## First-person and greybox defaults

The kit is for single-player, first-person exploration prototypes. These defaults override anything above that conflicts; apply them after the base build passes, then rerun tests and the acceptance checklist.

**Camera and controller**

- `CameraRig_FPS` is the active rig in `Sandbox` and the default named in `GDD_TEMPLATE.md`. Third-person and top-down remain as variants.
- `Player_Capsule` uses a first-person `CharacterController` tuned for a weighty-but-quick feel: FOV 95, head bob, landing dip, acceleration and deceleration curves, coyote time 0.1 s, gravity -30, sprint FOV kick. Every number lives in `PlayerConfig`; the owner tunes it in Play Mode and the values are then treated as deliberate.
- HUD panel starts hidden. Main menu, pause and game-over are unchanged.

**Greybox materials** (replaces the `Mat_Flat_*` set above)

- Palette is pastel and low-contrast so blockouts read as volume, not as final art: `Mat_Grey_Floor` (warm light grey), `Mat_Grey_Wall` (cool light grey), `Mat_Pastel_Blue/Green/Peach/Lilac/Butter/Rose` (URP Lit, mid-value, roughness 0.9, no specular highlights).
- One purpose per color, documented in `CLAUDE.md`: floor/walkable, walls/static, interactive, hazard, goal/exit, spawn. Agents must use the mapping, not pick colors freely.
- Scale reference everywhere: `Mat_Grid_Checker` at 1 m cells is the ground default; walls get a subtle 1 m grid variant. Provide a `Prefabs/Greybox/` set of unit blocks (1 m cube, 2 m door frame, 1 m step, 4 m wall panel, 3 m human-scale reference figure) so agents build to real dimensions.
- Materials carry a `/// <summary>` or asset label stating their role so an agent reading the folder knows which to reach for.

**Look preset**

- A second renderer/volume preset `Lo-Fi`: render scale 0.5, point filtering, dither or posterize post effect, linear fog 10–60 m, desaturated grade with one accent. `Sandbox` defaults to `Lo-Fi`; the clean preset stays available by swapping the Volume profile.

**Docs**: `CLAUDE.md`, `GDD_TEMPLATE.md` (default camera: first-person; default look: Lo-Fi; default materials: pastel greybox set) and `DECISIONS.md` updated to match.

## Acceptance checklist

Run every item before reporting done; paste the results into the final commit message.

- [ ] Fresh clone opens in the confirmed Unity version with zero errors and zero warnings in the console
- [ ] All asmdefs compile; no script sits outside an asmdef
- [ ] All edit-mode and play-mode tests pass via the MCP tool and via the batch-mode CLI
- [ ] Pressing Play in `Boot` shows the title, Play enters `Sandbox`, the capsule moves with WASD and gamepad, Esc pauses, Restart reloads cleanly
- [ ] Enter Play Mode with Domain Reload off works twice in a row without stale state
- [ ] A screenshot of `Sandbox` in play mode is saved to `docs/sandbox.png`
- [ ] `CLAUDE.md` names the exact MCP tool names and they have each been invoked once successfully
- [ ] `docs/DECISIONS.md` lists every default taken from the inputs table
- [ ] Repo is committed with a standard Unity `.gitignore` and `Library/` excluded

## Out of scope

Do not build: any actual game content, networking, save systems, audio middleware, mobile settings, or a CI pipeline. Do not install packages beyond the table above without recording why.

## Handoff and reuse

The setup agent also writes a root `README.md` with these steps, so the kit explains its own use:

1. **Host**: push the finished kit to GitHub and mark the repo as a **template repository** (Settings → General → Template repository). Keep `Library/`, `Temp/`, `Logs/`, `obj/` out via `.gitignore`.
2. **New prototype**: on GitHub click *Use this template* (or `gh repo create <name> --template <owner>/unity-prototype-kit --private --clone`), open the clone in Unity Hub with the pinned version once so `Library/` generates, then point the Unity MCP server at it.
3. **Brief**: write the one-paragraph brief into `docs/BRIEF.md`.
4. **Start**: open Claude Code in the repo root and say: "Read CLAUDE.md and docs/WORKFLOW.md, then run phase 1 on docs/BRIEF.md." `CLAUDE.md` loads automatically; the workflow takes it from there and stops for owner review after the GDD.
