# OOO: overnight build report (2026-11-02)

**Status:** playable end to end. Every phase in `docs/WORKFLOW.md` has run: GDD, architecture, fan-out, integration and polish. All 141 edit-mode and 38 play-mode tests pass. The console showed zero errors and zero warnings through the play sessions and the full test runs. Everything is on `origin/main`.

**To play:** open `Assets/_Project/Scenes/Boot.unity`, press Play, then Play on the title screen. Controls are in the README. If you change a builder, run **Kit > Build Office**.

## What's in the game

| Area | What it is | Screenshot |
| --- | --- | --- |
| A1 Waiting Room (spawn) | Wood panelling, orange shell-chair rows, NOW SERVING 104. The corridor's two 104 doors face each other, so it loops endlessly. Reception door 1 leads to the lobby. A transom and a jammed shutter lead to the atrium. | `docs/shots/a1_spawn_props.png`, `a1_corridor_portal.png` |
| A2 Atrium + lobby (hub) | 34 m brutalist atrium: four gallery rings, a skylight band with light shafts, seating pits, the tree. The directory high on the south wall holds "OUT OF OFFICE". The cloakroom's blank plate is where the player makes 000. The launch exit 6 is high on the west wall, and the boardroom pod juts into the void. | `docs/shots/a2_lobby_hero.png` |
| A3 Open Plan | 67 cubicles with green CRTs, aisle signs 31-42 (letterboard tiles), the endless 37 records passage, the Records Room, door 312, the 412 slide hatch, and the "warm desk" 3-114. | `docs/shots/a3_field_boost_exp.png` |
| A4 Cafeteria | The inside_02 hall: about 120 shell chairs, a skylight shaft, a hanging garden. The menu board (S0UP, C0FFEE) holds the O/0 hint. The 412 kitchen shutter. A balcony with door 312, reached by a wall run along the blade wall. | `docs/shots/a4_entry_exp16.png` |
| A5 Stair Core | A concrete shaft with stairs from -4 to 24 m, a chair at the edge of the top landing, and hatch 6 at the bottom. Dropping in launches you at 26 m/s across the atrium onto the pod. | `docs/shots/a5_top_landing.png` |
| A6 Executive Wing | Oxblood and wood, a lit glass meeting room in a dark hall (inside_01), T. Vane's office, and the boardroom whiteboard ("000 — ?"). The window looks out at 000 on the plain. | `docs/shots/a6_dark_hall-1.png`, `a6_window_000_v2.png` |
| A7 Outside (ending) | A pale haze plain, a lone lit door, a stele and the ground numerals 000, a line of lamp posts. Walking out fires the ending card. Play continues. | `docs/shots/a7_door_lookback.png` |

**The rule:** two openings showing the same number are one opening (exactly two; three cancel out). Front in, front out, with speed kept. Letterboard tiles move. The letter O reads as 0.

**The way out:** take three O's (from the directory, or from the cafeteria menu's TODAY/TOAST) and press them into the cloakroom plate so it reads 000, then walk through. The fastest route starts with reception door 1.

## How it was checked

- **Unit and play-mode tests:** pairing, normalising, crossing maths, traversal (including the floor-to-tilted-wall launch), letterboards, portals, respawn, the ending, look and fog, feel, audio synthesis, game flow (menu, pause, restart, no game over, no HUD).
- **Content tests on the built scene:**
  - Every threshold number follows the plan.
  - Every area has 8 or more lived-in text props.
  - No text reads as instructions.
  - Every socket where areas meet is open.
- **Route tests in the real scene:**
  - Every planned pair can be walked through from its front, arriving clear of walls.
  - The launch lands on the pod.
  - The full expert route works: door 1, three O's, cloakroom set to OOO, outside, `GameCompleted`, state still Playing.
- **Scripted runs with real gamepad input (`OfficeBot`):**
  - the shutter slide (A1 to A2)
  - the 412 slide (A3 to A4)
  - the transom hop (A1 to A2)
  - the balcony wall run (A4)
  - a negative check: a plain jump can't reach the balcony

## Changes from the plan

They are listed in `docs/DECISIONS.md`; these are the notable ones:
- The global light level was raised (exposure +1.5, lights x2.5) because the realtime-only interiors read as black. You can tune it with one number, `LookConfig.LightIntensityScale`.
- The sky is a generated pale haze cubemap. The procedural sky turned orange.
- The A4 balcony is reached by a wall run, not a surf. A scripted run showed head-on surfing can't climb that far.
- To make 000 readable from the boardroom, a stele and ground numerals were added, and the window mullions were moved off the centre line.

## Not done, or for you to judge

- **Performance is unmeasured.** The Editor was unfocused all night, so frame timings were meaningless. Profile the atrium on your machine. Knobs: `PortalConfig.ResolutionScale` and `MaxActive`, and `LookConfig.LightIntensityScale`.
- **Taste:**
  - overall brightness and fog density per area
  - text sizes on signs (calibrated by formula, not by eye)
  - how hard the directory wall run is
  - whether door 312 from the cafeteria balcony should let players skip the 37 puzzle (it does now: it's an alternate route)
- **Flaky test:** `Stairs_UpAndDown_StayGrounded_AndCameraGlides` failed once in a full run because of a frame hitch, and passes alone. It's in BACKLOG.
- **Not built:** board-marked concrete panel lines. The concrete is flat because the existing grid shader ignores interior lights. It's in BACKLOG.
