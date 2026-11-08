@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM A&B Revival - Cinematic Export Pipeline
REM Rebuilds Mov1-Mov9 from the frozen SWF as 1920x1080 H.264 MP4 files.
REM FFDec renders the frames; Unity's MediaEncoder writes the MP4 files.
REM Usage: scripts\export-cinematics.bat
REM ============================================================================

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.."
set "REPO_ROOT=%CD%"
popd
set "PROJECT_DIR=%REPO_ROOT%\A&B-U"

set "UNITY_EXE="
if exist "C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" (
    set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"
) else (
    for /d %%D in ("C:\Program Files\Unity\Hub\Editor\6000*") do (
        if exist "%%D\Editor\Unity.exe" set "UNITY_EXE=%%D\Editor\Unity.exe"
    )
)

if "%UNITY_EXE%"=="" (
    echo [Cinematics ERROR] Unity Editor 6000.x was not found in the default Hub location.
    echo Set FFDEC_CLI and run the export from the Unity menu: Tools ^> A^&B Revival ^> Export Cinematics.
    exit /b 1
)

if not exist "%REPO_ROOT%\reports" mkdir "%REPO_ROOT%\reports"

echo [Cinematics] Exporting Mov1-Mov9 with Unity: "%UNITY_EXE%"
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod ABRevival.Editor.CinematicExporter.ExportAll -logFile "%REPO_ROOT%\reports\cinematics-export.log"
set CINEMATIC_EXIT=%ERRORLEVEL%

if %CINEMATIC_EXIT% NEQ 0 (
    echo [Cinematics ERROR] Export failed with exit code %CINEMATIC_EXIT%. See reports\cinematics-export.log.
    exit /b %CINEMATIC_EXIT%
)

echo [Cinematics] Export completed. Assets: A^&B-U\Assets\Resources\Cinematics
exit /b 0
