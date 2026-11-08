@echo off
rem Збірка на Windows: програма (csc.exe з .NET Framework 4) і інсталятор (NSIS 3).
chcp 65001 >nul
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /target:winexe /codepage:65001 /optimize+ /out:app\myapp.exe /win32icon:app\src\app.ico ^
  /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll app\src\MyApp.cs || exit /b 1
set "MAKENSIS=%ProgramFiles(x86)%\NSIS\makensis.exe"
if not exist "%MAKENSIS%" set "MAKENSIS=%ProgramFiles%\NSIS\makensis.exe"
if not exist "%MAKENSIS%" (
  echo NSIS не знайдено. Встановіть його: https://nsis.sourceforge.io/Download
  exit /b 1
)
"%MAKENSIS%" /V2 installer\myapp_installer.nsi || exit /b 1
echo Готово: app\myapp.exe і installer\MyAppInstaller.exe
