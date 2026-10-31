@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install.ps1"
if errorlevel 1 (
  echo Installation failed or was cancelled. Review the message above.
  pause
  exit /b 1
)
