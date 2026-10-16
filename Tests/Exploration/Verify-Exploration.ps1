# PowerShell 7 / Windows. 원본 Scene·패키지·프로젝트 설정을 변경하지 않습니다.
param(
    [string]$UnityPath = 'C:\Program Files\Unity\Hub\Editor\6000.3.20f1\Editor\Unity.exe'
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$verificationRoot = [IO.Path]::GetFullPath((Join-Path $projectRoot ('Temp/ExplorationVerification-' + [Guid]::NewGuid().ToString('N').Substring(0, 8))))
if (-not $verificationRoot.StartsWith($projectRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw '검증 프로젝트는 원본 작업 폴더의 Temp 안에 있어야 합니다.'
}
if (-not (Test-Path -LiteralPath $UnityPath)) { throw ('Unity 실행 파일을 찾을 수 없습니다: ' + $UnityPath) }
if (Test-Path -LiteralPath $verificationRoot) { throw '검증 경로가 이미 존재합니다.' }

New-Item -ItemType Directory -Path (Join-Path $verificationRoot 'Assets/01_Main'), (Join-Path $verificationRoot 'Assets/HM'),
    (Join-Path $verificationRoot 'Assets/Editor'), (Join-Path $verificationRoot 'Packages'), (Join-Path $verificationRoot 'ProjectSettings') | Out-Null
foreach ($relativePath in @('Assets/01_Main/02_Scripts', 'Assets/01_Main/04_Resource', 'Assets/TextMesh Pro')) {
    $sourcePath = Join-Path $projectRoot $relativePath
    $targetPath = Join-Path $verificationRoot $relativePath
    Copy-Item -LiteralPath $sourcePath -Destination $targetPath -Recurse
    if (Test-Path -LiteralPath ($sourcePath + '.meta')) { Copy-Item -LiteralPath ($sourcePath + '.meta') -Destination ($targetPath + '.meta') }
}
$hmPackage = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.hm.codebase@*')
if ($hmPackage.Count -ne 1) { throw '사용할 HM CodeBase 캐시를 하나로 특정할 수 없습니다.' }
foreach ($relativePath in @('MVP/AView.cs', 'MVP/APresenter.cs', 'Interface/IOpen.cs', 'Interface/IDispose.cs')) {
    Copy-Item -LiteralPath (Join-Path $hmPackage[0].FullName ('Runtime/' + $relativePath)) -Destination (Join-Path $verificationRoot 'Assets/HM')
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ExplorationPlayModeChecks.cs') -Destination (Join-Path $verificationRoot 'Assets/Editor')
Copy-Item -LiteralPath (Join-Path $projectRoot 'ProjectSettings/ProjectVersion.txt') -Destination (Join-Path $verificationRoot 'ProjectSettings/ProjectVersion.txt')

$uguiPackage = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Library/PackageCache') -Directory -Filter 'com.unity.ugui@*')
if ($uguiPackage.Count -ne 1) { throw '사용할 uGUI 캐시를 하나로 특정할 수 없습니다.' }
$manifest = @{ dependencies = @{
    'com.unity.ugui' = 'file:' + $uguiPackage[0].FullName.Replace('\', '/')
    'com.unity.modules.ui' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
} } | ConvertTo-Json -Depth 4
[IO.File]::WriteAllText((Join-Path $verificationRoot 'Packages/manifest.json'), $manifest, [Text.UTF8Encoding]::new($false))

$logPath = Join-Path $verificationRoot 'ExplorationPlayMode.log'
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $verificationRoot + '"'),
    '-executeMethod', 'LastSupply.Tests.ExplorationPlayModeChecks.RunBatch', '-logFile', ('"' + $logPath + '"'))
Write-Output ('검증 프로젝트: ' + $verificationRoot)
$unityProcess = Start-Process -FilePath $UnityPath -ArgumentList $arguments -WindowStyle Hidden -PassThru
Write-Output ('Unity 검사 PID: ' + $unityProcess.Id)
if (-not $unityProcess.WaitForExit(240000)) {
    $unityProcess.Kill()
    throw ('검증용 Unity 프로세스의 제한 시간을 초과했습니다. 로그: ' + $logPath)
}
$exitCode = $unityProcess.ExitCode
$passed = Select-String -LiteralPath $logPath -Pattern 'PLAYMODE_UI_CHECKS_PASSED:'
if ($exitCode -ne 0 -or -not $passed) {
    Get-Content -LiteralPath $logPath -Tail 100
    throw ("Unity UI 검증 실패. 종료 코드: $exitCode / 로그: $logPath")
}
$passed.Line | Write-Output
Write-Output ('Unity 종료 코드: ' + $exitCode)
Write-Output ('검증 로그: ' + $logPath)
