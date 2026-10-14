# Plan B checkpoint

Branch: `plan-b-unity`. All new game files are under `Assets/PlanB`; original project content is preserved. This file is the resume placeholder requested for the 50% usage limit and is maintained even when work finishes below the cap.

## Implemented

Complete runtime game, procedural reference-inspired art, both floors, all enemies and boss, objective chain, character selection, combat, cabinets/pickups, menus/HUD, audio/feedback, camera, all six optional features, dedicated scene and editor setup/build commands. No slicing script or disk reference sheet is required.

## Recovery status

The corrupt shared compiler-cache entry has been quarantined. Thirty damaged new Plan B metadata files have been repaired. Unity recognizes and compiles the game scripts and creates the scene. See `RECOVERY.md` for the separate Windows blue-screen evidence and its unresolved underlying cause.

## Verification status

**PASS — final Unity 6000.6.4f1 graphics-enabled play-through.** The same gameplay checks also passed without graphics. No C# compile errors or C# warnings appeared in the final run. The tests reported no game exceptions.

Verified title bootstrap, actual projectile damage against a collider, timed reload, pause, both ASCII map routes, fuse collection/consumption, generator power, elevator transition, key pickup, locked containment, dormant boss protection, half-health summons, boss defeat, exit victory, death/restart, all three character classes, traps and exactly one active floor after script reload/restart.

Read `Assets/PlanB/Verification/SmokeResult.txt` and `Logs/PlanBFinalVerification.log` for the result. Four rendered previews are saved locally under `Verification`; title, residential and research images were visually inspected. The old-floor visibility issue found by that inspection was fixed and checked again. Prior failed runs are retained as diagnostic evidence.

All game code and documentation are saved in local milestone commits. Existing tracked project content remains unchanged. The last usage check reported 7% for both the short and weekly windows; the short window reset during this work. The 50% stop threshold was not reached.

## Next steps if work is interrupted

1. Check account usage before continuing; stop at 50% of either reported usage window.
2. Read the latest smoke result. Fix any failure and run `PlanB.Editor.PlanBSmokeTests.RunBatch` with the installed 6000.6.4f1 editor, `-batchmode -nographics -job-worker-count 2`, without `-quit`.
3. If feasible, run `RunBatchCapture` with graphics enabled and inspect title/residential/research/win screenshots under `Verification`.
4. Confirm `git diff` shows no original asset/settings changes and the branch is `plan-b-unity` before making further changes.
5. Remaining manual validation: human playtest the keyboard/mouse/UI controls, difficulty and actual 6–10-minute pacing. A Windows executable has not been built; use PlanB > Build Windows Demo when needed. Unity 2022.3 compatibility has been reviewed but not executed because that editor version is not installed.

This checkpoint contains no authentication tokens or private log contents.
