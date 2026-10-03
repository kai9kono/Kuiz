param([string]$CompilerPath)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$versionSource = Get-Content (Join-Path $repoRoot 'AppVersion.cs') -Raw
if ($versionSource -notmatch 'Version = "([0-9]+\.[0-9]+\.[0-9]+)"') { throw 'Version not found.' }
$releaseVersion = $Matches[1]
[xml]$project = Get-Content (Join-Path $repoRoot 'Kuiz.csproj')
if ($project.Project.PropertyGroup.Version -ne $releaseVersion) { throw 'Project and app versions differ.' }
if (-not $CompilerPath) {
    $candidates = @("$env:LOCALAPPDATA/Programs/Inno Setup 6/ISCC.exe", "${env:ProgramFiles(x86)}/Inno Setup 6/ISCC.exe", "$env:ProgramFiles/Inno Setup 6/ISCC.exe")
    $CompilerPath = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if (-not $CompilerPath) { throw 'Inno Setup compiler is required.' }
$publishDirectory = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts/publish/win-x64'))
$artifactRoot = [IO.Path]::GetFullPath((Join-Path $repoRoot 'artifacts')) + [IO.Path]::DirectorySeparatorChar
if (-not $publishDirectory.StartsWith($artifactRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid output path.' }
if (Test-Path -LiteralPath $publishDirectory) { Remove-Item -LiteralPath $publishDirectory -Recurse -Force }
& dotnet publish (Join-Path $repoRoot 'Kuiz.csproj') -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -p:DebugType=None -p:DebugSymbols=false -o $publishDirectory --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw 'Release publish failed.' }
if (-not (Test-Path (Join-Path $publishDirectory 'hostfxr.dll'))) { throw 'Self-contained runtime is missing.' }
& $CompilerPath "/DMyAppVersion=$releaseVersion" "/DPublishDir=$publishDirectory" (Join-Path $PSScriptRoot 'KuizSetup.iss')
if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
$installerPath = Join-Path $PSScriptRoot "KuizSetup-$releaseVersion.exe"
$hash = (Get-FileHash -LiteralPath $installerPath -Algorithm SHA256).Hash.ToLowerInvariant()
"$hash  KuizSetup-$releaseVersion.exe" | Set-Content (Join-Path $PSScriptRoot "KuizSetup-$releaseVersion.sha256") -Encoding ascii
Write-Output "Installer: $installerPath"
