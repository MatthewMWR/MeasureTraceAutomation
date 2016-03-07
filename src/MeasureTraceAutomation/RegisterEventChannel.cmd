md "%programfiles%\MeasureTraceAutomation"
copy /Y "%~dp0*etwManifest.*" "%programfiles%\MeasureTraceAutomation\"
wevtutil.exe um "%programfiles%\MeasureTraceAutomation\MeasureTraceAutomation.MeasureTraceAutomation.etwManifest.man"
wevtutil.exe im "%programfiles%\MeasureTraceAutomation\MeasureTraceAutomation.MeasureTraceAutomation.etwManifest.man" /rf:"%programfiles%\MeasureTraceAutomation\MeasureTraceAutomation.MeasureTraceAutomation.etwManifest.dll" /mf:"%programfiles%\MeasureTraceAutomation\MeasureTraceAutomation.MeasureTraceAutomation.etwManifest.dll"