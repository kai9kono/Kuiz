param([Parameter(Mandatory=$true)][string]$Version)
$ErrorActionPreference='Stop'
$repoRoot=Split-Path -Parent $PSScriptRoot
$installer=Join-Path $PSScriptRoot "KuizSetup-$Version.exe"
if (-not (Test-Path -LiteralPath $installer)) { throw 'Build the installer first.' }
$releaseRaw=& gh release view "v$Version" --repo kai9kono/Kuiz --json assets,isDraft,isPrerelease,publishedAt
if ($LASTEXITCODE -ne 0) { throw 'Published GitHub release not found.' }
$release=$releaseRaw | ConvertFrom-Json
if ($release.isDraft -or $release.isPrerelease) { throw 'Release must be published and stable.' }
$asset=@($release.assets | Where-Object name -eq "KuizSetup-$Version.exe")
$checksum=@($release.assets | Where-Object name -eq "KuizSetup-$Version.sha256")
if ($asset.Count -ne 1 -or $checksum.Count -ne 1) { throw 'Installer or checksum asset missing.' }
if ($asset[0].size -ne (Get-Item -LiteralPath $installer).Length) { throw 'Uploaded installer size differs.' }
$hash=(Get-FileHash -LiteralPath $installer -Algorithm SHA256).Hash.ToLowerInvariant()
if ($asset[0].digest -and $asset[0].digest -ne "sha256:$hash") { throw 'Uploaded installer hash differs.' }
$manifest=[ordered]@{ready=$true;version=$Version;date=([DateTimeOffset]::Parse($release.publishedAt).ToOffset([TimeSpan]::FromHours(9)).ToString('yyyy-MM-dd'));sizeMB=[Math]::Round((Get-Item -LiteralPath $installer).Length/1MB,1);url=$asset[0].url;checksumUrl=$checksum[0].url;sha256=$hash}
$manifest | ConvertTo-Json | Set-Content (Join-Path $repoRoot 'DownloadSite/dist/release.json') -Encoding utf8
Write-Output 'Download site now points to the verified stable release.'
