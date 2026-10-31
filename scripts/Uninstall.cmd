@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall.ps1"
if errorlevel 1 (
  echo Uninstallation failed or was cancelled. Review the message above.
  pause
  exit /b 1
)
