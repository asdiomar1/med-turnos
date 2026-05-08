@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
cd /d "%~dp0"

set "LAUNCHER_IMAGE=MedicalCenter.Launcher.exe"
set "LAUNCH_MODE=%LAUNCH_ON_RUNNING%"
if not defined LAUNCH_MODE set "LAUNCH_MODE=prompt"
for /f "tokens=* delims= " %%A in ("%LAUNCH_MODE%") do set "LAUNCH_MODE=%%~A"
:TrimLaunchModeEnd
if defined LAUNCH_MODE if "!LAUNCH_MODE:~-1!"==" " (
    set "LAUNCH_MODE=!LAUNCH_MODE:~0,-1!"
    goto :TrimLaunchModeEnd
)

if /I not "%LAUNCH_MODE%"=="prompt" if /I not "%LAUNCH_MODE%"=="abort" if /I not "%LAUNCH_MODE%"=="kill" (
    echo [WARN] Valor invalido de LAUNCH_ON_RUNNING="%LAUNCH_MODE%". Se usara "prompt".
    set "LAUNCH_MODE=prompt"
)

call :FindLauncherPids
if defined RUNNING_PIDS (
    echo [INFO] Se detectaron instancias activas de %LAUNCHER_IMAGE%: !RUNNING_PIDS!

    if /I "%LAUNCH_MODE%"=="abort" (
        echo [ERROR] LAUNCH_ON_RUNNING=abort. Cerrando para evitar fallo de compilacion por archivo bloqueado.
        exit /b 10
    )

    if /I "%LAUNCH_MODE%"=="prompt" (
        choice /C YN /N /M "Hay instancias activas. Quieres cerrarlas y continuar? (Y/N): "
        if !ERRORLEVEL! EQU 2 (
            echo [ERROR] Operacion cancelada por el usuario.
            exit /b 11
        )
    )

    set "KILL_FAILED_PIDS="
    for %%P in (!RUNNING_PIDS!) do (
        taskkill /PID %%P /F /T >nul 2>&1
        if !ERRORLEVEL! NEQ 0 (
            if defined KILL_FAILED_PIDS (
                set "KILL_FAILED_PIDS=!KILL_FAILED_PIDS! %%P"
            ) else (
                set "KILL_FAILED_PIDS=%%P"
            )
        )
    )

    if defined KILL_FAILED_PIDS (
        echo [ERROR] No se pudieron finalizar los PID: !KILL_FAILED_PIDS!
        exit /b 12
    )

    call :FindLauncherPids
    if defined RUNNING_PIDS (
        echo [ERROR] Persisten procesos activos tras taskkill: !RUNNING_PIDS!
        exit /b 13
    )
)

dotnet build "tools\MedicalCenter.Launcher\MedicalCenter.Launcher.csproj" --verbosity quiet
if !ERRORLEVEL! NEQ 0 (
    set "BUILD_EXIT=!ERRORLEVEL!"
    echo [ERROR] Build failed con codigo !BUILD_EXIT!.
    pause
    exit /b !BUILD_EXIT!
)

"tools\MedicalCenter.Launcher\bin\Debug\net8.0\MedicalCenter.Launcher.exe"
set "LAUNCHER_EXIT=%ERRORLEVEL%"
exit /b %LAUNCHER_EXIT%

:FindLauncherPids
set "RUNNING_PIDS="
for /f "usebackq tokens=1,2 delims=," %%A in (`tasklist /FI "IMAGENAME eq %LAUNCHER_IMAGE%" /FO CSV /NH 2^>nul`) do (
    set "TASK_IMAGE=%%~A"
    set "TASK_PID=%%~B"
    if /I not "!TASK_IMAGE!"=="INFO: No tasks are running which match the specified criteria." (
        if /I "!TASK_IMAGE!"=="%LAUNCHER_IMAGE%" (
            if defined RUNNING_PIDS (
                set "RUNNING_PIDS=!RUNNING_PIDS! !TASK_PID!"
            ) else (
                set "RUNNING_PIDS=!TASK_PID!"
            )
        )
    )
)
goto :eof
