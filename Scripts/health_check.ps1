# 指尖江湖2 服务健康检查脚本
# 用法: .\health_check.ps1

Write-Host "===== 指尖江湖2 服务健康检查 =====" -ForegroundColor Cyan

Write-Host "[1/1] 检查Web服务..." -ForegroundColor Yellow
try {
    $r = Invoke-WebRequest -Uri "http://localhost:9100/" -UseBasicParsing -TimeoutSec 3
    Write-Host "  [OK] Admin面板 -> $($r.StatusCode)" -ForegroundColor Green
} catch {
    Write-Host "  [FAIL] Admin面板 -> 无法连接" -ForegroundColor Red
}
