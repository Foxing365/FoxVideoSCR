$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (!(Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
    throw "64-bit .NET Framework compiler not found at: $framework"
}
$output = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $output -Force | Out-Null
$references = @(
    'System.dll',
    'System.Core.dll',
    'System.Runtime.Serialization.dll',
    'System.Windows.Forms.dll',
    'System.Drawing.dll',
    'System.Xaml.dll',
    'WPF\WindowsBase.dll',
    'WPF\PresentationCore.dll',
    'WPF\PresentationFramework.dll'
)
$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:x64',
    '/optimize+',
    '/utf8output',
    ('/win32manifest:' + (Join-Path $root 'src\app.manifest')),
    ('/out:' + (Join-Path $output 'FoxVideoSCR.scr'))
)
$arguments += $references | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
$arguments += Get-ChildItem -LiteralPath (Join-Path $root 'src') -Filter '*.cs' | ForEach-Object FullName
& (Join-Path $framework 'csc.exe') @arguments
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed' }
Copy-Item -LiteralPath (Join-Path $root 'scripts\Install.ps1'), (Join-Path $root 'scripts\Uninstall.ps1'), (Join-Path $root 'scripts\Install.cmd'), (Join-Path $root 'scripts\Uninstall.cmd'), (Join-Path $root 'README.md'), (Join-Path $root 'LICENSE') -Destination $output -Force
Write-Output "Built (x64): $output\FoxVideoSCR.scr"
