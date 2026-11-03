# OOO architecture

How `docs/GDD.md` becomes code. Read `CLAUDE.md` first. Every interface named here exists as a compiled stub on `main` before fan-out. Workers implement behind these interfaces and never change a signature without telling the lead.

## Ground rules for every worker

- You own only the files listed for your task. Anything else goes in `BACKLOG.md` with a note to the lead.
- **Compile check (mandatory, before you finish):** `powershell -NoProfile -ExecutionPolicy Bypass -File Tools/quick-compile.ps1 -SourceRoot <your worktree root>`. It must report 0 errors and 0 warnings. It takes about 10 s and works while the Editor is open.
- You can't open Unity or call MCP. The integrator (lead) compiles in the Editor, runs your tests, builds the Office scene, takes screenshots and sends you back exact errors.
- Unity 6000.6, URP, C# 9. New Input System only. Domain reload is off, so reset statics in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Every tunable number goes in your system's `ConfigAsset` subclass. Each config's asset lives at `Assets/_Project/Config/<ClassName>.asset` and is created by `OfficeBuilder.BuildConfigs`. At runtime, read it with `ConfigLookup.Get<T>()`, which returns defaults when no asset is registered (tests). At edit time, builders use `OfficeKit.Config<T>()`.
- Events are `readonly struct`s published through `EventBus`. Subscribe in `OnEnable`, unsubscribe in `OnDisable`.
- Every class and non-trivial method gets a `<summary>` saying why.
- Zero warnings: no unused fields or variables, no obsolete APIs (`FindObjectOfType` is obsolete: use `FindAnyObjectByType`).
- Never edit `.meta` files and never hand-write scene, prefab or material YAML. Generated content comes from builders under `Assets/_Project/Editor/`.

## Systems

| System | Folder (owner) | Namespace |
| --- | --- | --- |
| Thresholds (pairing, crossing, teleport) | `Gameplay/Thresholds/` | `Kit.Gameplay.Thresholds` |
| Portals (see-through rendering) | `Gameplay/Portals/`, `Shaders/Portal.shader` | `Kit.Gameplay.Portals` |
| Letterboard (plates, tiles, carrying) | `Gameplay/Letterboards/` | `Kit.Gameplay.Letterboards` |
| WorldText (diegetic text rendering) | `Gameplay/Signage/`, `Shaders/WorldText.shader` | `Kit.Gameplay.Signage` |
| Flow (respawn, screen fade, ending) | `Gameplay/Flow/` | `Kit.Gameplay.Flow` |
| Audio (procedural placeholder audio) | `Gameplay/Audio/` | `Kit.Gameplay.Audio` |
| Look (materials, volume, fog zones, light shafts, flicker) | `Gameplay/Look/`, `Shaders/LightShaft.shader`, `Editor/OfficeBuilder.Look.cs` | `Kit.Gameplay.Look` |
| Props (procedural furniture prefabs) | `Editor/OfficeBuilder.Props.cs`, `Editor/Props/` | `Kit.Editor` |
| Areas A1-A7 | `Editor/OfficeBuilder.Area_A*.cs` (one file each) | `Kit.Editor` |
| Game flow, player, camera, UI, office scene assembly, `OfficeKit` | lead | |

### Shared runtime contracts (lead-owned, already implemented)

- `PlayerController.Moved` (`event Action<PlayerController, Vector3 previousPosition>`) fires at the end of every `Update`, after collisions and camera feel. The thresholds system hooks crossings here.
- `PlayerController.TeleportThrough(Matrix4x4 map, float maxExitSpeed)` moves the player through a paired threshold:
  - Maps the capsule centre with `map` and keeps the body upright.
  - Rotates the full 3D velocity with `map`, then clamps it to `maxExitSpeed`.
  - Keeps chain and slide state, ends any wall run, and clears contacts.
  - Stays grounded only if `map` keeps "up" up.
  - Publishes `PlayerTeleported`. `CameraRig` re-aims the view with the same rotation (yaw and pitch, roll removed) and warps Cinemachine, so the view doesn't hitch.
- `GameManager`:
  - With `GameConfig.RespawnAtSafeGround` (default true), falling below `KillHeight` publishes `PlayerFellOut` once per fall instead of respawning. The Flow system answers with `GameManager.RespawnPlayer(Pose)`.
  - `GameManager.SpawnPose` is the fallback respawn point.
  - `GameCompleted` (Core) sets `GameManager.Completed`. The state stays `Playing`, there is no game over, and the player keeps moving.
- `ConfigLookup.Get<T>()` finds a config in `GameConfig.Systems`, or returns a cached default instance.
- `AreaZone`: a box placed by area builders. `AreaZone.Current` is the area the player is in (bounds check each frame). Changes publish `PlayerEnteredArea(id)`.
- UI (`UIRouter`):
  - The main menu shows the title **OOO** with Play and Quit. Pause has Resume, Restart and Quit.
  - The HUD is never shown.
  - `GameCompleted` fades in an ending card ("OOO") over 3 s, holds it 6 s, then fades it out. The card isn't interactive, and pause still works.
  - The game-over panel is never used.
- Layers: `Portal` (8) holds portal surfaces, which portal cameras never render. `ViewModel` (9) holds carried tiles in view.

### Events (`Gameplay/Game/OfficeEvents.cs`, plus `GameCompleted` in Core `GameEvents.cs`)

| Event | Publisher | Typical subscribers |
| --- | --- | --- |
| `PlayerTeleported(Matrix4x4 Map, Vector3 Delta)` | PlayerController | CameraRig, Audio |
| `PlayerCrossedThreshold(Threshold From, Threshold To, float Speed)` | Thresholds | Audio, Flow (ending), Portals |
| `ThresholdsRepaired(int PairCount)` | Thresholds | Portals, Audio |
| `TilePulled(Letterboard Board, int Slot, char Glyph)` / `TilePlaced(...)` | Letterboard | Audio |
| `PlayerFellOut` | GameManager | Flow |
| `PlayerRespawned(Vector3 Position)` | Flow | Audio |
| `PlayerEnteredArea(string AreaId)` | AreaZone | Audio, Look |
| `GameCompleted` (Core) | Flow (ending) | GameManager, UI, Audio |

### Thresholds (`Kit.Gameplay.Thresholds`)

**The rule.**
- A `Threshold` is an opening: a rectangle of `Size` (width x height) centred on its transform. `transform.forward` is the **front** normal, the side a player normally approaches from.
- Its number comes from an `INumberSource` on the same object or a child: a `BrassPlate` (fixed) or a `Letterboard` (movable tiles).
- `ThresholdNumber.Normalize`: uppercase, drop spaces and empty slots, read letter **O** as digit **0**.
- `ThresholdPairing.Resolve`: a non-empty number pairs only when **exactly two** registered thresholds share it.

**Crossing (convention: front in, front out).**
- `ThresholdMath.PairMap(a, b)` = `b.localToWorld * R(180 deg about local up) * a.worldToLocal`.
- Crossing A's plane front-to-back, inside its rectangle, emerges in front of B moving away from B's front. Back-to-front emerges behind B.
- Two thresholds facing each other across a passage therefore make an endless passage for anyone inside it.

**Components and classes.**
- `Threshold` holds:
  - `Size` and `Exterior`: true only on the outside door 000. The ending fires on arriving at it.
  - `Number`, `Partner`, `IsPaired`.
  - `PairMap` (the matrix to the partner).
  - `PortalSurfaceRoot`, an empty child where the portal quad goes.
  - It registers in `OnEnable`.
- `ThresholdRegistry` (static) re-pairs when a number source raises `Changed`, publishes `ThresholdsRepaired`, and exposes `All` and `Paired`.
- `ThresholdTraveller` is added to the player on `PlayerSpawned` by a self-installing `ThresholdSystem`. On `PlayerController.Moved` it tests the segment from the previous to the current capsule centre against every paired threshold within 30 m. Crossing front-to-back or back-to-front calls `TeleportThrough(PairMap, config.MaxExitSpeed)`, then publishes `PlayerCrossedThreshold`. At most one teleport per frame.
- `BrassPlate : INumberSource` holds a fixed number and renders it with `WorldText` (brass style).
- `ThresholdConfig`:
  - `MaxExitSpeed` 26
  - `CrossingSearchRadius` 30
  - `FrameDepth` 0.25: the frame geometry is built by `OfficeKit`, and players need at least 0.6 m clear behind and in front of every threshold.
- Test hook: `ThresholdDebug.Report()` lists every threshold with its number, partner, and whether the player is near.

### Portals (`Kit.Gameplay.Portals`)

- A self-installing `PortalSystem` keeps one `PortalView` per paired threshold, rebuilt on `ThresholdsRepaired`.
- A `PortalView` owns:
  - A quad on layer `Portal`, sized to the opening, parented to `PortalSurfaceRoot`, using the `Kit/Portal` shader (samples its RenderTexture in screen space, unlit, no fog).
  - A disabled `Camera` rendering to a full-resolution HDR RenderTexture.
- In `RenderPipelineManager.beginContextRendering`, for each portal that is within `PortalConfig.MaxDistance` (45 m), inside the main camera frustum and facing the camera:
  - The portal camera is placed at `PairMap * mainCamera`, with the same FOV.
  - It gets an oblique near plane on the partner's plane.
  - Its culling mask leaves out `Portal`, so recursion is depth 1 and a portal seen through a portal shows the real space behind it.
  - Post-processing is off on portal cameras. The main camera grades everything once.
  - Render with `RenderPipeline.SubmitRenderRequest(cam, new UniversalRenderPipeline.SingleCameraRequest { destination = rt })`, or with the camera enabled and a lower depth if that proves more reliable. The worker decides and records it in `DECISIONS.md`.
- Near-plane fix: when the main camera is within 0.6 m of a portal plane, the quad becomes a box extruded 0.6 m behind the plane, so the camera never sees the real space mid-crossing.
- Unpaired thresholds show no quad. You see the real space behind them.
- `PortalConfig`: `MaxDistance` 45, `MaxActive` 4, `NearExtrude` 0.6, `SurfaceMaterial` (assigned by `OfficeBuilder.BuildConfigs` from `Materials/Mat_Portal.mat`, which the portal worker's builder creates).
- Test hook: `PortalDebug.Report()`.

### Letterboard (`Kit.Gameplay.Letterboards`)

- A `Letterboard : INumberSource` is a felt board with `Rows` x `Columns` slots, each holding one glyph or empty (`' '`).
  - As a threshold plate it has one row, and `Number` = its glyphs. As a sign (directory, menu) it has many rows and no threshold.
  - Each slot gets a trigger `BoxCollider` (`LetterSlot`, with board and index) for interaction raycasts. Tiles are rendered with `WorldText`.
  - `bool TryPull(int slot, out char glyph)`, `bool TryPlace(int slot, char glyph)`, and `event Action Changed`.
  - `Setup(int rows, int cols, string content)` (builder-time; `content` rows separated by `\n`, `_` = empty slot).
- `TileCarrier` is added to the player on `PlayerSpawned` by the self-installing `LetterboardSystem`.
  - On Interact, it raycasts from the main camera (real forward, with pitch) up to `Reach` against `LetterSlot` triggers. A full slot is pulled; an empty slot gets the oldest carried tile placed.
  - It carries up to `MaxCarried` (3) and shows carried tiles as small plates at the lower right of the view (layer `ViewModel`, child of the main camera).
  - It publishes `TilePulled` / `TilePlaced`.
- `LetterboardConfig`: `MaxCarried` 3, `Reach` 2.6, `SlotSize` 0.16 x 0.2 m, `TileGap` 0.02, plus tile and felt materials.
- Test hook: `LetterboardDebug.Report()` (carried tiles, the nearest board), and `Give(char)` / `Clear()`.

### WorldText (`Kit.Gameplay.Signage`)

- `WorldText` wraps a `TextMesh` (font `LegacyRuntime.ttf`) with a material from `WorldTextConfig` per `WorldTextStyle`: `DarkOnLight`, `LightOnDark`, `Brass`, `Glow` (emissive, for lit signs and screens).
- The `Kit/WorldText` shader is alpha-tested, gets approximate scene lighting (main light, ambient and additional lights) and fog, so text sits in the scene instead of glowing.
- `Set(string text, float letterHeight, WorldTextStyle style, TextAnchor anchor = MiddleCenter)` works in edit mode, so builders call it.
- `static WorldText Create(Transform parent, string name, string text, Vector3 localPos, Quaternion localRot, float letterHeight, WorldTextStyle style, TextAnchor anchor)`.
- `WorldTextConfig`: font, materials per style, `PixelsPerMetre` (sets `fontSize` vs `characterSize` for crisp text).
- It handles `Font.textureRebuilt` (the dynamic font atlas) by re-binding the texture.

### Flow (`Kit.Gameplay.Flow`)

- `SafeGroundTracker` is added to the player on `PlayerSpawned`. It records the last pose where the player stood grounded on walkable ground for at least `SafeGroundDwell` (0.3 s), not inside a `NoSafeGroundZone`.
- `FallZone` (boxes placed by builders) and `PlayerFellOut` both trigger a respawn:
  1. Fade to black over `FadeOut` 0.25 s.
  2. `GameManager.RespawnPlayer(lastSafePose)`. With no safe ground yet, use `SpawnPose`.
  3. Fade in over `FadeIn` 0.4 s.
  4. Publish `PlayerRespawned`.
- `ScreenFade` (static API, self-installing) uses a full-screen black overlay that is not UI: a high-priority global `Volume` driving `ColorAdjustments.postExposure`, or a camera-space quad on `ViewModel`. `FadeTo(float alpha, float duration, Action onDone = null)` and `Alpha`.
- `EndingDirector` (self-installing): on `PlayerCrossedThreshold` whose `To.Exterior` is true, waits `EndingTitleDelay` (6 s), then publishes Core `GameCompleted`.
- `FlowConfig`: `SafeGroundDwell`, `FadeOut`, `FadeIn`, `EndingTitleDelay`.
- Test hook: `FlowDebug.Report()`.

### Audio (`Kit.Gameplay.Audio`)

- A self-installing `AudioDirector` builds clips procedurally at start (`AudioClip.Create`): footsteps (concrete, carpet), jump, land, slide hiss, wall-run scuff, threshold crossing (a soft pressure "whump" plus a short reverb tail), tile pull/place (felt click), respawn swoosh, and ambiences (fluorescent hum, HVAC drone, a distant air rumble outside, an ending tone).
- Ambience per area comes from `PlayerEnteredArea`, with 2 s crossfades.
- `AudioConfig`: volumes per category, footstep stride.
- Test hook: `AudioDebug.Report()`.

### Look (`Kit.Gameplay.Look`)

- **Office material set** (`Editor/OfficeBuilder.Look.cs`): every material named in `OfficeKit.M`, built with URP Lit (or the GridChecker shader for concrete panel lines).
- **`Volume_Office`** tuned to the GDD: fog `#2A323C` 8-70 m (via `KitFog`), bloom about 0.5, a cool grade, saturation -15, ACES.
- **LookController** now reads the **blended volume stack** (`VolumeManager.instance.stack`), not just its own profile, so local Volumes (`OfficeKit.FogZone`) give per-area fog. The Outside gets pale fog.
- **`LightShaft`** mesh and the `Kit/LightShaft` shader (additive, soft edges, fades with distance and view angle) for skylight shafts.
- **`FlickerLight`** for a rare, subtle fluorescent flicker, and **`ScreenGlow`** for a slight CRT pulse.
- **URP renderer:** switch to Forward+, so many interior lights work. This is done in `OfficeBuilder.Look.cs` via SerializedObject, not by hand-editing YAML.
- `LookConfig` holds flicker rates and similar values.

### Props (`Kit.Editor`)

`OfficeBuilder.BuildProps()` generates every prop in `OfficeKit.PropIds` as `Prefabs/Office/Props/<Id>.prefab`, using procedural meshes saved under `Generated/Props/` and `OfficeKit.M` materials.
- Pivots are bottom-centre, sizes are real (metres), and props face +Z.
- Colliders are simple boxes or capsules where the player would collide; small clutter has none.
- The signature prop is the inside_02 **shell chair**: a curved one-piece plastic shell in burnt orange on four thin black legs, about 0.45 m seat height.

### Areas (`Kit.Editor`)

- Each area worker implements one partial method of `OfficeBuilder`: `static partial void BuildArea_A1(Transform root)` and so on. It builds everything under `root` in **world coordinates**, inside its envelope from `OfficeLayout`, using only `OfficeKit` helpers (geometry, thresholds, letterboards, signs, props, lights, zones).
- `OfficeBuilder.BuildOffice()` (menu **Kit > Build Office**) creates `Scenes/Office.unity`: environment, global volume, the area roots, and `PlayerSpawn`.

## World layout (`Editor/OfficeLayout.cs` holds every number here)

1 unit = 1 m. Floors are 4 m apart (`L(n)` = 4n): L-1 = -4, L0 = 0 ... L6 = 24. Areas meet at **sockets**: an opening both sides must leave clear, given as a centre, a size and a facing.

| Area | Envelope (world, min -> max) | Floor y | Connects |
| --- | --- | --- | --- |
| A2 Atrium (hub) | x -20..20, z -30..30, y 0..34 | L0..L6 | all |
| A1 Waiting Room | x -52..-20, z -12..12, y 16..22 | 16 (L4) | A2 west wall at L4 (transom and shutter onto the gallery) |
| A3 Open Plan | x -35..35, z 30..90, y 12..16 | 12 (L3) | A2 north wall at L3 |
| A4 Cafeteria | x 20..72, z -25..25, y 4..18 | 4 (L1), balcony 12 | A2 east wall at L1 |
| A5 Stair Core | x -40..-20, z -34..-14, y -4..34 | -4..24 | A2 west wall at L0 and L2 |
| A6 Executive Wing | x 20..60, z -20..20, y 24..29 | 24 (L6) | the A2 L6 "boardroom pod" bridge |
| A7 Outside | x 72..420, z -200..200, y 0 | 0 | the exterior door 000 only (A6 windows look over it) |

**Sockets** (the wall plane is shared, and each side builds its own half of the wall):
- `A1_Transom` x -20, y 16 + 2.6 .. 16 + 3.4, z 2 .. 4; `A1_Shutter` x -20, y 16 .. 16 + 0.9 (half-lowered), z -6 .. -3. The A2 L4 west gallery must run past both.
- `A3_Entry` z 30, x -2 .. 2, y 12 .. 15 (A2 L3 north gallery).
- `A4_Entry` x 20, z -3 .. 3, y 4 .. 7 (A2 L1 east gallery).
- `A5_L0` x -20, z -26 .. -22, y 0 .. 3; `A5_L2` x -20, z -26 .. -22, y 8 .. 11.
- `A6_Bridge` x 20, z -2 .. 2, y 24 .. 27. The A2 boardroom pod is a cantilevered slab at y 24, x 0 .. 20, z -6 .. 6, reachable **only** by the launch.
- `Launch_Exit` (threshold 6) on the A2 west wall at x -20 (front +X, tilted 30 deg up), centre y 29, z 0, opening 3 x 3 (the same size as the hatch). Exit speed 26 m/s at 30 deg up, under gravity -30, lands about 26 m out at y 24 (x about +6), on the pod.
- `Launch_Hatch` (threshold 6) is the A5 floor hatch at y -4 + 0.01 (front +Y), x -32, z -26, 3 x 3. It sits under the A5 void (x -36..-28, z -30..-22), open from y 24 down.

## Threshold number plan (authoritative)

**Both thresholds of a pair have the same opening size and the same sill height above their floor.** The map is rigid: an off-centre entry comes out at the mirrored offset, so a smaller partner would put the player inside a wall. Sizes: 104 is 1.2 x 2.4. 1, 312 and 000 (and the cloakroom) are 1 x 2.1 (`OfficeLayout.Door`). 37 is 2.4 x 2.6. 412 is a 2 x 1.2 wall hatch with its sill 0.9 m above the floor. 6 is 3 x 3.

A threshold number pairs anywhere in the world, so **only the numbers below may appear on threshold plates**. The ranges hold an area's own unpaired door numbers: each must be unique, in range, and must not appear on any other threshold. Text that isn't on a threshold (memos, clocks, signs, letterboard menus) may use any number.

| Number | Location | Plate | Front faces | Role |
| --- | --- | --- | --- | --- |
| 104 | A1 corridor, north end | brass | south (into corridor) | endless corridor (K1) |
| 104 | A1 corridor, south end | brass | north (into corridor) | endless corridor |
| 1 | A1 reception office door | brass | into waiting room | spawn <-> lobby link |
| 1 | A2 lobby revolving doors (south wall, z -30) | brass | north (into lobby) | "leaving" loops to the waiting room |
| 37 | A3 records passage, south end | letterboard `37` | north (into passage) | endless passage (K2) |
| 37 | A3 records passage, north end | letterboard `37` | south (into passage) | endless passage |
| 31-36, 38-42 | A3 aisle signs (one each) | letterboard | along each aisle | ordinary, and a source of digit tiles |
| 312 | A3 north records-room door | brass | into A3 | shortcut |
| 312 | A4 balcony door | brass | onto balcony | shortcut |
| 412 | A4 kitchen roller shutter | brass | into cafeteria | shortcut |
| 412 | A3 service hatch (wall, near entry) | brass | into A3 | shortcut |
| 6 | A5 floor hatch (bottom of void) | brass | up | launch (K3) |
| 6 | A2 west wall high opening | brass | +X, 30 deg up | launch exit |
| 000 | A7 free-standing exterior door | brass (large, lit) | east (+X, away from the building) | the way out (K5) |
| (3 empty slots) | A2 lobby cloakroom door | letterboard `___` | into lobby | where the player makes 000 |
| ranges | A1 101-199, A2 201-299, A3 301-399, A4 401-499, A5 501-599, A6 601-699 | brass | | unpaired doors |

**Tile sources:**
- The lobby directory letterboard: rows including `OUT OF OFFICE`, which gives three O's. It sits high on the lobby's south wall and is reachable only with movement.
- The cafeteria menu letterboard (`S0UP`, `C0FFEE` with digit zeros: the K4 hint).
- The A3 aisle signs.

## Tests per feature (GDD acceptance criteria)

| Feature | Test file (owner) |
| --- | --- |
| F2, F4, F6, F7 | `Tests/EditMode/ThresholdLogicTests.cs` (pure math and pairing), `Tests/PlayMode/ThresholdTraversalTests.cs` (Thresholds) |
| F3 | `Tests/PlayMode/PortalViewTests.cs` (Portals) |
| F5 | `Tests/PlayMode/LetterboardTests.cs` (Letterboard) |
| F8, F9 | `Tests/PlayMode/FlowTests.cs` (Flow) |
| F12, F14 | `Tests/PlayMode/OfficeFlowTests.cs` (lead) |
| F13, number plan | `Tests/EditMode/OfficeContentTests.cs` (lead, after areas land) |
| F10, F11 | `Tests/PlayMode/OfficeRouteTests.cs` (lead, after areas land) |
| F1 | the existing movement tests |
