$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x C# compiler not found.' }
$outDir = Join-Path $PSScriptRoot 'dist'
New-Item -ItemType Directory -Path $outDir -Force | Out-Null
$source = Join-Path $PSScriptRoot 'src\Installer.cs'
$patch = Join-Path $PSScriptRoot 'patches\PPSA01341.json'
$exe = Join-Path $outDir 'KeepSoulsInstaller.exe'
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll "/resource:$patch,patch.json" "/out:$exe" $source
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed.' }
$test = Start-Process -FilePath $exe -ArgumentList '--self-test' -WindowStyle Hidden -Wait -PassThru
if ($test.ExitCode -ne 0) { throw 'Installer self-test failed; see the temporary KeepSoulsInstaller-test-error.txt file.' }
Write-Output "Built and self-tested $exe"
