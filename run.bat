@echo off
setlocal
cd /d "%~dp0"

echo [Everyview] Initializing Voidtools Everything Engine & Launching Everyview...

:: Start Voidtools Everything in background if installed
if exist "%ProgramFiles%\Everything\Everything.exe" (
    start "" "%ProgramFiles%\Everything\Everything.exe" -startup
) else if exist "%ProgramFiles(x86)%\Everything\Everything.exe" (
    start "" "%ProgramFiles(x86)%\Everything\Everything.exe" -startup
) else if exist "%LOCALAPPDATA%\Programs\Everything\Everything.exe" (
    start "" "%LOCALAPPDATA%\Programs\Everything\Everything.exe" -startup
)

set "EXE_PATH=%~dp0bin\Release\net10.0-windows\Everyview.exe"
if exist "%EXE_PATH%" goto LAUNCH

set "EXE_PATH=%~dp0bin\Debug\net10.0-windows\Everyview.exe"
if exist "%EXE_PATH%" goto LAUNCH

echo ========================================================
echo  Everyview executable not found.
echo  Building project in Release mode with .NET...
echo ========================================================
echo.
dotnet build -c Release
echo.

set "EXE_PATH=%~dp0bin\Release\net10.0-windows\Everyview.exe"
if exist "%EXE_PATH%" goto LAUNCH

echo.
echo [ERROR] Could not build or locate Everyview.exe.
echo Please ensure .NET 10 SDK is installed and try again.
echo.
pause
exit /b 1

:LAUNCH
start "" "%EXE_PATH%"
exit /b 0
