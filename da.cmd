@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0da.ps1" %*
exit /b %ERRORLEVEL%
