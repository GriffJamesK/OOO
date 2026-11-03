# Workplan

Lead: this session (Opus 5.5, high effort). The lead is also the integrator: only it holds the Editor and MCP.
Workers: subagents in their own git worktrees. They write files only and check with `Tools/quick-compile.ps1`.

| # | Task | Phase | Owns | Depends on | Done check | Model / effort | Status |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | GDD | 1 | docs/GDD.md | brief | owner approval | Opus 5.5 / high | done |
| 2 | Architecture, stubs, failing tests, quick-compile | 2 | docs/ARCHITECTURE.md, OfficeKit/Layout/Builder, events, Player/Camera/Game/UI changes, tests | 1 | compiles in Editor; existing tests pass; new tests fail | Opus 5.5 / high | done |
| 3 | Thresholds | 3 | Gameplay/Thresholds/, Tests/EditMode/ThresholdLogicTests.cs, Tests/PlayMode/ThresholdTraversalTests.cs | 2 | ThresholdLogicTests + ThresholdTraversalTests pass | Opus / high | merged; tests green |
| 4 | Portals | 3 | Gameplay/Portals/, Shaders/Portal.shader, Editor/OfficeBuilder.Portals.cs, Tests/PlayMode/PortalViewTests.cs | 2 (3 for live test) | PortalViewTests pass; screenshot shows partner view | Opus / high | merged; tests green |
| 5 | Letterboards | 3 | Gameplay/Letterboards/, Editor/OfficeBuilder.Letterboard.cs, Tests/PlayMode/LetterboardTests.cs | 2 | LetterboardTests pass | Opus / medium | merged; edit-mode tests green |
| 6 | Signage (WorldText) | 3 | Gameplay/Signage/, Shaders/WorldText.shader, Editor/OfficeBuilder.Signage.cs, Tests/EditMode/SignageTests.cs | 2 | text renders lit and fogged; tests pass | Opus / medium | merged; edit-mode tests green |
| 7 | Flow (respawn, fade, ending) | 3 | Gameplay/Flow/, Tests/PlayMode/FlowTests.cs | 2 | FlowTests + kit FallingOutOfBounds pass | Opus / medium | merged; edit-mode tests green |
| 8 | Audio | 3 | Gameplay/Audio/, Tests/EditMode/AudioTests.cs | 2 | clips synthesise; events play; no warnings | Opus / medium | merged; tests green |
| 9 | Look (materials, volume, fog zones, shafts) | 3 | Gameplay/Look/, Shaders/LightShaft.shader, Editor/OfficeBuilder.Look.cs | 2 | materials exist; screenshots match the GDD palette | Opus / high | merged; tests green |
| 10 | Props | 3 | Editor/OfficeBuilder.Props.cs, Editor/Props/ | 2 | every PropId prefab builds; screenshot review | Opus / high | merged; tests green |
| 11 | A1 Waiting Room | 3 | Editor/OfficeBuilder.Area_A1.cs | 2 | builds; 104 loop; exits; screenshots | Opus / high | merged; tests green |
| 12 | A2 Atrium + lobby | 3 | Editor/OfficeBuilder.Area_A2.cs | 2 | builds; sockets open; pod and launch exit; directory and cloakroom | Opus / high | merged; tests green |
| 13 | A3 Open Plan | 3 | Editor/OfficeBuilder.Area_A3.cs | 2 | builds; 37 passage loop; 312/412 | Opus / high | merged; tests green |
| 14 | A4 Cafeteria | 3 | Editor/OfficeBuilder.Area_A4.cs | 2 | builds; menu board; surf terraces; balcony | Opus / high | merged; tests green |
| 15 | A5 Stair Core | 3 | Editor/OfficeBuilder.Area_A5.cs | 2 | builds; climb to L6; drop into hatch 6 | Opus / high | merged; tests green |
| 16 | A6 Executive + A7 Outside | 3 | Editor/OfficeBuilder.Area_A6.cs, Editor/OfficeBuilder.Area_A7.cs | 2 | builds; window view of 000; outside plain | Opus / high | merged; tests green |
| 17 | Integrate: merge, compile, tests, play-through, screenshots | 4 | lead | 3-16 | all tests green, zero console warnings | Opus 5.5 / high | done: 141 edit-mode + 38 play-mode tests green, 0 console warnings |
| 18 | Content and route tests (F10, F11, F13, number plan) | 4 | Tests/EditMode/OfficeContentTests.cs, Tests/PlayMode/OfficeRouteTests.cs | 11-16 | pass | lead | done: OfficeContentTests + OfficeRouteTests green |
| 19 | Polish checklist | 5 | per item | 17 | checklist ticked | Opus / high | done: feel worker (Gameplay/Feel) + lead |

Phase 3 started all 14 workers at once (2026-11-02). They depend only on the committed stubs, so waves would only have added delay.

Feedback rounds after integration: A6/A7 twice (K5 visibility from the boardroom, outside haze, light levels) and A4 once (balcony route rebuilt around a wall run after the surf line failed a scripted run). Polish: one extra worker (Gameplay/Feel: landing shake, dust, tile puffs). All workers ran on Opus at high effort, except letterboards, signage, flow and audio (medium).
