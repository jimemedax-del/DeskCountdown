$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$distDir = Join-Path $scriptDir "dist"
if (!(Test-Path $distDir)) {
    New-Item -ItemType Directory -Path $distDir | Out-Null
}

$outExe = Join-Path $distDir "DeskCountdown.exe"
$fwDir = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319"
$wpfDir = Join-Path $fwDir "WPF"
$csc = Join-Path $fwDir "csc.exe"

$sources = @(
    (Join-Path $scriptDir "Models.cs"),
    (Join-Path $scriptDir "StorageService.cs"),
    (Join-Path $scriptDir "Win32Helper.cs"),
    (Join-Path $scriptDir "ThemePresets.cs"),
    (Join-Path $scriptDir "FloatingWidgetWindow.cs"),
    (Join-Path $scriptDir "ReminderPopupWindow.cs"),
    (Join-Path $scriptDir "EditItemDialog.cs"),
    (Join-Path $scriptDir "MainWindow.cs"),
    (Join-Path $scriptDir "Program.cs")
)

$refs = @(
    "/r:$wpfDir\PresentationFramework.dll",
    "/r:$wpfDir\PresentationCore.dll",
    "/r:$wpfDir\WindowsBase.dll",
    "/r:$fwDir\System.Xaml.dll",
    "/r:$fwDir\System.Windows.Forms.dll",
    "/r:$fwDir\System.Drawing.dll",
    "/r:$fwDir\System.Web.Extensions.dll",
    "/r:$fwDir\System.Core.dll"
)

Write-Host "Compiling DeskCountdown.exe..."
& $csc /nologo /codepage:65001 /utf8output /target:winexe /optimize+ /platform:anycpu /out:$outExe @refs @sources
if ($LASTEXITCODE -ne 0) {
    throw "Compilation failed with exit code $LASTEXITCODE"
}

Write-Host "Build succeeded: $outExe"
