@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0\.."

echo ===============================================================================
echo             A^&B REVIVAL - ADOBE AIR DESKTOP / MOBILE RUNTIME LAUNCHER
echo ===============================================================================

set "AIR_DESCRIPTOR=src\air\application.xml"
set "TARGET_SWF=game\a&b_space_mod.swf"

if not exist "!TARGET_SWF!" (
    echo [ERROR] SWF not found at !TARGET_SWF!. Running recompile first...
    call scripts\recompile.bat
    if errorlevel 1 exit /b 1
)

:: Locate ADL (AIR Debug Launcher)
set "ADL_CMD="
if exist "C:\Dev\AIRSDK_51.5.1\bin\adl.exe" (
    set "ADL_CMD=C:\Dev\AIRSDK_51.5.1\bin\adl.exe"
) else if exist "C:\AIRSDK\bin\adl.exe" (
    set "ADL_CMD=C:\AIRSDK\bin\adl.exe"
) else if exist "C:\AdobeAIRSDK\bin\adl.exe" (
    set "ADL_CMD=C:\AdobeAIRSDK\bin\adl.exe"
) else if exist "C:\Dev\AIRSDK\bin\adl.exe" (
    set "ADL_CMD=C:\Dev\AIRSDK\bin\adl.exe"
) else if defined AIR_HOME (
    if exist "!AIR_HOME!\bin\adl.exe" set "ADL_CMD=!AIR_HOME!\bin\adl.exe"
) else (
    for /d %%D in ("C:\Dev\AIRSDK*") do (
        if exist "%%D\bin\adl.exe" set "ADL_CMD=%%D\bin\adl.exe"
    )
)

if not defined ADL_CMD (
    where adl >nul 2>&1
    if !errorlevel! equ 0 set "ADL_CMD=adl"
)

if not defined ADL_CMD (
    echo [NOTE] Adobe AIR SDK ^(adl.exe^) was not found in PATH or standard directories.
    echo.
    echo To run A^&B Revival natively via HARMAN Adobe AIR:
    echo 1. Download the free HARMAN AIR SDK for Windows from:
    echo    https://airsdk.harman.com/download
    echo 2. Extract to C:\AIRSDK ^(or C:\Dev\AIRSDK, or add its \bin folder to PATH^).
    echo 3. Re-run this script!
    echo.
    echo Falling back to Flash Player Standalone Debugger...
    if exist "C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer11_9r900_152_win_sa_debug.exe" (
        start "" "C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer11_9r900_152_win_sa_debug.exe" "!TARGET_SWF!"
    ) else (
        start "" "!TARGET_SWF!"
    )
    exit /b 0
)

echo [OK] Found Adobe AIR SDK: !ADL_CMD!
echo [INFO] Launching !AIR_DESCRIPTOR! with direct renderMode (Stage3D enabled)...
start "" "!ADL_CMD!" "!AIR_DESCRIPTOR!" "game"
