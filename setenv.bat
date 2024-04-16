@echo off
PATH="%WINDIR%\Microsoft.NET\Framework\v1.0.3705";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v1.1.4322";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v2.0.50727";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v3.0";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v3.5";%PATH%
PATH="%WINDIR%\Microsoft.NET\Framework\v4.0.30319";%PATH%

Set enc=%1
Set tmp=%2
Set dec=%enc:~-2%
echo.# C#･VB.NETコンパイラーの使い方
echo.# 
echo.# ^>csc ???.cs
echo.# でc#コンパイル
echo.# ^>csc /target:winexe ???.cs
echo.# でWindowsAppとしてコンパイル
echo.#
echo.# ^>vbc ???.vb
echo.# でVB.NETコンパイル
echo.# ^>vbc /target:winexe ???.vb
echo.# でWindowsAppとしてコンパイル
echo.#
echo.# 因みにWindowsで基本的に使える言語(拡張子)は
echo.# bat,cs,css,html(hta),js,ps,vb,vbs,vba,(ini,exe)
echo.# です。
echo.# 
if "%dec%"=="cs" (
	echo.# 選択されたファイル:%enc%
	csc %enc%
) else if "%dec%"=="vb" (
	echo.# 選択されたファイル:%enc%
	vbc %enc%
) else (
	cmd
)
if not %errorlevel% == 0 (
	echo. Error!
	PAUSE
) else (
	if "%dec%"=="cs" (
		start %enc:~0,-2%exe
	) else if "%dec%"=="vb" (
		start %enc:~0,-2%exe
	)
)