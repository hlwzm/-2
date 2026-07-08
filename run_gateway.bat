@echo off
chcp 65001 > nul
cd /d "%~dp0Server"
echo Starting Gateway Server...
dotnet run --project Jx3.Gateway --configuration Release