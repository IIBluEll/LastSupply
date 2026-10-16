# PowerShell 7: 실제 Core와 검사 소스를 메모리에서 컴파일합니다. Unity 실행 검사는 아닙니다.
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$coreFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets\01_Main\02_Scripts\Core') -Recurse -Filter *.cs -File | Select-Object -ExpandProperty FullName)
$checkFiles = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter *.cs -File | Select-Object -ExpandProperty FullName)
Add-Type -Path ($coreFiles + $checkFiles) -CompilerOptions '/langversion:9.0'
Write-Output ('Core compilation passed: ' + $coreFiles.Count + ' files')
[LastSupply.Core.SessionEntryChecks]::Run()
[LastSupply.Core.EventActionChecks]::Run()
[LastSupply.Core.CoreSessionChecks]::Run()
