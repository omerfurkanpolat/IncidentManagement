$base = "D:\Incident Managment\src"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"

Write-Host "Tüm servisler başlatılıyor..." -ForegroundColor Cyan

Start-Process "cmd" -ArgumentList "/k title DocumentApp && `"$dotnet`" run --project `"$base\IncidentManagement.DocumentApp`"" -WindowStyle Normal
Start-Sleep 2

Start-Process "cmd" -ArgumentList "/k title JobApi && `"$dotnet`" run --project `"$base\IncidentManagement.JobApi`"" -WindowStyle Normal
Start-Sleep 2

Start-Process "cmd" -ArgumentList "/k title McpServer && `"$dotnet`" run --project `"$base\IncidentManagement.McpServer`"" -WindowStyle Normal
Start-Sleep 2

Start-Process "cmd" -ArgumentList "/k title RagPipeline && `"$dotnet`" run --project `"$base\IncidentManagement.RagPipeline`"" -WindowStyle Normal
Start-Sleep 2

Start-Process "cmd" -ArgumentList "/k title Orchestrator && `"$dotnet`" run --project `"$base\IncidentManagement.Orchestrator`"" -WindowStyle Normal

Write-Host ""
Write-Host "Servisler baslatildi!" -ForegroundColor Green
Write-Host "  DocumentApp : http://localhost:5000" -ForegroundColor Yellow
Write-Host "  JobApi      : http://localhost:5001/swagger" -ForegroundColor Yellow
Write-Host "  McpServer   : http://localhost:5002" -ForegroundColor Yellow
