# Plan B checkpoint

Branch: `plan-b-unity`. All new game files are under `Assets/PlanB`; original project content is preserved. This file is the resume placeholder requested for the 50% usage limit and is maintained even when work finishes below the cap.

## Implemented

Complete runtime game, procedural reference-inspired art, both floors, all enemies and boss, objective chain, character selection, combat, cabinets/pickups, menus/HUD, audio/feedback, camera, all six optional features, dedicated scene and editor setup/build commands. No slicing script or disk reference sheet is required.

## Recovery status

The corrupt shared compiler-cache entry has been quarantined. Thirty damaged new Plan B metadata files have been repaired. Unity recognizes and compiles the game scripts and creates the scene. See `RECOVERY.md` for the separate Windows blue-screen evidence and its unresolved underlying cause.

## Verification status

**PASS — final Unity 6000.6.4f1 graphics-enabled play-through.** The same gameplay checks also passed without graphics. No C# compile errors or C# warnings appeared in the final run. The tests reported no game exceptions.

Verified title bootstrap, actual projectile damage against a collider, timed reload, pause, both ASCII map routes, fuse collection/consumption, generator power, elevator transition, key pickup, locked containment, dormant boss protection, half-health summons, boss defeat, exit victory, death/restart, all three character classes, traps and exactly one active floor after script reload/restart.

Read `Assets/PlanB/Verification/SmokeResult.txt`, `PolishResult.txt` and `Logs/PlanBMapPolishVerified.log` for the latest result. Seven rendered previews are saved locally under `Verification`; title, residential, research, open quarantine and both complete floor plans have been visually inspected. The old-floor visibility issue found during the original build was fixed and checked again. Prior failed runs are retained as diagnostic evidence.

Performance/audio polish is complete: warmed projectile/debris reuse, allocation-free sprite cache hits and unchanged HUD refreshes, throttled AI/physics scans, cached boss/actor references, 52 sound clips/variants, bounded voices, floor ambience, pause ducking and script-reload audio recovery. The full gameplay and new pool/allocation/audio checks passed without C# errors, C# warnings or game exceptions. All game code and documentation are saved in local commits; existing project content outside PlanB remains unchanged.

## October 16 UI/map/performance pass

Implemented and verified: redesigned terminal menus and HUD, survivor cards, minimap/player position/room names, richer procedural room details, continuous rugs, signs and lighting, corrected maintenance opening and proportional door art, transparent open apertures, consistent depth sorting and bounded camera follow.

Each floor now has 29 terrain renderers, with 82 total floor sprites on residential and 79 on research, down from 1,472 residential sprites. Residential transforms fell from 1,578 to 181. Six cached navigation fields avoid repeated rebuilds from alternating enemy targets. Shared map textures survive restarts and reload; furniture footprints and menu-card references also recover correctly.

Final graphics-enabled smoke passed all existing gameplay/audio/pool checks plus door fit, locked/open physical passage, minimap, camera bounds, map renderer budgets, 1,000 zero-allocation cached navigation calls and exactly six map textures after repeated restarts. No C# compile errors/warnings or game exceptions were reported.

Matched render probe: 120 warmed 1600×900 offscreen world renders, mean Camera.Render CPU 0.331 ms before / 0.213 ms after. This measures render submission/culling only, not full-player FPS. Reports: `Verification/MapProbe-baseline.txt`, `MapProbe-after.txt`; probe log: `Logs/PlanBMapAfterRender.log`.

The Windows build is being prepared; check `Logs/PlanBWindowsBuild.log` and `Builds/PlanB/PlanB.exe` before reporting build success. A human assessment of remaining lag is still needed.

The latest usage check reported 38% in the short window and 6% in the weekly window. The 50% stop threshold has not been reached.

## Next steps if work is interrupted

1. Check account usage before continuing; stop at 50% of either reported usage window.
2. Read the latest smoke result. Fix any failure and run `PlanB.Editor.PlanBSmokeTests.RunBatch` with the installed 6000.6.4f1 editor, `-batchmode -nographics -job-worker-count 2`, without `-quit`.
3. If feasible, run `RunBatchCapture` with graphics enabled and inspect title/residential/research/win screenshots under `Verification`.
4. Confirm `git diff` shows no original asset/settings changes and the branch is `plan-b-unity` before making further changes.
5. Remaining manual validation: human playtest keyboard/mouse/UI controls, actual smoothness, difficulty and 6–10-minute pacing. Check the Windows build status above; use PlanB > Build Windows Demo if another build is needed. Unity 2022.3 compatibility has been reviewed but not executed because that editor version is not installed.

This checkpoint contains no authentication tokens or private log contents.
