param([switch]$SystemCopyOnly)
$ErrorActionPreference = 'Stop'
$source = Join-Path $PSScriptRoot 'FoxVideoSCR.scr'
if (!(Test-Path -LiteralPath $source)) { throw 'FoxVideoSCR.scr must be beside Install.ps1. Build the project or extract the release ZIP first.' }
if ($SystemCopyOnly) {
    foreach ($folder in @((Join-Path $env:WINDIR 'System32'), (Join-Path $env:WINDIR 'SysWOW64'))) {
        if (Test-Path -LiteralPath $folder) {
            $target = Join-Path $folder 'FoxVideoSCR.scr'
            Copy-Item -LiteralPath $source -Destination $target -Force
            if ((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash) { throw "Verification failed: $target" }
        }
    }
    exit 0
}
$installer = '"' + $PSCommandPath + '"'
$elevated = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $installer, '-SystemCopyOnly') -Verb RunAs -Wait -PassThru -WindowStyle Hidden
if ($elevated.ExitCode -ne 0) { throw 'System installation failed; current screen saver was not changed.' }
$data = Join-Path $env:LOCALAPPDATA 'FoxVideoSCR'
New-Item -ItemType Directory -Path $data -Force | Out-Null
$backup = Join-Path $data 'previous-screensaver.json'
if (!(Test-Path -LiteralPath $backup)) {
    $old = Get-ItemProperty 'HKCU:\Control Panel\Desktop'
    @{ Path = $old.'SCRNSAVE.EXE'; Active = $old.ScreenSaveActive } | ConvertTo-Json | Set-Content -LiteralPath $backup -Encoding UTF8
}
Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -Value (Join-Path $env:WINDIR 'System32\FoxVideoSCR.scr')
Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value '1'
Start-Process -FilePath (Join-Path $env:WINDIR 'System32\control.exe') -ArgumentList 'desk.cpl,,@screensaver'
Write-Output 'FoxVideoSCR installed. Choose Settings to configure your videos. The wait time and sign-in setting are preserved.'
