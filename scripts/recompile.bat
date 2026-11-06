@echo off
setlocal
echo Recompiling SWF with modified ActionScript 3 sources...

set FFDEC="C:\Program Files (x86)\FFDec\ffdec-cli.exe"
set IN_SWF="%~dp0..\game\a&b_space.swf"
set OUT_SWF="%~dp0..\game\a&b_space_mod.swf"
set SCRIPTS="%~dp0..\src\as3\scripts"

%FFDEC% -importScript %IN_SWF% %OUT_SWF% %SCRIPTS%

if %ERRORLEVEL% EQU 0 (
    echo Build SUCCESS: %OUT_SWF% created!
) else (
    echo Build FAILED with error code %ERRORLEVEL%.
)

