@echo off
REM reg-dev.bat — Register PR directory for KnotLink discovery
setlocal enabledelayedexpansion
set "SD=%~dp0"
set "BS=\"
set "RK=HKCU%BS%Software%BS%KnotLink%BS%StandaloneNodes"

if "%1"=="" goto :menu
if /I "%1"=="list" goto :do_list
cls
set "appid=%~2"
if "%appid%"=="" call :detect_one
if "%appid%"=="" (echo Multiple AppIDs found, specify one & exit /b 1)
set "pp=%SD%pr%BS%%appid%"
if not exist "%pp%" (echo [ERROR] PR dir not found: %pp% & exit /b 1)
if /I "%1"=="C" (reg add "%RK%" /v "%appid%" /t REG_SZ /d "%pp%" /f 2>nul & echo [REG] %appid% -> %pp%)
if /I "%1"=="D" (reg delete "%RK%" /v "%appid%" /f 2>nul & echo [DEL] %appid%)
exit /b 0

:menu
echo.
echo   KnotLink Dev Register
echo   =======================
echo   [C] Create  [D] Delete  [L] List
echo.
set /p "ACT=  Choose [C/D/L]: "
if /I "!ACT!"=="L" goto :do_list
set "cnt=0"
for /d %%d in ("%SD%pr%BS%*") do (set /a cnt+=1 & set "id_!cnt!=%%~nxd" & echo   [!cnt!] %%~nxd)
if !cnt! equ 0 (echo [ERROR] No node dirs in pr/ & pause & exit /b 1)
echo.
if !cnt! equ 1 (set "APPID=!id_1!" & echo   Auto: !APPID!)
if !cnt! gtr 1 (set /p "CH=  Choose (1-!cnt!): " & set "APPID=!id_%CH%!" & if "!APPID!"=="" (echo Invalid & pause & exit /b 1))
set "FULLPATH=%SD%pr%BS%!APPID!"
if /I "!ACT!"=="C" (reg add "%RK%" /v "!APPID!" /t REG_SZ /d "!FULLPATH!" /f 2>nul & echo   [REG] !APPID! -^> !FULLPATH!)
if /I "!ACT!"=="D" (reg delete "%RK%" /v "!APPID!" /f 2>nul & echo   [DEL] !APPID!)
echo. & pause
exit /b 0

:detect_one
for /d %%d in ("%SD%pr%*") do (set /a cnt+=1 & set "found=%%~nxd")
if !cnt! equ 1 (set "appid=!found!" & echo [Auto] !appid!)
exit /b

:do_list
echo. & echo   Registered nodes: & echo   =================
reg query "%RK%" 2>nul || echo   (none)
echo. & pause & exit /b 0