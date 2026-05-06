@echo off
chcp 65001 >nul
cd /d "%~dp0"

dotnet build "tools\MedicalCenter.Launcher\MedicalCenter.Launcher.csproj" --verbosity quiet
if %ERRORLEVEL% NEQ 0 (
    echo Build failed.
    pause
    exit /b %ERRORLEVEL%
)

tools\MedicalCenter.Launcher\bin\Debug\net8.0\MedicalCenter.Launcher.exe
