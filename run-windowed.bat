@echo off
REM Escape hatch beside the player exe (M64). Forces windowed 1280x720 + D3D11.
cd /d "%~dp0"
start "" "%~dp0Gatekeeper Bastion.exe" -screen-fullscreen 0 -screen-width 1280 -screen-height 720 -force-d3d11
