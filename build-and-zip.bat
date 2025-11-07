@echo off
setlocal enabledelayedexpansion

REM ===== CONFIG =====
set "ROOT_DIR=%CD%"
set "PROJECT_PATH=QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain.csproj"
set "PLUGIN_NAME=QuickBrain"
set "PUBLISH_DIR=QuickBrain\Publish"

REM ===== CLEAN UP =====
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\bin" rmdir /s /q "QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\bin"
if exist "QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\obj" rmdir /s /q "QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\obj"
powershell -NoProfile -Command "Get-ChildItem -Path '%ROOT_DIR%' -Filter '%PLUGIN_NAME%-*.zip' | Remove-Item -Force -ErrorAction SilentlyContinue"

REM ===== GET VERSION =====
for /f "usebackq tokens=* delims=" %%i in (`powershell -NoProfile -Command "(Get-Content 'QuickBrain/Community.PowerToys.Run.Plugin.QuickBrain/plugin.json' | ConvertFrom-Json).Version"`) do set "VERSION=%%i"
echo Plugin: %PLUGIN_NAME%
echo Version: %VERSION%

REM ===== DEPENDENCIES TO EXCLUDE =====
set "DEPENDENCIES_TO_EXCLUDE=PowerToys.Common.UI.* PowerToys.ManagedCommon.* PowerToys.Settings.UI.Lib.* Wox.Infrastructure.* Wox.Plugin.*"

REM ===== BUILD X64 =====
echo === Building for x64 ===
dotnet publish "%PROJECT_PATH%" -c Release -r win-x64 --self-contained false
if errorlevel 1 goto :error

REM ===== BUILD ARM64 =====
echo === Building for ARM64 ===
dotnet publish "%PROJECT_PATH%" -c Release -r win-arm64 --self-contained false
if errorlevel 1 goto :error

REM ===== PACKAGE BUILDS =====
call :package win-x64
if errorlevel 1 goto :error
call :package win-arm64
if errorlevel 1 goto :error

goto :checksums

:package
set "ARCH=%~1"
set "CLEAN_ARCH=%ARCH:win-=%"
echo === Packaging %CLEAN_ARCH% ===
set "PUBLISH_PATH=%ROOT_DIR%\QuickBrain\Community.PowerToys.Run.Plugin.QuickBrain\bin\Release\net9.0-windows10.0.22621.0\%ARCH%\publish"
set "DEST=%ROOT_DIR%\QuickBrain\Publish\%CLEAN_ARCH%"
set "ZIP_PATH=%ROOT_DIR%\%PLUGIN_NAME%-%VERSION%-%CLEAN_ARCH%.zip"

if exist "%DEST%" rmdir /s /q "%DEST%"
mkdir "%DEST%"

xcopy "%PUBLISH_PATH%\*" "%DEST%\" /E /I /Q /Y >nul
if errorlevel 1 (
    echo Failed to copy publish output for %ARCH%.
    exit /b 1
)

for %%P in (%DEPENDENCIES_TO_EXCLUDE%) do (
    powershell -NoProfile -Command "Get-ChildItem -Path '%DEST%' -Filter '%%~P' -Recurse | Remove-Item -Force -ErrorAction SilentlyContinue" >nul
)

if exist "%ZIP_PATH%" del /q "%ZIP_PATH%"
powershell -NoProfile -Command "Compress-Archive -Path '%DEST%\*' -DestinationPath '%ZIP_PATH%' -Force" >nul
if errorlevel 1 (
    echo Failed to create archive for %ARCH%.
    exit /b 1
)

echo Created: %PLUGIN_NAME%-%VERSION%-%CLEAN_ARCH%.zip
exit /b 0

:checksums
echo === Generating checksums ===
powershell -NoProfile -Command "Get-ChildItem -Path '%ROOT_DIR%' -Filter '%PLUGIN_NAME%-%VERSION%-*.zip' | ForEach-Object { $hash = Get-FileHash -Path $_.FullName -Algorithm SHA256; Write-Output ('{0}: {1}' -f $_.Name, $hash.Hash) }"

echo === Completed packages ===
dir /b "%PLUGIN_NAME%-%VERSION%-*.zip"
exit /b 0

:error
echo Build or packaging failed.
exit /b 1
