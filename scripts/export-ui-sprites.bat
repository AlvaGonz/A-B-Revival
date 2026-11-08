@echo off
setlocal
set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.."
set "REPO_ROOT=%CD%"
popd
set "PROJECT_DIR=%REPO_ROOT%\A&B-U"
set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"
set "LOG_FILE=%TEMP%\ab-ui-sprite-export.log"

if not exist "%UNITY_EXE%" (
    echo [ui-export] Unity 6.6.8f1 not found at "%UNITY_EXE%".
    exit /b 1
)

echo [ui-export] Exporting hangar and HUD sprites from the frozen UI library...
"%UNITY_EXE%" -batchmode -quit -projectPath "%PROJECT_DIR%" -executeMethod ABRevival.Editor.UiSpriteExportPipeline.ExportBatch -logFile "%LOG_FILE%"
set "EXIT_CODE=%ERRORLEVEL%"
if not "%EXIT_CODE%"=="0" (
    echo [ui-export] Failed with exit code %EXIT_CODE%. See "%LOG_FILE%".
    exit /b %EXIT_CODE%
)

echo [ui-export] Done. Manifest: docs\assets\ui-sprites.md
exit /b 0
