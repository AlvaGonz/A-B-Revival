@echo off
setlocal
echo Configuring Git hooks directory to .githooks...
git config core.hooksPath .githooks
if %ERRORLEVEL% EQU 0 (
    echo Git pre-commit hooks successfully installed!
) else (
    echo Failed to configure core.hooksPath.
    exit /b %ERRORLEVEL%
)

