# <Prototype name>

Copy this file to `docs/GDD.md` and fill every section from `docs/BRIEF.md`. Replace the instruction and the example under each heading. Keep it to two pages.

## 60-second pitch
What the player sees and does in the first minute. This is the definition of done.
> Example: In first person, the player wakes in a foggy greybox courtyard, presses Play, and has 60 seconds to find three green exit markers by climbing steps and slipping through door frames. Each find pulses the marker and ticks the counter.

## References
One line per image in `docs/references/`, from the owner's notes in `REFERENCES.md` plus what you see. Say what carries over (concrete: colours, distances, shapes, timings) and what is ignored. Write it so an agent who never opens the image can still build from it. Write "None" if the folder is empty.
> Example: courtyard-dusk.jpg (art direction, layout): low orange sun against purple fog -> fog 15-80 m tinted #7A5C9E, key light amber at 10 deg elevation; one open courtyard ~30 m across with steps on three sides. Ignored: characters, text overlay, lens blur.

## Core loop
Numbered steps. End with the single feel adjective that matters most (snappy / weighty / floaty).
> Example: 1. Look around for a green marker. 2. Plot a route over steps and through doors. 3. Reach and touch it. 4. The counter ticks up and the next marker lights. 5. Repeat until all three are found. Feel: **weighty**.

## Controls, camera, scale
Camera rig variant (default: **first-person**, CameraRig_FPS); move speed m/s; jump height m; player height m; unit = 1 m. Feel numbers come from PlayerConfig; list only the ones you change.
> Example: CameraRig_FPS, FOV 95 with sprint kick; move 6 m/s, sprint x1.5; jump 1.5 m; coyote time 0.1 s; player 1.8 m, eyes at 1.6 m. Keyboard+mouse and gamepad.

## Art direction (placeholders)
Look (`Volume_Default`: bloom, grade, fog), greybox material per role (default: **pastel greybox set**, see the role table in CLAUDE.md), fog, time of day.
> Example: Fog 15-80 m tinted purple, bloom 0.6. Floors Mat_Grey_Floor, walls Mat_Grid_Wall, levers Mat_Pastel_Blue (interactive), lava Mat_Pastel_Rose (hazard), exit door Mat_Pastel_Green (goal). Overcast noon.

## Features with acceptance criteria
- F1: <behavior>. Test: <setup> -> <action> -> <assert>.
> Example: F1: Dashing into an orb knocks it away. Test: orb 2 m ahead -> press Dash -> orb velocity > 5 m/s and ScoreChanged(1) published.

## Non-goals
What is explicitly out, so agents do not build it.
> Example: No enemies, no save system, no audio mixing beyond placeholder clips, no level select.

## Polish checklist
Copied from WORKFLOW.md, ticked by the polish agent.
- [ ] Tween on every state change
- [ ] Hit-stop / camera shake on impacts where relevant
- [ ] Particles on impacts and pickups
- [ ] Audio stubs on every action (placeholder clips allowed)
- [ ] Main menu, pause, game-over, restart all reachable
- [ ] Post-processing profile tuned to the GDD palette
- [ ] Title shown on boot
- [ ] Zero console errors or warnings in a full play-through
