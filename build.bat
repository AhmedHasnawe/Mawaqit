@echo off
chcp 65001 >nul
setlocal
echo.
echo   ======================================
echo    بناء مواقيت - Mawaqit
echo   ======================================
echo.

set "MSB="

rem --- ابحث عن MSBuild الخاص بـ Visual Studio ---
set "VSWHERE=%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe"
if not exist "%VSWHERE%" set "VSWHERE=%ProgramFiles%\Microsoft Visual Studio\Installer\vswhere.exe"
if exist "%VSWHERE%" (
  for /f "usebackq tokens=*" %%i in (`"%VSWHERE%" -latest -prerelease -products * -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe 2^>nul`) do set "MSB=%%i"
)

rem --- وإلا استعمل MSBuild المرفق مع .NET Framework ---
if not defined MSB (
  if exist "%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" set "MSB=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
)
if not defined MSB (
  if exist "%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" set "MSB=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
)

if not defined MSB (
  echo   [خطأ] لم يُعثر على MSBuild.
  echo   افتح Mawaqit.sln في Visual Studio واضغط Ctrl+Shift+B.
  echo.
  pause
  exit /b 1
)

echo   MSBuild: %MSB%
echo.
"%MSB%" "%~dp0Mawaqit.sln" /p:Configuration=Release /p:Platform="Any CPU" /v:minimal /nologo
if errorlevel 1 (
  echo.
  echo   [خطأ] فشل البناء. راجع الرسائل أعلاه.
  echo.
  pause
  exit /b 1
)

echo.
echo   تم البناء بنجاح.
echo   الملف: %~dp0Mawaqit\bin\Release\Mawaqit.exe
echo.
choice /c yn /n /m "   هل تريد تشغيله الآن؟ (y/n) "
if errorlevel 2 goto :done
start "" "%~dp0Mawaqit\bin\Release\Mawaqit.exe"

:done
endlocal
