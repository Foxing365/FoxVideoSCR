param([switch]$SystemRemoveOnly)
$ErrorActionPreference = 'Stop'
if ($SystemRemoveOnly) {
    foreach ($folder in @((Join-Path $env:WINDIR 'System32'), (Join-Path $env:WINDIR 'SysWOW64'))) {
        $target = Join-Path $folder 'FoxVideoSCR.scr'
        if (Test-Path -LiteralPath $target) { Remove-Item -LiteralPath $target -Force }
    }
    exit 0
}
function Test-IsFoxScreensaver([string]$Path, [string]$FoxPath) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    if ($Path -ieq $FoxPath) { return $true }
    $leaf = Split-Path -Path $Path -Leaf
    if ($leaf -ieq 'FoxVideoSCR.scr' -or $leaf -ieq 'FOXVID~1.SCR') { return $true }
    if (Test-Path -LiteralPath $Path) {
        return ((Get-Item -LiteralPath $Path).FullName -ieq $FoxPath)
    }
    return $false
}
$script = '"' + $PSCommandPath + '"'
$p = Start-Process -FilePath (Join-Path $env:WINDIR 'System32\WindowsPowerShell\v1.0\powershell.exe') -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', $script, '-SystemRemoveOnly') -Verb RunAs -Wait -PassThru -WindowStyle Hidden
if ($p.ExitCode -ne 0) { throw 'Unable to remove the system files. Close screen saver previews and retry.' }
$foxPath = Join-Path $env:WINDIR 'System32\FoxVideoSCR.scr'
$backup = Join-Path $env:LOCALAPPDATA 'FoxVideoSCR\previous-screensaver.json'
$current = Get-ItemProperty 'HKCU:\Control Panel\Desktop'
if (Test-IsFoxScreensaver $current.'SCRNSAVE.EXE' $foxPath) {
    $old = if (Test-Path -LiteralPath $backup) { Get-Content -LiteralPath $backup -Raw | ConvertFrom-Json } else { $null }
    if ($old -and $old.Path -and (Test-Path -LiteralPath $old.Path)) {
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -Value $old.Path
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value ([string]$old.Active)
    } else {
        Remove-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'SCRNSAVE.EXE' -ErrorAction SilentlyContinue
        Set-ItemProperty 'HKCU:\Control Panel\Desktop' -Name 'ScreenSaveActive' -Value '0'
    }
}
if (Test-Path -LiteralPath $backup) { Remove-Item -LiteralPath $backup -Force }
Write-Output 'Uninstalled. Personal settings in LocalAppData\FoxVideoSCR are retained.'
