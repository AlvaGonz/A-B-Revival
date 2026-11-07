# Forcing Adobe Flash Player Standalone to use the NVIDIA GTX 1650

Target: `C:\Dev\A&B revival\run-game.bat`, which launches
`C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer_32_sa.exe`
with `game\a&b_space_mod.swf` (fallback `a&b_space.swf`).

The machine has both an NVIDIA GeForce GTX 1650 and an AMD Radeon adapter;
by default Windows picks the power-saving (Radeon/integrated) adapter for the
Flash projector. The steps below force the 1650 for that executable.

## 1. Per-app GPU preference (done via registry)

Windows 10 20H1+ stores per-app GPU choice in

`HKEY_CURRENT_USER\Software\Microsoft\DirectX\UserGpuPreferences`

- Value name = full path to the executable
- Value data = `GpuPreference=2;` (2 = High performance = dedicated GPU)

Applied entry (REG_SZ):

`C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer_32_sa.exe` =
`GpuPreference=2;`

This is the same store written by Settings -> System -> Display -> Graphics ->
Options -> High performance, and on Win10 20H1+ it overrides any per-app
setting in NVIDIA Control Panel. (Sources: NVIDIA support KB "Run with graphics
processor missing from context menu"; laptopjudge.com registry reference.)

Manual equivalent if you ever need to redo it in the GUI:
Settings -> System -> Display -> Graphics -> "Desktop app" -> Browse ->
`flashplayer_32_sa.exe` -> Options -> High performance -> Save, then restart
the player.

## 2. Enable Flash's own hardware acceleration (manual, one-time)

The standalone projector does NOT read `mms.cfg` (that file is only for the
browser plugin). Hardware acceleration on/off is controlled in-player:

Right-click during playback -> Settings -> Display tab ->
check "Enable hardware acceleration".

This is stored in
`%APPDATA%\Macromedia\Flash Player\macromedia.com\support\flashplayer\sys\settings.sol`.
Without this checked, Flash ignores the GPU entirely and renders in software.

## 3. NVIDIA Control Panel backup (optional)

NVIDIA Control Panel -> Manage 3D Settings -> Program Settings -> Add
`flashplayer_32_sa.exe` -> "High-performance NVIDIA processor" -> Apply.
Useful as a fallback on older drivers, but Windows' per-app store takes
precedence on 20H1+.

## 4. Verify

1. Fully close and relaunch the game via `run-game.bat`.
2. Open Task Manager -> Performance -> GPU (or Details tab, enable "GPU
   engine" column) and confirm the Flash process shows the NVIDIA adapter
   while the game runs.

## Caveats (important)

- Flash Player on desktop renders 2D vector graphics in software (CPU) by
  design; the GPU is only used for H.264 video decoding, Stage3D content (if
  the SWF uses it), and full-screen scaling. Forcing the GPU therefore will
  NOT speed up a pure-2D Flash game; it only ensures any GPU-accelerated path
  uses the 1650 instead of the Radeon. (Source: Adobe Flash Platform "GPU
  rendering in Flash Player applications" doc.)
- If the game still shows the Radeon, check that the registry value name
  exactly matches the path the process actually uses (Task Manager -> Details
  -> right-click column headers), and that the player was fully restarted.
- `mms.cfg` is intentionally NOT created: it is ignored by the standalone
  projector and would have no effect.

## Sources

- NVIDIA support, "Run with graphics processor missing from context menu":
  https://nvidia.custhelp.com/app/answers/detail/a_id/5035
- laptopjudge.com, registry reference for UserGpuPreferences
- swfchan.com final-Flash readme (standalone projector ignores mms.cfg)
- Adobe Flash Platform, "GPU rendering in Flash Player applications"
  (help.adobe.com)
- Flashpoint Datahub, save-data locations (confirms %APPDATA%\Macromedia
  paths)
