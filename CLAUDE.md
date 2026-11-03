# Prototype Kit: agent guide

This repo is a Unity 6 (6000.6.4f1, URP, 3D) template for turning a one-paragraph brief into a playable, polished **single-player, first-person exploration** prototype. A prototype is **finished** when a new player is playing within 60 seconds of pressing Play in `Boot`, the main menu, pause, game over and restart all work, the console shows zero errors and zero warnings through a full play-through, and every item in the polish checklist (`docs/WORKFLOW.md`) is ticked.

## Folder map

```
CLAUDE.md  BACKLOG.md  README.md  .mcp.json  .gitignore  .gitattributes
Tools/                 offline tools: compile-check.ps1, run-tests.ps1
docs/                  GDD_TEMPLATE.md, WORKFLOW.md, DECISIONS.md (+ GDD.md, ARCHITECTURE.md per prototype)
docs/references/       owner's reference images + REFERENCES.md (what to take from each, what to ignore)
Assets/_Project/
  Core/                Kit.Core: EventBus, ObjectPool, Tween, Juice, Timer, StateMachine, ConfigAsset, DebugOverlay
  Gameplay/<System>/   Kit.Gameplay: one subfolder per system (Game/, Player/, Camera/, Look/ ship with the kit)
  UI/                  Kit.UI: UIRouter (panels driven by GameEvents)
  Input/               Kit.Input: KitActions.inputactions, generated KitActions.cs, KitInput (shared instance)
  Editor/              Kit.Editor: KitBuilder (menu Kit > Rebuild Kit Assets) and other editor tools
  Config/              ConfigAsset .asset files (GameConfig, PlayerConfig, CameraConfig_*); owner-tuned
  Prefabs/Camera|Player|UI|Greybox/   rigs, Player_Base + Player_Capsule, UI_Canvas, greybox unit blocks
  Materials/  Shaders/ (GridChecker)  Generated/  Scenes/ (Boot, Sandbox)
  Settings/            URP asset + renderer, Volume_Default (the look: bloom, ACES, grade, fog)
  Tests/EditMode/  Tests/PlayMode/
Assets/ThirdParty/     imported assets, never edited
```

Asmdef graph. References flow one way, and nothing references upward:

```
Kit.Core  <-  Kit.UI
Kit.Core  <-  Kit.Gameplay  ->  Kit.Input, Unity.Cinemachine, URP
Kit.Editor, Kit.Tests.*  ->  everything above
```

- `Kit.Core` references only Unity packages (it uses `Unity.InputSystem` for the overlay key).
- `Kit.UI` never references `Kit.Gameplay`. UI and gameplay talk only through `EventBus`.
- A worker owning `Gameplay/<System>/` never edits `Core/` or `UI/`. If Core needs a change, put it in `BACKLOG.md`.
- Non-test asmdefs have Auto Referenced off. Every script must sit inside an asmdef folder.

## Conventions

- **Tunables:** every number a designer might tweak lives in a `ConfigAsset` subclass, saved under `Config/`, never as a serialized number on a MonoBehaviour.
- **Config values are deliberate.** The owner tunes `Config/` assets in Play Mode. Never "fix" a config value back to a default unless the owner asks; `KitBuilder` only creates missing configs and never overwrites one.
- **Cross-system talk:** use `EventBus.Publish/Subscribe` with `readonly struct` events. Subscribe in `OnEnable` and unsubscribe in `OnDisable`. Game-flow events are in `Kit.Core.Events.GameEvents`; gameplay-only events go in `Gameplay/Game/GameplayEvents.cs` or in the system's own folder.
- **Spawning:** anything spawned more than 10 times derives from `PooledBehaviour` and comes from `ObjectPool.ForPrefab`. Despawn with `Despawn()`, never `Destroy`.
- **Prefabs over scene objects:** build things as prefabs and place instances. Scenes hold layout only.
- **Builders:** generated assets come from `KitBuilder` (or a new builder in `Editor/`). Change the builder and re-run it; don't hand-edit generated YAML.
- **Never edit `.meta` files.** Let the Editor generate them, and commit them alongside their assets.
- **Never add a package** without a dated line in `docs/DECISIONS.md` saying why.
- **One system per subfolder** in `Gameplay/`, e.g. `Gameplay/Enemies/`, `Gameplay/Pickups/`.
- **Big classes:** at about 800 lines, split into `partial` files named `ClassName.Area.cs` (e.g. `Player.Jump.cs`). Fields and Unity messages stay in the main file.
- **Test hooks:** each system gets a static `<System>Debug` class with `Report()` (returns a string) plus helpers, called through MCP `execute_code`. Examples: `GameDebug`, `PlayerDebug`.
- **Self-installing systems:** runtime services set themselves up with `[RuntimeInitializeOnLoadMethod]` (see `KitHost`). Don't place plumbing objects in scenes.
- **Domain Reload is off.** Any static state must be reset in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- **Comments:** put a `<summary>` on every class and non-trivial method, and say *why* in plain English.
- **BACKLOG.md:** add requests you can't do now under Pending. Move finished items to Done with the date.
- **Game feel:** use `Tween.To`, `Juice.Punch/Shake/HitStop`, and `Timer.After/Every` rather than hand-written coroutines.

## Reference images

- The owner drops reference images in `docs/references/` and writes notes for each one in `docs/references/REFERENCES.md`: **Use for**, **Pay attention to**, **Ignore**, plus **General** notes for all of them.
- The GDD agent opens every image and records what each contributes in the GDD's **References** section (see `docs/WORKFLOW.md`, phase 1). Later agents work from the GDD and open an image only when they need a detail it doesn't cover.
- The owner's notes win over your own reading of an image. Never build something listed under **Ignore**.
- References inform the prototype's look and design; don't recreate a specific existing game, character or asset from them.
- The folder lives outside `Assets/`, so Unity never imports the images. Images go through Git LFS (`.gitattributes`).
- `ReferenceNotesTests` fails if an image has no notes section, or a section names a missing image.

## First-person and greybox defaults

- **Camera:** `CameraRig_FPS` is the rig in `Boot`. Third-person and top-down are variants: swap the prefab instance only if the GDD asks.
- **Player feel** (`Config/PlayerConfig.asset`): FOV 95 at walking pace, widening with speed up to +35 at `MaxSpeed`, plus a +6 punch on every boost; subtle head bob; landing dip; walking eases in over 0.2 s; coyote time 0.1 s; gravity -30. Feel code: `Player/PlayerController.Feel.cs`.
- **Source-style movement** (`Player/PlayerController.Movement.cs`):
  - On walkable ground with nothing held you **stop instantly**, unless you jump that frame: a hop on landing keeps all speed (bunny hopping).
  - In the air there is no drag. Source's air-accelerate rule (`AirAccelerate` 12, `AirWishSpeedCap` 0.6 m/s) means **air strafing** (strafe while turning the view) gains speed.
  - **Easy air control:** holding forward steers momentum toward the view (`AirSteer` 120 deg/s, scaled by the forward part of the input) and brings you up to walking pace (`AirWalkAcceleration`). It turns toward where you look, not toward the input direction, so W+A never drags you sideways. A pure strafe gets no steering, so air strafing and surfing stay Source-like.
  - **Leaving surfaces:** flying off a surf ramp, or ending a wall run without jumping, turns momentum up to `LeaveSurfaceTurn` (90 deg) toward where you look, never back into the surface. A wall jump launches where you look (`WallJumpAimTurn` 90), with `WallJumpPush` only as the minimum angle off the wall. Nothing should pull the player in a direction they aren't facing.
  - Velocity is clipped against every surface hit: slide along walls, stop at ceilings.
  - Slopes steeper than the controller's slope limit (45 deg) are not ground: you **surf** along them with no friction. `IsSurfing` reports it (from contact normals, not `controller.isGrounded`, which often reports a steep face as a side hit). While surfing, air accelerate is `SurfAirAccelerate` (100) and gravity is scaled by `SurfGravityScale` (0.5), so strafing into a ramp holds you on it for its whole length.
  - **Stairs:** a downward ray confirms the floor under a step edge, step risers are not clipped while grounded, walking off a step snaps down up to `GroundSnapDistance` (0.45 m), and the camera eases each step pop over `StepSmoothTime` (0.1 s). Keep this working: `Stairs_UpAndDown_StayGrounded_AndCameraGlides` guards it.
  - `PlayerController.SetVelocity` sets momentum directly (launch pads, knockback, tests).
- **Momentum chains**:
  - **Slide** (Ctrl / C / pad B) while moving at least 5 m/s gives a boost and a low capsule.
  - A **slide-jump** (Jump while sliding, or within `ChainWindow` of releasing Slide) keeps speed, adds `HopBoost`, and snaps momentum up to 120 deg toward where you look.
  - A Jump within `ChainWindow` of landing also chains; Jump is buffered 0.12 s before landing.
  - Holding Slide as you land chains straight into another slide.
  - Each consecutive chain multiplies the next boost (+15% per chain, up to 2.2x), so long chains keep gaining speed, gradually, toward `MaxSpeed` 26 m/s.
  - Missing the window breaks the streak, and ground friction pulls you back to walking pace.
  - **Wall run** (`Player/PlayerController.WallRun.cs`): airborne at 7 m/s or more, holding a direction, beside a wall, you attach and arc gently along it for up to 1.4 s, with the camera leaning away. A Jump on the wall (or within 0.15 s of leaving it) is a **wall jump**: it kicks away from the wall toward where you look and adds a small boost (a chain). Wall-to-wall chains work; the same wall can't be re-grabbed for 0.5 s.
  - Events: `PlayerSlid`, `PlayerWallRan`, `PlayerJumped.Chained`.
- **Falling below `GameConfig.KillHeight`** (-25 m) respawns the player at `PlayerSpawn` with momentum cleared.
- **Sandbox is a walled 160 m movement course** (`Editor/KitBuilder.Course.cs`). Spawn is on the south edge at (0, -72) facing +Z.
  - **The loop (east half), sections numbered C01 to C09:**
    - start straight: two hurdles one jump apart, then a slide bar
    - kicker onto a 2 m deck, a 4 m gap, a slide bar on the deck
    - 9 m wall-run gap (wall-run, then wall-jump), a deck with a hop box turning east
    - ramp down, pillar slalom, curved wall turning south, 6 m wall-to-wall corridor
    - return lane (vaults, stair hump, slide tunnel, step-ups) back into the straight
    - A scripted run completes it at sprint speed in about 36 s.
  - **Test bay (west half):** stair rises 0.15 / 0.25 / 0.30 m, ramps 15 / 30 / 44 / 50 deg, tiers, a 10 m goal tower, a 2-5 m jump-gap ladder, curb and vault rows, a free wall-run wall, a **surf ramp** (one triangular prism mesh, 60 deg faces, floating 3 m up with its ridge at 12 m, x -18, z -15 to 30; a 13 m start platform as wide as the ramp, stairs from the south), and the greybox sample.
  - Teleport with `PlayerDebug.Teleport(pos)` to test one section.
- **Dev materials for test and blockout courses:**
  - `Mat_Dev_Grey_Light` / `_Mid` / `_Dark` for plain geometry (mixed per object for variety).
  - `Mat_Dev_Orange` for movement features: anything you jump, slide under, vault or wall-run.
  - The pastel role table below is still the palette for prototype content.
- **HUD** starts hidden. Main menu, pause and game-over work as before.
- **Greybox colours: use the role, never pick freely.** Asset labels (`role-*`) carry the same mapping.

| Role | Material |
| --- | --- |
| Floor / walkable | `Mat_Grey_Floor`; large ground uses `Mat_Grid_Checker` (1 m checker) |
| Walls / static | `Mat_Grey_Wall`; walls that need scale use `Mat_Grid_Wall` (1 m grid lines) |
| Interactive | `Mat_Pastel_Blue` |
| Hazard | `Mat_Pastel_Rose` |
| Goal / exit | `Mat_Pastel_Green` |
| Spawn | `Mat_Pastel_Butter` |
| Pickup / collectible (kit extension) | `Mat_Pastel_Peach` |
| Player / NPC character (kit extension) | `Mat_Pastel_Lilac` |

- **Build to real dimensions** with `Prefabs/Greybox/`: `Greybox_Block_1m`, `Greybox_DoorFrame_2m` (1x2 m opening), `Greybox_Step_1m` (0.25 m rise), `Greybox_Wall_4m` (4x3 m), `Greybox_ScaleRef_3m`. Pivots are bottom-centre, so drop them at y = 0.
- **Look:** one profile, `Settings/Volume_Default`: bloom, ACES tonemapping, a light colour grade, no vignette, and linear fog 20-120 m. Fog lives in the profile (`KitFog`); `LookController` on the Global Volume copies it into the scene's fog settings.

## Tools: MCP first, CLI fallback

MCP server **UnityMCP** (`.mcp.json`, HTTP `http://127.0.0.1:8080/mcp`). Start it in Unity with **Window > MCP for Unity > Connect > Start Server**.

| Task | MCP tool | CLI fallback (Editor must be closed) |
| --- | --- | --- |
| Compile check | `refresh_unity` (`compile: "request"`, `wait_for_ready: true`), then `read_console` (`types: ["error","warning"]`) | `powershell -File Tools/compile-check.ps1` |
| Edit-mode tests | `run_tests` (`mode: "EditMode"`), then `get_test_job` (`job_id`, `wait_timeout: 60`) | `powershell -File Tools/run-tests.ps1 -Platform EditMode` |
| Play-mode tests | `run_tests` (`mode: "PlayMode"`, `init_timeout: 120000`), then `get_test_job` | `powershell -File Tools/run-tests.ps1 -Platform PlayMode` |
| Enter / exit Play | `manage_editor` (`action: "play"` / `"stop"`) | n/a |
| Read console | `read_console` (`action: "get"`, `types: ["error","warning","log"]`) | read `Logs/compile-check.log` |
| Screenshot | `manage_camera` (`action: "screenshot"`, `include_image: true`); or `execute_code`: `ScreenCapture.CaptureScreenshot("docs/shot.png")` | n/a |
| Run editor C# | `execute_code`, e.g. `return Kit.Gameplay.GameDebug.Report();` (also `PlayerDebug`, `Look.LookDebug`) | n/a |
| Rebuild kit assets | `execute_code`: `Kit.Editor.KitBuilder.BuildAll();` | n/a |
| Save scene | `manage_scene` (`action: "save"`) | n/a |

Raw CLI: `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml`.

Play mode only advances while the Editor is ticking. Run In Background is on, but if `Time.frameCount` stops moving, the Unity window is minimized: ask the owner to restore it.

- **Stop play mode before you compile.** A recompile during play reloads scripts and breaks live state (non-serialized fields go null).
- **Don't poll MCP during a timed play-mode check.** Each call can stall the Editor for a frame of up to 0.33 s, which skips physics moments such as a wall catch. Script the run (e.g. `Kit.Core.Timer.Every` plus a virtual `Gamepad`), wait in the shell, then read the result once.
- **Mouse look is live in play mode.** If the owner's mouse is over the Game view, the camera moves. Reset the pan/tilt before screenshots.
- **Batch runs need a short path.** Clone to a short folder (e.g. `%TEMP%\kc`): deep paths exceed the Windows 260-character limit inside `Library/PackageCache` and fill the log with false errors.

## Concurrency rule

Only one process may hold the project open. A running Editor locks `Temp/UnityLockfile`, so the batch-mode CLI fails while it is open.
- **Worker agents write files only**: C#, prefab/asset YAML they generate, configs, tests. They never open the Editor and never call MCP.
- **One integrator agent owns the Editor and the MCP session.** It compiles, runs tests, enters play mode, and takes screenshots.
- For a CLI check while the Editor is open, `git clone` the repo into a temp folder and run the tools against the clone.

## Next

- Follow `docs/WORKFLOW.md` phase by phase. Do not skip owner review after the GDD.
- Fill `docs/GDD_TEMPLATE.md` into `docs/GDD.md` from `docs/BRIEF.md` and the images and notes in `docs/references/`.
- Check `docs/DECISIONS.md` before changing a setting the kit already decided.
