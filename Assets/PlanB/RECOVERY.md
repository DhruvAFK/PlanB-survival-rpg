# Recovery after the Windows restart — 15 October 2026

## Confirmed evidence

Windows System event 1001 at 00:47:51 IST records bugcheck `0x00000050`. Event 41 records an unclean restart. The preserved kernel minidump is `C:\Windows\Minidump\101526-24375-01.dmp` (5,763,643 bytes).

[Microsoft identifies 0x50 as PAGE_FAULT_IN_NONPAGED_AREA](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/bug-check-0x50--page-fault-in-nonpaged-area). The code has multiple possible causes. No blamed driver has been established. Windows Kits contains debugger support DLLs but no debugger executable, so a symbol-based analysis was not possible with installed tools. No drivers, registry settings, security settings or OS files were changed.

Unity's post-restart logs contained `PostProcessing failed: System.InvalidOperationException: PE image does not have metadata` while processing `Unity.Collections.Tests.CoreCLR.ProtectedJobNested.dll`. Inspection found that this 5,120-byte generated DLL consisted of zero bytes. A fresh local cache rebuild restored the same invalid file from Unity's shared Bee cache. Running Unity's compiler with the same generated response files produced a valid assembly, confirming the source was usable.

Thirty new Plan B `.meta` files were also filled with zero bytes. They contained no usable GUIDs. The new source files themselves were checked for null bytes and were intact. Existing tracked project files have not been changed.

## Repairs made

1. Preserved generated caches as `Library/Bee.before-recovery-20261015` and `Library/ScriptAssemblies.before-recovery-20261015`.
2. Recompiled the affected generated package DLL from its existing compiler response files.
3. Quarantined the single damaged shared cache directory by renaming it with `.corrupted-20261015` under `C:\Users\Dhruv\AppData\Local\Unity\Caches\bee`. Its cache key was `91aa2a3de7ebd2e040bb75b066f4b39400000000000000000000000000000006`.
4. Rebuilt the 30 damaged metadata files belonging only to the newly created Plan B folder.
5. Verified all 322 generated DLL headers after recompilation. Unity subsequently compiled the actual Plan B source and generated its scene.
6. Replaced deprecated physics query calls and hardened bootstrap/floor cleanup after script reload. Both graphics-disabled and Direct3D 11 play-through checks passed with two worker threads. Visual inspection caught and resolved an old-floor visibility issue after script reload.

Logs remain under `Logs/PlanBRecoveryCompile.log`, `Logs/PlanBRecoveryVerified.log` and `Logs/PlanBRecoveryImport.log`. The first failed attempts are retained as diagnostic evidence; the final verification status is in `RESUME.md`.

The invalid generated files explain Unity's Safe Mode prompt. They do not establish the root cause of the Windows blue screen. If another blue screen occurs, the saved kernel dump needs analysis with a Windows debugger and matching symbols. No stress tests or broad cache deletion were performed.
