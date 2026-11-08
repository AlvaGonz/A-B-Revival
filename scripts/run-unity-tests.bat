@echo off
setlocal enabledelayedexpansion

REM ============================================================================
REM A&B Revival - Unity Automated Test Runner (ADR 0016)
REM Tier 1: Fast standalone CLI (.NET SDK) - ~150ms inner loop
REM Tier 2: Headless Unity UTF EditMode and PlayMode - Full engine integration
REM Handles ampersand '&' in repository path cleanly.
REM ============================================================================

set "SCRIPT_DIR=%~dp0"
pushd "%SCRIPT_DIR%.."
set "REPO_ROOT=%CD%"
popd
set "PROJECT_DIR=%REPO_ROOT%\A&B-U"
set "TEST_PROJ=%PROJECT_DIR%\Assets\Scripts\Tests\ABRevival.Tests.csproj"

if "%1"=="--unity" goto RUN_UNITY_UTF
if "%1"=="-u" goto RUN_UNITY_UTF
if "%1"=="--all" goto RUN_ALL

:RUN_TIER_1
echo [TestRunner] Running Tier 1 Fast CLI .NET Test Suite...
dotnet test "%TEST_PROJ%" --nologo
set TEST_EXIT=%ERRORLEVEL%
if %TEST_EXIT% NEQ 0 (
    echo [TestRunner ERROR] Tier 1 tests failed with exit code %TEST_EXIT%.
    exit /b %TEST_EXIT%
)
echo [TestRunner] Tier 1 tests completed successfully.
if "%1"=="" exit /b 0
goto :EOF

:RUN_UNITY_UTF
echo [TestRunner] Locating Unity 6.6 Editor...
set "UNITY_EXE="
if exist "C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe" (
    set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.6.8f1\Editor\Unity.exe"
) else (
    for /d %%D in ("C:\Program Files\Unity\Hub\Editor\6000*") do (
        if exist "%%D\Editor\Unity.exe" set "UNITY_EXE=%%D\Editor\Unity.exe"
    )
)

if "%UNITY_EXE%"=="" (
    echo [TestRunner WARNING] Unity Editor 6000.x executable not found in default Hub locations.
    echo Please ensure Unity 6.6 (6000.6.8f1) is installed or run Tier 1 tests via 'dotnet test'.
    exit /b 1
)

echo [TestRunner] Found Unity Editor: "%UNITY_EXE%"
if not exist "%REPO_ROOT%\reports" mkdir "%REPO_ROOT%\reports"

echo [TestRunner] Running Tier 2 Headless Unity UTF EditMode tests...
"%UNITY_EXE%" -batchmode -projectPath "%PROJECT_DIR%" -runTests -testPlatform EditMode -testResults "%REPO_ROOT%\reports\unity-editmode-results.xml" -logFile "%REPO_ROOT%\reports\unity-editmode.log"
set UNITY_EXIT=%ERRORLEVEL%
if %UNITY_EXIT% EQU 0 (
    echo [TestRunner] Tier 2 Unity EditMode tests passed!
) else (
    echo [TestRunner ERROR] Unity EditMode tests exited with code %UNITY_EXIT%. See reports\unity-editmode.log.
    exit /b %UNITY_EXIT%
)

echo [TestRunner] Running Tier 2 Headless Unity UTF PlayMode tests...
"%UNITY_EXE%" -batchmode -projectPath "%PROJECT_DIR%" -runTests -testPlatform PlayMode -testResults "%REPO_ROOT%\reports\unity-playmode-results.xml" -logFile "%REPO_ROOT%\reports\unity-playmode.log"
set UNITY_EXIT=%ERRORLEVEL%
if %UNITY_EXIT% EQU 0 (
    echo [TestRunner] Tier 2 Unity PlayMode tests passed!
) else (
    echo [TestRunner ERROR] Unity PlayMode tests exited with code %UNITY_EXIT%. See reports\unity-playmode.log.
    exit /b %UNITY_EXIT%
)
goto :EOF

:RUN_ALL
call :RUN_TIER_1
if %ERRORLEVEL% NEQ 0 exit /b %ERRORLEVEL%
call :RUN_UNITY_UTF
exit /b %ERRORLEVEL%
