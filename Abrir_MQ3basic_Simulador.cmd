@echo off
setlocal

rem OBS installs a global Vulkan layer that conflicts with Meta XR Simulator.
rem Disable it only for this Unity process; OBS remains installed and unchanged.
set "VK_LOADER_LAYERS_DISABLE=VK_LAYER_OBS_HOOK"

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.3.21f1\Editor\Unity.exe"
if not exist "%UNITY_EXE%" (
    echo No se encontro Unity 6000.3.21f1 en:
    echo %UNITY_EXE%
    pause
    exit /b 1
)

start "MQ3basic - Meta XR Simulator" "%UNITY_EXE%" -projectPath "%~dp0" -force-d3d11
endlocal
