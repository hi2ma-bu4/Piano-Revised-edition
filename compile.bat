@echo off
PATH="%WINDIR%\Microsoft.NET\Framework\v1.0.3705";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v1.1.4322";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v2.0.50727";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v3.0";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v3.5";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v4.0.30319";%PATH%
if not "%1" == "" (
csc %1
) else (
echo. You can easily compile with "csc" command.
echo.
cmd
)
if not %errorlevel% == 0 (
echo. Error!
PAUSE
)
