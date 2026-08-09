@echo off
set UNITY="C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe"
set PROJ=f:\Programming\Gatekeeper Bastion
set LOGDIR=%PROJ%\Logs
if not exist "%LOGDIR%" mkdir "%LOGDIR%"

echo === ShipUiAssembler ===
%UNITY% -batchmode -nographics -quit -projectPath "%PROJ%" -executeMethod GatekeeperBastion.EditorTools.ShipUiAssembler.AssembleFromBatch -logFile "%LOGDIR%\m35_ship_assemble.log"
set SHIP_EXIT=%ERRORLEVEL%
echo SHIP_EXIT=%SHIP_EXIT%

echo === ShiftFrontEndAssembler ===
%UNITY% -batchmode -nographics -quit -projectPath "%PROJ%" -executeMethod GatekeeperBastion.EditorTools.ShiftFrontEndAssembler.BuildFromBatch -logFile "%LOGDIR%\m35_frontend_assemble.log"
set FE_EXIT=%ERRORLEVEL%
echo FE_EXIT=%FE_EXIT%

echo === BastionUiAssembler ===
%UNITY% -batchmode -nographics -quit -projectPath "%PROJ%" -executeMethod GatekeeperBastion.EditorTools.BastionUiAssembler.AssembleFromBatch -logFile "%LOGDIR%\m35_bastion_assemble.log"
set BASTION_EXIT=%ERRORLEVEL%
echo BASTION_EXIT=%BASTION_EXIT%

echo SHIP_EXIT=%SHIP_EXIT%> "%LOGDIR%\m35_assemble_exits.txt"
echo FE_EXIT=%FE_EXIT%>> "%LOGDIR%\m35_assemble_exits.txt"
echo BASTION_EXIT=%BASTION_EXIT%>> "%LOGDIR%\m35_assemble_exits.txt"

if %SHIP_EXIT% NEQ 0 exit /b %SHIP_EXIT%
if %FE_EXIT% NEQ 0 exit /b %FE_EXIT%
if %BASTION_EXIT% NEQ 0 exit /b %BASTION_EXIT%
exit /b 0
