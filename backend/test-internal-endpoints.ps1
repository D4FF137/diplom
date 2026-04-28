# Тестирование внутренних эндпоинтов

Write-Host "=== Testing internal endpoints ===" -ForegroundColor Green

Write-Host "`n1. Testing ChatService internal endpoint..." -ForegroundColor Yellow
$chatResponse = docker exec chatservice curl -s http://localhost:5004/api/internal/chats/1
Write-Host "Response: $chatResponse"

Write-Host "`n2. Testing UserService internal endpoint..." -ForegroundColor Yellow
$userResponse = docker exec userservice curl -s "http://localhost:5001/api/internal/users?companyId=1"
Write-Host "Response: $userResponse"

Write-Host "`n=== Done ===" -ForegroundColor Green



