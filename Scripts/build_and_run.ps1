# 指尖江湖2 一键构建+运行
param([switch]$SkipBuild, [switch]$SkipAdmin)
$root = Split-Path -Parent $PSScriptRoot
$serverDir = Join-Path $root "Server"
if (-not $SkipBuild) {
    Write-Host "===== 1. 编译所有项目 =====" -ForegroundColor Cyan
    Set-Location $serverDir
    dotnet build Jx3.sln -c Release
    if ($LASTEXITCODE -ne 0) { Write-Host "编译失败!" -ForegroundColor Red; exit 1 }
    Write-Host "编译成功!" -ForegroundColor Green
}
if (-not $SkipAdmin) {
    Write-Host "===== 2. 启动Admin面板 =====" -ForegroundColor Cyan
    $adminProc = Get-Process -Name "Jx3.Admin" -ErrorAction SilentlyContinue
    if (-not $adminProc) {
        $adminDir = Join-Path $serverDir "Jx3.Admin"
        Start-Process -WindowStyle Hidden -FilePath "dotnet" -ArgumentList "run --project $adminDir -c Release --urls http://0.0.0.0:9100"
        Start-Sleep -Seconds 3
    }
    try { $r = Invoke-WebRequest -Uri "http://localhost:9100/" -UseBasicParsing -TimeoutSec 3; Write-Host "Admin就绪: $($r.StatusCode)" -ForegroundColor Green }
    catch { Write-Host "Admin启动失败" -ForegroundColor Red }
}
Write-Host "===== 运行测试 =====" -ForegroundColor Cyan
Set-Location $serverDir
dotnet test Jx3.Tests\Jx3.Tests.csproj -c Release --no-build -v n 2>&1 | Select-String "通过|失败|测试总数"
Write-Host "===== 完成 =====" -ForegroundColor Cyan
Write-Host "Admin面板: http://localhost:9100/" -ForegroundColor Green
