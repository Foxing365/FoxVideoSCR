param([switch]$SystemRemoveOnly)
$ErrorActionPreference = 'Stop'
if ($SystemRemoveOnly) {
    foreach ($folder in @((Join-Path $env:WINDIR 'System32'), (Join-Path $env:WINDIR 'SysWOW64'))) {
        $target = Join-Path $folder 'FoxVideoSCR.scr'
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
    }
    exit 0
}
$current = Get-ItemProperty 'HKCU:\Control Panel\Desktop'
if ($current.'SCRNSAVE.EXE' -eq (Join-Path $env:WINDIR 'System32\FoxVideoSCR.scr')) {
    $backup = Join-Path $env:LOCALAPPDATA 'FoxVideoSCR\previous-screensaver.json'
    $old = if (Test-Path -LiteralPath $backup) { Get-Content -LiteralPath $backup -Raw | ConvertFrom-Json } else { $null }
    if ($old -and $old.Path -and (Test-Path -LiteralPath $old.Path)) {
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -Value $old.Path
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value ([string]$old.Active)
    } else {
        Remove-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value '0'
    }
}
$script = '"' + $PSCommandPath + '"'
$p = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $script, '-SystemRemoveOnly') -Verb RunAs -Wait -PassThru -WindowStyle Hidden
if ($p.ExitCode -ne 0) { throw 'Unable to remove the system files. Close screen saver previews and retry.' }
Write-Output 'Uninstalled. Personal settings in LocalAppData\FoxVideoSCR are retained.'
