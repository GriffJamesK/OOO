# Tests
- Edit mode (`EditMode/`, asmdef `Kit.Tests.EditMode`): pure logic, no frames. Copy a method from `CoreTests.cs`: `[Test] public void Thing_DoesX() { /* arrange, act */ Assert.AreEqual(expected, actual); }`. Step time by hand (`TimerScheduler.Tick`, `TweenState.Step`); never wait.
- Play mode (`PlayMode/`, asmdef `Kit.Tests.PlayMode`): real frames. `[UnityTest] public IEnumerator Thing() { /* setup */ yield return null; Assert... }`. For input, derive from `InputTestFixture`, call `KitInput.Reset()` after `base.Setup()`, and drive devices with `Set(gamepad.leftStick, v)`.
- Run through MCP: `run_tests` with `mode: "EditMode"` or `mode: "PlayMode"` (PlayMode: `init_timeout: 120000`), then `get_test_job` with the returned `job_id` and `wait_timeout: 60`.
- CLI fallback (only when no Editor has the project open):
  `Unity -batchmode -runTests -testPlatform EditMode -projectPath . -testResults results.xml`
  `Unity -batchmode -runTests -testPlatform PlayMode -projectPath . -testResults results-play.xml`
- Or run `Tools/run-tests.ps1`, which finds the pinned Unity version and runs both.
- Any `Debug.LogError` or exception during a test fails it. Keep the console clean.
- Creating colliders in a test? Call `Physics.SyncTransforms()` after positioning them, or a fast-falling CharacterController can pass through before physics sees them.
- In `InputTestFixture`, change one control per step when a button release matters (stick first, release the button a frame later) and assert `isPressed`; a lost release makes the next Press invisible to `WasPressedThisFrame`.
- Drive play-mode input tests with a virtual `Gamepad`: the Input System resets keyboards when the Editor is unfocused and in batch mode.
- In play mode, wait in game time (`WaitForSeconds`), not frame counts: test-runner frames can be milliseconds long, and movement eases in.
- New system? Add one edit-mode test for its logic and, if it has input or scenes, one play-mode test.
