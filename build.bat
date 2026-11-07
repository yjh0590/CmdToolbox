@echo off
rem Build with the .NET Framework compiler bundled with Windows
cd /d "%~dp0"
"%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo /codepage:65001 /target:winexe /win32icon:app.ico /out:CmdToolbox.exe CmdToolbox.cs
if %errorlevel%==0 echo Build OK: CmdToolbox.exe
