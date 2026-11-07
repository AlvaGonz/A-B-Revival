@echo off
setlocal enabledelayedexpansion
cd /d "%~dp0\.."

echo ===============================================================================
echo            A^&B REVIVAL - ADOBE AIR ANDROID APK PACKAGER
echo ===============================================================================

set "AIR_DESCRIPTOR=src\air\application.xml"
set "TARGET_SWF=game\a&b_space_mod.swf"
set "OUTPUT_DIR=dist"
set "OUTPUT_APK=dist\AssemblyAndBattle.apk"
set "CERT_FILE=scripts\air_dev_cert.p12"
set "CERT_PASS=abrevival"

if not exist "!OUTPUT_DIR!" mkdir "!OUTPUT_DIR!"

if not exist "!TARGET_SWF!" (
    echo [ERROR] SWF not found at !TARGET_SWF!. Running recompile first...
    call scripts\recompile.bat
    if errorlevel 1 exit /b 1
)

:: Locate ADT (AIR Developer Tool)
set "ADT_CMD="
if exist "C:\Dev\AIRSDK_51.5.1\bin\adt.bat" (
    set "ADT_CMD=C:\Dev\AIRSDK_51.5.1\bin\adt.bat"
) else if exist "C:\AIRSDK\bin\adt.bat" (
    set "ADT_CMD=C:\AIRSDK\bin\adt.bat"
) else if exist "C:\AdobeAIRSDK\bin\adt.bat" (
    set "ADT_CMD=C:\AdobeAIRSDK\bin\adt.bat"
) else if exist "C:\Dev\AIRSDK\bin\adt.bat" (
    set "ADT_CMD=C:\Dev\AIRSDK\bin\adt.bat"
) else if defined AIR_HOME (
    if exist "!AIR_HOME!\bin\adt.bat" set "ADT_CMD=!AIR_HOME!\bin\adt.bat"
) else (
    for /d %%D in ("C:\Dev\AIRSDK*") do (
        if exist "%%D\bin\adt.bat" set "ADT_CMD=%%D\bin\adt.bat"
    )
)

if not defined ADT_CMD (
    where adt >nul 2>&1
    if !errorlevel! equ 0 set "ADT_CMD=adt"
)

if not defined ADT_CMD (
    echo [NOTE] Adobe AIR SDK (adt.bat) was not found in PATH or standard directories.
    echo.
    echo To package Android APKs:
    echo 1. Download the free HARMAN AIR SDK for Windows from:
    echo    https://airsdk.harman.com/download
    echo 2. Extract to C:\AIRSDK (or set AIR_HOME environment variable).
    echo 3. The packaging command is:
    echo    adt -package -target apk-captive-runtime -storetype pkcs12 -keystore !CERT_FILE! -storepass !CERT_PASS! !OUTPUT_APK! !AIR_DESCRIPTOR! -C game a^&b_space_mod.swf
    exit /b 0
)

:: Generate self-signed certificate if not present
if not exist "!CERT_FILE!" (
    echo [INFO] Generating self-signed development certificate: !CERT_FILE!
    call "!ADT_CMD!" -certificate -cn "ABRevivalDev" 2048-RSA "!CERT_FILE!" !CERT_PASS!
    if errorlevel 1 (
        echo [ERROR] Failed to generate developer signing certificate.
        exit /b 1
    )
)

echo [OK] Using Adobe AIR SDK: !ADT_CMD!
echo [INFO] Packaging Android APK: !OUTPUT_APK!...
call "!ADT_CMD!" -package -target apk-captive-runtime -storetype pkcs12 -keystore "!CERT_FILE!" -storepass !CERT_PASS! "!OUTPUT_APK!" "!AIR_DESCRIPTOR!" -C game "a&b_space_mod.swf"
if errorlevel 1 (
    echo [ERROR] APK packaging failed.
    exit /b 1
)

echo [SUCCESS] Android APK created successfully at: !OUTPUT_APK!
echo To install on a connected Android phone via ADB:
echo   adb install -r "!OUTPUT_APK!"
