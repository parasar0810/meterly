$connectionString = "Host=localhost;Port=5433;Database=meterly_db;Username=meterly_user;Password=meterly_password;"
try {
    Add-Type -Path "C:\Program Files\dotnet\shared\Microsoft.AspNetCore.App\8.0.11\System.Data.Common.dll" -ErrorAction SilentlyContinue
    # Actually wait, writing a raw TCP client is easier
    $tcp = [System.Net.Sockets.TcpClient]::new()
    $tcp.Connect("127.0.0.1", 5433)
    Write-Host "Connected to 127.0.0.1:5433 successfully!" -ForegroundColor Green
    $tcp.Close()
} catch {
    Write-Host "Failed to connect: $_" -ForegroundColor Red
}
