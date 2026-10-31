$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319'
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$references = @('System.dll', 'System.Core.dll', 'System.Runtime.Serialization.dll', 'System.Windows.Forms.dll', 'System.Drawing.dll', 'System.Xaml.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll', 'WPF\PresentationFramework.dll')
$arguments = @('/nologo', '/target:winexe', '/platform:x86', '/optimize+', '/utf8output', ('/out:' + (Join-Path $output 'FoxVideoSCR.scr')))
$arguments += $references | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$arguments += Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item -LiteralPath (Join-Path $root 'scripts\Install.ps1'), (Join-Path $root 'scripts\Uninstall.ps1'), (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') -Destination $output -Force
Write-Output "Built: $output\FoxVideoSCR.scr"
