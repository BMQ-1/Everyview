@echo off
setlocal
cd /d "%~dp0"

:: Check if Voidtools Everything client is already running in current user session
set "EVERYTHING_IN_SESSION="
for /f "tokens=1" %%P in ('tasklist /FI "IMAGENAME eq Everything.exe" /FI "SESSION ne 0" /NH 2^>nul ^| find /I "Everything.exe"') do (
    set "EVERYTHING_IN_SESSION=1"
)

:: Start Voidtools Everything daemon ONLY if not already running in the user session
if not defined EVERYTHING_IN_SESSION (
    if exist "%ProgramFiles%\Everything\Everything.exe" (
        start "" "%ProgramFiles%\Everything\Everything.exe" -startup
    ) else if exist "%ProgramFiles(x86)%\Everything\Everything.exe" (
        start "" "%ProgramFiles(x86)%\Everything\Everything.exe" -startup
    ) else if exist "%LOCALAPPDATA%\Programs\Everything\Everything.exe" (
        start "" "%LOCALAPPDATA%\Programs\Everything\Everything.exe" -startup
    ) else if exist "%ProgramFiles%\Everything 1.5a\Everything64.exe" (
        start "" "%ProgramFiles%\Everything 1.5a\Everything64.exe" -startup
    )
)

:: Locate Everyview executable
set "EXE_PATH="
if exist "%~dp0bin\Release\net10.0-windows\Everyview.exe" set "EXE_PATH=%~dp0bin\Release\net10.0-windows\Everyview.exe"
if not defined EXE_PATH if exist "%~dp0publish\Everyview-v1.1-win-x64\Everyview.exe" set "EXE_PATH=%~dp0publish\Everyview-v1.1-win-x64\Everyview.exe"
if not defined EXE_PATH if exist "%~dp0publish\Everyview-v1.1-Portable\Everyview.exe" set "EXE_PATH=%~dp0publish\Everyview-v1.1-Portable\Everyview.exe"
if not defined EXE_PATH if exist "%~dp0bin\Debug\net10.0-windows\Everyview.exe" set "EXE_PATH=%~dp0bin\Debug\net10.0-windows\Everyview.exe"

if defined EXE_PATH goto LAUNCH

echo ========================================================
echo  Everyview executable not found.
echo  Building project in Release mode with .NET...
echo ========================================================
echo.
dotnet build -c Release
echo.

if exist "%~dp0bin\Release\net10.0-windows\Everyview.exe" (
    set "EXE_PATH=%~dp0bin\Release\net10.0-windows\Everyview.exe"
    goto LAUNCH
)

echo.
echo [ERROR] Could not build or locate Everyview.exe.
echo Please ensure .NET 10 SDK is installed and try again.
echo.
pause
exit /b 1

:LAUNCH
for %%F in ("%EXE_PATH%") do set "EXE_DIR=%%~dpF"
start "" /d "%EXE_DIR%" "%EXE_PATH%"
exit /b 0
