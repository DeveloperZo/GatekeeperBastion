@echo off
REM Gatekeeper's Protocol — interactive same-machine dual-box.
REM Requires Unity Editor open on this project for -Build.
REM Builds (optional), launches two windowed players side by side, NO auto-quit.
REM Close both windows yourself when done. Development Console should stay closed
REM if the player is clean of errors.

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0run.ps1" -Build -Interactive
echo.
echo Exit code %ERRORLEVEL% (0 = both connected in logs)
pause
exit /b %ERRORLEVEL%
