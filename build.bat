@echo off
echo Compiling FNIRSI DPS-150 Studio Pro (Created by: PATRYK JANKOWSKI)...
"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:FNIRSI_DPS150_Studio.exe /win32icon:app.ico /r:System.dll /r:System.Drawing.dll /r:System.Windows.Forms.dll AssemblyInfo.cs Program.cs Dps150Protocol.cs LiveChartControl.cs MainForm.cs
if %ERRORLEVEL% EQU 0 (
    echo Build succeeded: FNIRSI_DPS150_Studio.exe
) else (
    echo Build failed!
)
pause
