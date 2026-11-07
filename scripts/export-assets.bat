@echo off
setlocal
echo Exporting game assets from game\a^&b_space.swf...

set "FFDEC=C:\Program Files (x86)\FFDec\ffdec-cli.exe"
set "SWF=%~dp0..\game\a&b_space.swf"
set "OUT=%~dp0..\src\assets"

if not exist "%OUT%" mkdir "%OUT%"

echo Exporting images, shapes, sounds, and binary data...
"%FFDEC%" -export image,shape,sound,binaryData "%OUT%" "%SWF%"

echo Export complete!
