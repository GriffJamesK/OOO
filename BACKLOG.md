# Backlog

Pending requests at the top; move finished items to Done with the date (YYYY-MM-DD).

## Pending

- OOO: `PlayerSourceMovementTests.Stairs_UpAndDown_StayGrounded_AndCameraGlides` is frame-time sensitive. It failed once in a full play-mode run (camera step 0.105 m against the 0.08 limit, right after the heavy Office-scene route tests) and passes alone. Consider measuring camera speed per second instead of per frame.
- OOO: performance is unmeasured. The Editor was unfocused overnight, so frame timings were meaningless. Profile the atrium (6 shadowed spots, about 40 lights, up to 4 portal cameras) on target hardware. If needed, lower `PortalConfig.ResolutionScale` or `MaxActive`.
- OOO: concrete has no panel lines. GridChecker only takes the main light, so it would ignore interior lights. A lit, world-space 1.5 x 1 m panel-line shader that works with Forward+ lights would bring brutal_01's board-marking back.

- Optional: add a 2D camera rig variant (orthographic, CinemachinePositionComposer).
- Optional: add `com.unity.probuilder` for blockout once a prototype needs it (record in DECISIONS.md).
- Optional: replace `Kit/GridChecker` HLSL with a Shader Graph version if Shader Graph authoring tools become scriptable.

- Optional: slopes accelerate slides downhill and slow them uphill.
- Owner: tune `Config/PlayerConfig.asset` in Play Mode (FOV, head bob, landing dip, acceleration, air accelerate). The tuned values then become deliberate.

## Done

- 2026-11-02: Owner request: reference images in `docs/references/` with per-image notes; the GDD agent carries them into a References section; edit-mode test keeps images and notes in step.
- 2026-11-02: Owner request: surf ramp rebuilt as a raised, larger triangular prism; leaving surf ramps and wall runs (and wall jumps) goes the way you face; air steering aims at the view, not the input direction.
- 2026-11-02: Owner request: air control while holding forward; stairs smooth up and down (ground probe, riser skip, snap-down, camera easing); surf ramp in the test bay with surf-specific air accelerate and gravity.
- 2026-11-02: Owner request: Lo-Fi preset and vignette removed; instant stop on release; Source-style air strafing, bunny hopping and surfing; smoother, slower speed build-up.
- 2026-11-02: Owner request: subtler Lo-Fi dither, Valve-style dev colours (greys plus orange accents), walled arena, course redesigned as a flowing obstacle-course loop plus test bay.
- 2026-11-02: Owner request: Sandbox rebuilt as a 160 m movement test course; wall run and wall jump; out-of-bounds respawn (KillHeight).
- 2026-11-02: Owner feedback: slide-jump turns toward facing, chains escalate speed (streak multiplier, cap 26 m/s), stronger speed FOV plus a boost punch.
- 2026-11-02: Owner feedback: subtler head bob, slide mechanic, slide/hop chaining for speed, speed-based FOV.
- 2026-11-02: First-person and greybox defaults: FPS rig and controller feel, pastel greybox materials and prefabs, Lo-Fi look preset, docs updated.
- 2026-11-02: Initial kit: Core library, Input, Boot/Sandbox scenes, camera/player/UI prefabs, materials, post-processing, tests, docs.
