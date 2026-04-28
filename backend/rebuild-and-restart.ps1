# Пересборка и перезапуск сервисов

Write-Host "=== Building services ===" -ForegroundColor Green
docker-compose build chatservice userservice notificationservice

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n=== Restarting services ===" -ForegroundColor Green
    docker-compose restart chatservice userservice notificationservice
    
    Write-Host "`n=== Done! ===" -ForegroundColor Green
    Write-Host "Check logs with: docker logs notificationservice -f" -ForegroundColor Yellow
} else {
    Write-Host "`n=== Build failed! ===" -ForegroundColor Red
    exit 1
}



