@echo off
setlocal
echo Starting Assembly and Battle: The Story in the Space...

set FLASH_PLAYER="C:\Dev\Flashpoint Infinity 14.0.3\FPSoftware\Flash\flashplayer_32_sa.exe"
set SWF="%~dp0game\a&b_space.swf"

if exist %FLASH_PLAYER% (
    start "" %FLASH_PLAYER% %SWF%
) else (
    echo Flash Player not found at %FLASH_PLAYER%.
    echo Opening with default system SWF association...
    start "" %SWF%
)
