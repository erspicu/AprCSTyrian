@echo off
REM ============================================================
REM  AprCSTyrian - reset keylog records + C# config (clean baseline)
REM  Run this before re-recording a keylog / reproducing a bug.
REM ============================================================

echo ============================================================
echo   AprCSTyrian Reset  (keylog records + C# config)
echo ============================================================

REM --- keylog screenshots/log : original=temp\orig , C# replay=temp\cs ---
if exist "%~dp0temp\orig" rmdir /s /q "%~dp0temp\orig"
if exist "%~dp0temp\cs"   rmdir /s /q "%~dp0temp\cs"
del /q "%~dp0temp\cmp_*.png" >nul 2>&1
echo   [x] keylog records cleared  (temp\orig, temp\cs)

REM --- C# config/saves : now in config\ next to the exe (Build / Release) ---
if exist "%~dp0Build\config" rmdir /s /q "%~dp0Build\config"
if exist "%~dp0AprCSTyrianRelease\config" rmdir /s /q "%~dp0AprCSTyrianRelease\config"
if exist "%APPDATA%\AprCSTyrian" rmdir /s /q "%APPDATA%\AprCSTyrian"
echo   [x] C# config cleared  (Build\config, AprCSTyrianRelease\config, old %APPDATA%)

echo.
echo   Done. Clean state - ready to re-record keylog / reproduce bug.
echo ============================================================
pause
