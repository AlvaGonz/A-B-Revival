@echo off
setlocal
cd /d "%~dp0\.."

echo ===============================================================================
echo             A^&B REVIVAL - RUN GAME WITH LIVE DEV TELEMETRY RECORDER
echo ===============================================================================
echo 1. Clearing previous flashlog.txt...
python scripts\record_dev_session.py --clear

echo 2. Launching Flash Player Standalone Debugger...
start "" "bin\flashplayer11_9r900_152_win_sa_debug.exe" "game\a&b_space_mod.swf"

echo 3. Starting Telemetry Watcher (logs will save to logs\dev_session.txt)...
echo    Press 'I' or '~' in game to toggle Dev Mode!
echo    Close the terminal or press Ctrl+C when finished.
echo ===============================================================================
python scripts\record_dev_session.py --watch
