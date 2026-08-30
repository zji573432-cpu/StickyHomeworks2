@echo off
REM version.bat - Manage app version and manifest version
setlocal enabledelayedexpansion
set "SD=%~dp0"
set "PR=%SD%pr\"
set "RL=%SD%release\"

if "%1"=="" goto :menu

set "CMD=%~1" & set "APPID=%~2"
if "%APPID%"=="" for /d %%d in ("%PR%*") do set "APPID=%%~nxd"
if "%APPID%"=="" (echo No AppID & pause & exit /b 1)

if /I "%CMD%"=="show" (call :show "%APPID%" & exit /b 0)
if /I "%CMD%"=="set" (
    if "%~3"=="" (echo Usage: version.bat set ^<AppID^> ^<appVer^> ^<manifestVer^> & pause & exit /b 1)
    call :update "%APPID%" "%~3" "%~4"
    exit /b 0
)
pause & exit /b 0

:menu
echo.
echo   Version Manager
echo   =================
set "cnt=0"
for /d %%d in ("%PR%*") do (
    set /a cnt+=1
    set "id_!cnt!=%%~nxd"
    echo   [!cnt!] %%~nxd
)
if !cnt! equ 0 (echo   No node dirs in pr/ - run reg-dev.bat first & pause & exit /b 0)
echo.
if !cnt! equ 1 (set "APPID=!id_1!" & echo   Auto: !APPID!)
if !cnt! gtr 1 (
    set /p "CH=  Choose (1-!cnt!): "
    set "APPID=!id_%CH%!"
    if "!APPID!"=="" (echo Invalid & pause & exit /b 1)
)
echo.
call :show "!APPID!"
echo.
set /p "V1=  App version (enter=skip): "
set /p "V2=  Manifest version (enter=skip): "
if "!V1!"=="" if "!V2!"=="" (echo Nothing to update & pause & exit /b 0)
call :update "!APPID!" "!V1!" "!V2!"
echo. & pause
exit /b 0

:show
set "appid=%~1"
set "mf=%PR%%appid%\plugin_manifest.json"
if not exist "!mf!" set "mf=%PR%%appid%\standalone_manifest.json"
set "fl=%PR%%appid%\FuncList.json"
set "AVER=???" & set "MVER=???"
if exist "!mf!" for /f "usebackq tokens=*" %%v in (`powershell -NoProfile -Command "try{$j=Get-Content -Raw -Encoding UTF8 '!mf!'|ConvertFrom-Json;Write-Output $j.version}catch{try{$j=Get-Content -Raw '!mf!'|ConvertFrom-Json;Write-Output $j.version}catch{Write-Output '???'}}" 2^>nul`) do set "AVER=%%v"
if exist "!fl!" for /f "usebackq tokens=*" %%v in (`powershell -NoProfile -Command "try{$j=Get-Content -Raw -Encoding UTF8 '!fl!'|ConvertFrom-Json;Write-Output $j.manifestVersion}catch{try{$j=Get-Content -Raw '!fl!'|ConvertFrom-Json;Write-Output $j.manifestVersion}catch{Write-Output '???'}}" 2^>nul`) do set "MVER=%%v"
echo   App version     : !AVER!
echo   Manifest version: !MVER!
exit /b 0

:update
set "AV=%~2" & set "MV=%~3"
for %%b in ("%PR%%~1" "%RL%%~1") do (
    if exist "%%~b" (
        if not "!AV!"=="" for %%f in ("%%~b\plugin_manifest.json" "%%~b\standalone_manifest.json") do (
            if exist "%%f" powershell -NoProfile -Command "$c=Get-Content -Raw -Encoding UTF8 '%%f';$c=$c -replace '(""version""):\s*"".*?""','$1: ""!AV!""';Set-Content -Encoding UTF8 '%%f' -Value $c -NoNewline" 2>nul
        )
        if not "!MV!"=="" if exist "%%~b\FuncList.json" (
            powershell -NoProfile -Command "$c=Get-Content -Raw -Encoding UTF8 '%%~b\FuncList.json';$c=$c -replace '(""manifestVersion""):\s*"".*?""','$1: ""!MV!""';Set-Content -Encoding UTF8 '%%~b\FuncList.json' -Value $c -NoNewline" 2>nul
        )
    )
)
if not "!AV!"=="" echo   App version     -^> !AV!
if not "!MV!"=="" echo   Manifest version -^> !MV!
exit /b 0
