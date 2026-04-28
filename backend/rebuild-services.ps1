# Пересборка и перезапуск сервисов для применения изменений в InternalController

Write-Host "Building services..."
docker-compose -f docker-compose.yml build chatservice userservice notificationservice

Write-Host "Restarting services..."
docker-compose -f docker-compose.yml restart chatservice userservice notificationservice

Write-Host "Done! Check logs with: docker logs notificationservice -f"



