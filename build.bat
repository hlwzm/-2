@echo off
chcp 65001 > nul
echo ===== 指尖江湖2 构建脚本 =====
echo.

echo [1/2] Building Server...
cd /d "%~dp0Server"
dotnet build Jx3.sln -c Release
if %errorlevel% neq 0 (
    echo Server build FAILED!
    exit /b 1
)
echo Server build OK
echo.

echo [2/2] Build completed!
echo.
echo To run:  cd Server ^&^& dotnet run --project Jx3.Gateway