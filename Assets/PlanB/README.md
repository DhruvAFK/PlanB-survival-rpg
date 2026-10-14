# Plan B — The Last Shift

A standalone top-down facility survival demo. Everything is generated from code: two furnished maps, pixel art, characters, enemies, projectiles, sound, UI and gameplay. No reference sheet, sliced PNGs, external downloads, TMP essentials or Inspector wiring are required.

## Open and play

1. In Unity Hub, add/open `C:\UnityProjects\Plan_B\My project` with **6000.6.4f1**, the version already recorded in this local project.
2. Open `Assets/PlanB/Scenes/PlanB_Game.unity` and press **Play**. An empty scene or the original SampleScene also boots the demo automatically.
3. Select Fighter, Engineer or Scout, then choose **Enter the facility**. Controls and objectives appear on screen.

The reference repository uses 6000.6.3f1. The existing local project remains on its installed patch, 6000.6.4f1, to avoid changing original settings. The new gameplay code uses APIs available in Unity 2022.3 and Unity 6; only this installed Unity 6 version has been executed for verification.

## Controls

- WASD / arrow keys: move.
- Mouse: aim. Hold left mouse: shoot.
- R: reload. Empty magazine also reloads when firing if reserve remains.
- E: search cabinets, talk, insert fuse, use elevator or unlock containment.
- Right mouse / Space: melee bash, with a 0.6-second cooldown. Melee always remains available without ammunition.
- T: place one of up to two traps.
- Escape: pause/resume.

## Complete the shift

Start in the west bedroom on the residential floor. Find the fuse in southwest kitchen storage, insert it into the northeast maintenance generator, then use the south elevator. On the research floor, retrieve the key in the northeast server room. Unlock the red quarantine door east of the exit lobby, eliminate the Warden, and return to the south elevator to escape.

Fighter has 120 HP and two-damage melee. Engineer has 100 HP, 0.8-second reload and 12 extra starting reserve rounds. Scout has 90 HP and 25% faster movement. All use a 12-round pistol; standard reload is 1.2 seconds and shots deal one damage every 0.25 seconds. There is no health regeneration.

Floor 1 starts with eight Walkers and three Runners. Floor 2 starts with six Crawlers, four Spitters and the 25-HP Warden, which summons two Crawlers at half health. Enemy HP including summons totals **83**. Standard guaranteed ammunition totals **228**: 36 initial rounds, 144 from twelve loose boxes, and 48 from four marked guaranteed-ammo cabinets. This is 2.75 times enemy HP before random cabinets, drops or Engineer bonuses. Bash and traps provide further reserves against a soft lock.

## Build for Windows

Use **PlanB > Build Windows Demo** to produce `Builds/PlanB/PlanB.exe` directly without replacing the project's existing build-scene settings. Windows Build Support must be installed for the editor.

Alternatively, use **PlanB > Setup** to open the dedicated scene and make it the only enabled build scene, then **File > Build Profiles** on Unity 6 (or **File > Build Settings** on 2022.3), select Windows x86_64 and Build. Setup deliberately updates Build Settings when invoked; the initial installation leaves the original settings untouched.

## Implementation and art

New implementation files live under `Assets/PlanB`. The root `.gitignore` excludes Unity-generated files and local builds. Git branch: `plan-b-unity`.

`GameManager` bootstraps a single scene. `FloorManager` toggles two generated floor roots. `LevelBuilder` owns 48×28 ASCII maps, merged wall colliders, furniture and reusable breadth-first steering fields. Rigidbody2D actors use MovePosition. Projectiles raycast each movement segment to avoid tunneling. InputBridge supports both input backends with compile-time guards.

SpriteFactory recreates the chat reference's dark navy/charcoal palette, warm wood/amber residential rooms, blue metal laboratories, red Fighter, helmeted Engineer, hooded Scout, humanoid mutants, red-eyed spiders, scientist, items and objects. All sprites use point filtering and deterministic pixel drawing. Players have three frames in four directions. Guns, lasers, muzzle flashes, shadows, particles and UI are also procedural. SpriteLibrary can load future individual resource replacements, then falls back to code. **Zero sprites were sliced; every current sprite is procedural**, as requested.

Built optional features: edge darkness, warm/cold floor tint, scientists with three hints per floor, visible red security cone and ten-second alarm, traps with damage/stun, scrap/battery/wire salvage score, and the dimmed live residential level behind the title. The title backdrop is generated game scenery rather than a raster copy of the illustration.

## Verification and recovery

Unity compilation and automated play checks are documented in `RESUME.md`. The play check calls real gameplay components and uses actual physics projectiles; it also verifies map reachability, locked containment, progression, boss reinforcements, pause, survivor classes, victory, death and restart. It does not substitute for a human assessment of challenge or a timed 6–10-minute run.

Run the checks from a closed editor with:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe' -batchmode -nographics -job-worker-count 2 -projectPath 'C:\UnityProjects\Plan_B\My project' -executeMethod PlanB.Editor.PlanBSmokeTests.RunBatch -logFile 'C:\UnityProjects\Plan_B\My project\Logs\PlanBSmoke.log'
```

The asynchronous smoke check exits Unity itself; do not add `-quit`. It writes `Assets/PlanB/Verification/SmokeResult.txt`. `RunBatchCapture` additionally writes local screenshots when run with a graphics device. Generated verification files and logs are excluded from Git.

The Windows restart and Unity Safe Mode repair are recorded in `RECOVERY.md`. Original scenes, art, package manifests and project settings were preserved.

## Assumptions and limitations

- The supplied pasted brief defines the game; the latest chat instruction overrides its disk-sheet/slicing pipeline.
- The local Unity project is authoritative for the installed version; the remote reference was inspected read-only.
- Fighter's selectable 120-HP specification takes precedence over the brief's earlier 100-HP default.
- Pickups collect on proximity; E is reserved for object interactions.
- Maps prioritize broad passages and reliable completion. Furniture collisions are intentionally smaller than the artwork.
- Progress is in memory for each run. Restart fully rebuilds both maps; there is no save/load system.
- An OS blue-screen cause cannot be identified from its stop code alone. Project cache repair does not prove a driver or hardware fault has been resolved.
