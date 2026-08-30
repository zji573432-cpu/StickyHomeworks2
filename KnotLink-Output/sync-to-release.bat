@echo off
REM sync-to-release.bat
setlocal enabledelayedexpansion 2>nul
set "SD=%~dp0"
set "PR=%SD%pr\"
set "RL=%SD%release\"

if "%1"=="" goto :menu
call :sync_one "%1"
goto :eof

:menu
echo.
echo   PR -^> Release Sync
echo   =====================
set "cnt=0"
for /d %%d in ("%PR%*") do (
    set /a cnt+=1
    set "id_!cnt!=%%~nxd"
    echo   [!cnt!] %%~nxd
)
if !cnt! equ 0 (echo No node dirs in pr/ & pause & exit /b 1)
echo.
if !cnt! equ 1 (set "APPID=!id_1!" & echo   Auto: !APPID!)
if !cnt! gtr 1 (
    set /p "CH=  Choose (1-!cnt!, Enter=all): "
    if "!CH!"=="" (for /l %%i in (1,1,!cnt!) do call :sync_one "!id_%%i!") else (set "APPID=!id_%CH%!")
)
call :sync_one "!APPID!"
echo. & pause
goto :eof

:sync_one
set "appid=%~1"
set "src=%PR%%appid%\"
set "dst=%RL%%appid%\"
if not exist "%src%" (echo [SKIP] %src% & goto :eof)
if not exist "%dst%" mkdir "%dst%" 2>nul
if exist "%src%standalone_manifest.json" (
    copy /Y "%src%standalone_manifest.json" "%dst%" >nul
    copy /Y "%src%FuncList.json" "%dst%" >nul
    echo [Standalone] %appid%
) else if exist "%src%plugin_manifest.json" (
    copy /Y "%src%plugin_manifest.json" "%dst%" >nul
    copy /Y "%src%FuncList.json" "%dst%" >nul
    if exist "%src%logo.png" copy /Y "%src%logo.png" "%dst%" >nul
    if exist "%src%logo.svg" copy /Y "%src%logo.svg" "%dst%" >nul
    if exist "%src%README.md" copy /Y "%src%README.md" "%dst%" >nul
    echo [Plugin] %appid%
) else (
    echo [ERROR] No manifest in %src%
)
goto :eof
