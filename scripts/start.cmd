@echo off
rem Compiles the timer from src\StickyPomodoro.cs on first use, then opens it.
rem Uses the C# compiler included with Windows. Nothing is downloaded.
setlocal
set "VERSION=1.0.0"
set "DATA=%USERPROFILE%\.claude\sticky-pomodoro"
set "EXE=%DATA%\StickyPomodoro-%VERSION%.exe"
set "NET=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%NET%\csc.exe" set "NET=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"

if not exist "%EXE%" (
  if not exist "%NET%\csc.exe" (
    echo Sticky Pomodoro could not find the .NET Framework 4 compiler csc.exe on this PC.
    exit /b 1
  )
  if not exist "%DATA%" mkdir "%DATA%"
  "%NET%\csc.exe" /nologo /target:winexe /optimize+ "/out:%EXE%" "/r:%NET%\WPF\PresentationFramework.dll" "/r:%NET%\WPF\PresentationCore.dll" "/r:%NET%\WPF\WindowsBase.dll" "/r:%NET%\System.Xaml.dll" "%~dp0..\src\StickyPomodoro.cs"
  if errorlevel 1 (
    echo Sticky Pomodoro failed to compile. See the messages above.
    exit /b 1
  )
)

start "" "%EXE%"
echo Sticky Pomodoro is open in the upper right of your screen.
exit /b 0
