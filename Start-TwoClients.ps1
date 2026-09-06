param([switch]$SkipBuild, [switch]$Smoke)
$ErrorActionPreference = 'Stop'
Push-Location $PSScriptRoot
try {
    if (-not $SkipBuild) {
        dotnet build Kuiz.csproj -c Debug --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'Client build failed.' }
        dotnet build Tools/MultiplayerDebug/MultiplayerDebug.csproj -c Debug --nologo -v quiet
        if ($LASTEXITCODE -ne 0) { throw 'Debug server build failed.' }
    }
    $sessionRoot = Join-Path $env:LOCALAPPDATA ('Kuiz-Debug/' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $sessionRoot | Out-Null
    $serverDll = Join-Path $PSScriptRoot 'Tools/MultiplayerDebug/bin/Debug/net10.0/MultiplayerDebug.dll'
    $clientExe = Join-Path $PSScriptRoot 'bin/Debug/net10.0-windows/Kuiz.exe'
    if (Get-NetTCPConnection -LocalPort 5187 -State Listen -ErrorAction SilentlyContinue) {
        throw 'Port 5187 is already in use. Close the previous debug server before starting another session.'
    }
    $server = Start-Process dotnet -ArgumentList @(('"' + $serverDll + '"'), '--serve') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $sessionRoot 'server.log') -RedirectStandardError (Join-Path $sessionRoot 'server-error.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 40; $attempt++) {
        if ($server.HasExited) { throw "Debug server exited; see $sessionRoot" }
        try {
            $health = Invoke-RestMethod 'http://127.0.0.1:5187/health' -TimeoutSec 1
            if ($health.service -eq 'Kuiz local debug') { $ready = $true; break }
        } catch { }
        Start-Sleep -Milliseconds 250
    }
    if (-not $ready) { throw 'Debug server startup timed out.' }
    $clients = @()
    foreach ($role in @('host','guest')) {
        $clientArgs = @('--debug-role', $role, '--debug-root', ('"' + $sessionRoot + '"'))
        if ($Smoke) { $clientArgs += '--debug-smoke' }
        $clients += Start-Process $clientExe -ArgumentList $clientArgs -PassThru
    }
    Write-Host "Host PID: $($clients[0].Id); Guest PID: $($clients[1].Id); Server PID: $($server.Id)"
    Write-Host "Session data and logs: $sessionRoot"
    Write-Host 'Close both client windows to stop the debug server. In Visual Studio use Debug > Attach to Process and select these Kuiz.exe processes.'
    $clients | Wait-Process
} finally {
    if ($server -and -not $server.HasExited) { $server.Kill() }
    Pop-Location
}
