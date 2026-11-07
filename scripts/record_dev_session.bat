@echo off
setlocal
cd /d "%~dp0\.."
python scripts\record_dev_session.py %*
pause

