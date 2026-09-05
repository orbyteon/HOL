[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectPath,

    [string]$UnityPath,

    [string]$OutputRoot,

    [string]$BuildPath,

    [string]$NodePath = 'node',

    [string]$ProvenancePath,

    [switch]$UseGuiBuiltPlayer,

    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Get-FullPath([string]$PathValue) {
    return [System.IO.Path]::GetFullPath($PathValue)
}

function Test-IsWithin([string]$Candidate, [string]$Parent) {
    $candidateFull = (Get-FullPath $Candidate).TrimEnd('\', '/') + '\'
    $parentFull = (Get-FullPath $Parent).TrimEnd('\', '/') + '\'
    return $candidateFull.StartsWith(
        $parentFull,
        [System.StringComparison]::OrdinalIgnoreCase)
}

function Get-StringSha256([string]$Value) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Value)
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return ([System.BitConverter]::ToString(
            $algorithm.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Get-SourceManifest([string]$Root) {
    $excludedDirectories = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in @(
        '.git', '.vs', 'Library', 'Temp', 'Logs', 'obj', 'artifacts',
        'Build', 'Builds', 'UserSettings', 'MemoryCaptures', 'Recordings')) {
        [void]$excludedDirectories.Add($name)
    }
    $excludedExtensions = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($extension in @('.csproj', '.sln', '.user', '.pidb', '.booproj')) {
        [void]$excludedExtensions.Add($extension)
    }

    $records = [System.Collections.Generic.List[object]]::new()
    $files = @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force |
        Sort-Object FullName)
    foreach ($file in $files) {
        $relative = [System.IO.Path]::GetRelativePath($Root, $file.FullName)
        $top = ($relative -split '[\\/]')[0]
        if ($excludedDirectories.Contains($top) -or
            $excludedExtensions.Contains($file.Extension)) {
            continue
        }
        $records.Add([ordered]@{
            path = $relative.Replace('\', '/')
            length = $file.Length
            sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        })
    }
    $json = ConvertTo-Json -InputObject @($records) -Depth 4 -Compress
    return [pscustomobject]@{
        Records = @($records)
        Json = $json
        Sha256 = Get-StringSha256 $json
    }
}

function Get-CheckpointSourceFingerprint([string]$Root) {
    $excludedDirectories = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($name in @(
        '.git', '.vs', 'Library', 'Temp', 'Logs', 'obj', 'artifacts',
        'Build', 'Builds', 'UserSettings', 'MemoryCaptures', 'Recordings')) {
        [void]$excludedDirectories.Add($name)
    }
    $excludedExtensions = [System.Collections.Generic.HashSet[string]]::new(
        [System.StringComparer]::OrdinalIgnoreCase)
    foreach ($extension in @('.csproj', '.sln', '.user', '.pidb', '.booproj')) {
        [void]$excludedExtensions.Add($extension)
    }

    $records = [System.Collections.Generic.List[string]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $Root -File -Recurse -Force)) {
        $relative = [System.IO.Path]::GetRelativePath(
            $Root, $file.FullName).Replace('\', '/')
        $top = ($relative -split '/')[0]
        if ($excludedDirectories.Contains($top) -or
            $excludedExtensions.Contains($file.Extension)) {
            continue
        }
        $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
        $records.Add("$relative|$($file.Length)|$hash")
    }
    $records.Sort([System.StringComparer]::Create(
        [System.Globalization.CultureInfo]::GetCultureInfo('en-CY'),
        $true))
    $payload = [string]::Join([string][char]10, $records)
    return [pscustomobject]@{
        FileCount = $records.Count
        Sha256 = Get-StringSha256 $payload
    }
}

function Get-BuildOutputManifest([string]$ExecutablePath) {
    $directory = Split-Path -Parent $ExecutablePath
    if ([string]::IsNullOrWhiteSpace($directory) -or
        -not (Test-Path -LiteralPath $directory -PathType Container)) {
        throw "Capture build directory is missing: $directory"
    }

    $relativePaths = [System.Collections.Generic.List[string]]::new()
    foreach ($file in @(Get-ChildItem -LiteralPath $directory -File -Recurse -Force)) {
        $relativePaths.Add([System.IO.Path]::GetRelativePath(
            $directory, $file.FullName).Replace('\', '/'))
    }
    $relativePaths.Sort([System.StringComparer]::Ordinal)

    $records = [System.Collections.Generic.List[object]]::new()
    $recordLines = [System.Collections.Generic.List[string]]::new()
    foreach ($relative in $relativePaths) {
        $full = Join-Path $directory $relative.Replace('/', '\')
        $file = Get-Item -LiteralPath $full
        $record = [ordered]@{
            path = $relative
            length = $file.Length
            sha256 = (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
        }
        $records.Add($record)
        $recordLines.Add(
            "$($record.path)|$($record.length)|$($record.sha256)")
    }
    return [pscustomobject]@{
        Records = @($records)
        FileCount = $records.Count
        Sha256 = Get-StringSha256 (
            [string]::Join([string][char]10, $recordLines))
    }
}

function Get-BuildReceiptPath([string]$ExecutablePath) {
    $directory = [System.IO.DirectoryInfo]::new(
        (Split-Path -Parent $ExecutablePath))
    if ($null -eq $directory.Parent) {
        throw 'Capture build directory cannot be a filesystem root.'
    }
    return Join-Path `
        $directory.Parent.FullName `
        ($directory.Name + '.hol-solo-build.json')
}

function Get-PeMachine([string]$ExecutablePath) {
    $stream = [System.IO.File]::OpenRead($ExecutablePath)
    $reader = [System.IO.BinaryReader]::new($stream)
    try {
        if ($reader.ReadUInt16() -ne 0x5a4d) {
            throw "Capture player is not a PE executable: $ExecutablePath"
        }
        $stream.Position = 0x3c
        $peOffset = $reader.ReadInt32()
        if ($peOffset -lt 0x40 -or $peOffset -gt ($stream.Length - 6)) {
            throw "Capture player has an invalid PE header: $ExecutablePath"
        }
        $stream.Position = $peOffset
        if ($reader.ReadUInt32() -ne 0x00004550) {
            throw "Capture player has an invalid PE signature: $ExecutablePath"
        }
        return $reader.ReadUInt16()
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Assert-GuiBuildReceipt(
    [string]$ReceiptPath,
    [string]$ExecutablePath,
    [string]$ProjectRoot,
    [object]$ExpectedSourceFingerprint
) {
    if (-not (Test-Path -LiteralPath $ReceiptPath -PathType Leaf)) {
        throw "GUI capture build receipt is missing: $ReceiptPath"
    }
    try {
        $receipt = Get-Content -Raw -LiteralPath $ReceiptPath |
            ConvertFrom-Json -Depth 20
    }
    catch {
        throw "GUI capture build receipt is invalid JSON: $ReceiptPath"
    }

    if ($receipt.schemaVersion -ne 1 -or
        $receipt.kind -ne 'hol-solo-gui-capture-build') {
        throw 'GUI capture build receipt schema or kind is invalid.'
    }
    if ($receipt.unityVersion -ne '2022.3.62f3') {
        throw "GUI capture build used unexpected Unity version: $($receipt.unityVersion)"
    }
    $projectVersion = Join-Path $ProjectRoot 'ProjectSettings\ProjectVersion.txt'
    $versionLine = @(Get-Content -LiteralPath $projectVersion |
        Where-Object { $_ -like 'm_EditorVersion:*' })
    if ($versionLine.Count -ne 1 -or
        $versionLine[0].Trim() -ne 'm_EditorVersion: 2022.3.62f3') {
        throw 'Solo project does not require exact Unity 2022.3.62f3.'
    }
    if ((Get-FullPath $receipt.projectPath) -ne (Get-FullPath $ProjectRoot)) {
        throw 'GUI capture build receipt names a different Unity project.'
    }
    if ($receipt.scene -ne 'Assets/Scenes/MainMenu.unity' -or
        $receipt.target -ne 'StandaloneWindows64' -or
        -not $receipt.developmentBuild -or
        -not $receipt.includeTestAssemblies -or
        $receipt.unexpectedBuildOptions -ne 0) {
        throw 'GUI capture build target, scene, or options differ from the approved contract.'
    }
    if (-not $receipt.outputDirectoryWasEmpty) {
        throw 'GUI capture build did not attest a new empty output directory.'
    }
    if ($receipt.companyName -ne 'HOL QA' -or
        $receipt.productName -ne 'HOL Solo Capture') {
        throw 'GUI capture player preference isolation contract changed.'
    }
    if ($receipt.buildResult -ne 'Succeeded' -or
        $receipt.totalErrors -ne 0 -or
        $receipt.totalWarnings -ne 0) {
        throw (
            'GUI capture build was not clean: ' +
            "$($receipt.buildResult), errors=$($receipt.totalErrors), " +
            "warnings=$($receipt.totalWarnings).")
    }
    if ($receipt.sourceFileCount -ne $ExpectedSourceFingerprint.FileCount -or
        $receipt.sourceFingerprintSha256 -ne $ExpectedSourceFingerprint.Sha256) {
        throw 'GUI capture build source fingerprint does not match current source.'
    }

    $expectedExecutable = Get-FullPath $ExecutablePath
    if ((Get-FullPath $receipt.executablePath) -ne $expectedExecutable -or
        (Get-FullPath $receipt.outputDirectory) -ne
            (Get-FullPath (Split-Path -Parent $expectedExecutable))) {
        throw 'GUI capture build receipt names an unexpected output path.'
    }
    if (-not (Test-Path -LiteralPath $expectedExecutable -PathType Leaf)) {
        throw "GUI-built capture player is missing: $expectedExecutable"
    }
    if ((Get-PeMachine $expectedExecutable) -ne 0x8664) {
        throw 'GUI-built capture player is not Windows x64 (PE32+ AMD64).'
    }
    $executableInfo = Get-Item -LiteralPath $expectedExecutable
    $executableSha256 = (Get-FileHash -LiteralPath $expectedExecutable -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($receipt.executableLength -ne $executableInfo.Length -or
        $receipt.executableSha256 -ne $executableSha256) {
        throw 'GUI-built capture executable hash or length changed.'
    }

    $culture = [System.Globalization.CultureInfo]::InvariantCulture
    $styles = [System.Globalization.DateTimeStyles]::RoundtripKind
    try {
        $started = [DateTimeOffset]::Parse($receipt.buildStartedUtc, $culture, $styles)
        $completed = [DateTimeOffset]::Parse($receipt.buildCompletedUtc, $culture, $styles)
        $created = [DateTimeOffset]::Parse($receipt.createdUtc, $culture, $styles)
    }
    catch {
        throw 'GUI capture build receipt timestamps are invalid.'
    }
    $now = [DateTimeOffset]::UtcNow
    if ($completed -lt $started -or $created -lt $completed -or
        $created -gt $now.AddMinutes(5) -or
        $created -lt $now.AddHours(-4)) {
        throw 'GUI capture build receipt is stale or has invalid timestamp ordering.'
    }
    if ($executableInfo.LastWriteTimeUtc -lt $started.UtcDateTime.AddSeconds(-5) -or
        $executableInfo.LastWriteTimeUtc -gt $created.UtcDateTime.AddMinutes(5)) {
        throw 'GUI-built capture executable timestamp is outside the fresh build window.'
    }

    $outputManifest = Get-BuildOutputManifest $expectedExecutable
    if ($receipt.outputFileCount -ne $outputManifest.FileCount -or
        $receipt.outputManifestSha256 -ne $outputManifest.Sha256 -or
        @($receipt.outputFiles).Count -ne $outputManifest.FileCount) {
        throw 'GUI capture build output manifest count or hash differs.'
    }
    for ($index = 0; $index -lt $outputManifest.FileCount; $index++) {
        $expected = $outputManifest.Records[$index]
        $actual = $receipt.outputFiles[$index]
        if ($actual.path -cne $expected.path -or
            $actual.length -ne $expected.length -or
            $actual.sha256 -ne $expected.sha256) {
            throw "GUI capture build output record differs at index $index."
        }
    }

    return [pscustomobject]@{
        Receipt = $receipt
        ReceiptSha256 = (Get-FileHash -LiteralPath $ReceiptPath -Algorithm SHA256).Hash.ToLowerInvariant()
        ExecutableSha256 = $executableSha256
        OutputManifest = $outputManifest
    }
}

function Get-ProvenanceManifest([string]$Repository) {
    $statusLines = @(& git -C $Repository -c core.quotepath=false `
        status --porcelain=v1 --untracked-files=all)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to fingerprint the Solo provenance worktree.'
    }

    $records = [System.Collections.Generic.List[object]]::new()
    foreach ($line in $statusLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
            continue
        }
        $status = $line.Substring(0, 2)
        $pathSpec = $line.Substring(3)
        $paths = if ($pathSpec.Contains(' -> ')) {
            @($pathSpec -split ' -> ', 2)
        }
        else {
            @($pathSpec)
        }
        foreach ($relative in $paths) {
            $full = Join-Path $Repository $relative
            $records.Add([ordered]@{
                status = $status
                path = $relative.Replace('\', '/')
                sha256 = if (Test-Path -LiteralPath $full -PathType Leaf) {
                    (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.ToLowerInvariant()
                }
                else {
                    $null
                }
            })
        }
    }
    $json = ConvertTo-Json -InputObject @($records) -Depth 4 -Compress
    return [pscustomobject]@{
        Records = @($records)
        Json = $json
        Sha256 = Get-StringSha256 $json
    }
}

function Quote-ProcessArgument([string]$Value) {
    if ($Value -notmatch '[\s"]') {
        return $Value
    }
    return '"' + $Value.Replace('"', '\"') + '"'
}

function Invoke-Process(
    [string]$FilePath,
    [string[]]$Arguments,
    [string]$Context,
    [int]$TimeoutSeconds = 300
) {
    $argumentLine = ($Arguments | ForEach-Object {
        Quote-ProcessArgument $_
    }) -join ' '
    $process = Start-Process `
        -FilePath $FilePath `
        -ArgumentList $argumentLine `
        -WindowStyle Hidden `
        -PassThru
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
            $process.WaitForExit()
        }
        catch {
            Write-Warning "Unable to terminate timed-out process $($process.Id): $_"
        }
        throw "$Context timed out after $TimeoutSeconds second(s)."
    }
    if ($process.ExitCode -ne 0) {
        throw "$Context failed with exit code $($process.ExitCode)."
    }
}

function Assert-UnityClosed([string]$Context) {
    $editors = @(Get-Process -Name 'Unity' -ErrorAction SilentlyContinue)
    if ($editors.Count -ne 0) {
        throw "${Context}: found $($editors.Count) open Unity Editor process(es)."
    }
}

function Assert-ProvenanceIdentity(
    [string]$Repository,
    [string]$ExpectedBranch,
    [string]$ExpectedHead,
    [string]$ExpectedTree,
    [string]$Context
) {
    $actualBranch = (& git -C $Repository branch --show-current).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "${Context}: unable to read the provenance branch."
    }
    $actualHead = (& git -C $Repository rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "${Context}: unable to read the provenance HEAD."
    }
    $actualTree = (& git -C $Repository rev-parse 'HEAD^{tree}').Trim()
    if ($LASTEXITCODE -ne 0) {
        throw "${Context}: unable to read the provenance tree."
    }
    if ($actualBranch -ne $ExpectedBranch -or
        $actualHead -ne $ExpectedHead -or
        $actualTree -ne $ExpectedTree) {
        throw (
            "${Context}: provenance identity changed. " +
            "Expected $ExpectedBranch / $ExpectedHead / $ExpectedTree; " +
            "found $actualBranch / $actualHead / $actualTree.")
    }
}

$ProjectPath = Get-FullPath $ProjectPath
$hasUnityPath = -not [string]::IsNullOrWhiteSpace($UnityPath)
if ($hasUnityPath) {
    $UnityPath = Get-FullPath $UnityPath
}
$ProvenancePath = if ([string]::IsNullOrWhiteSpace($ProvenancePath)) {
    $ProjectPath
}
else {
    Get-FullPath $ProvenancePath
}
$nodeCommand = Get-Command $NodePath -ErrorAction SilentlyContinue
if (-not (Test-Path -LiteralPath $ProjectPath -PathType Container)) {
    throw "Project path does not exist: $ProjectPath"
}
if ($UseGuiBuiltPlayer) {
    if ($hasUnityPath) {
        throw '-UnityPath must be omitted in GUI-built-player mode.'
    }
    if ([string]::IsNullOrWhiteSpace($BuildPath) -or
        -not [System.IO.Path]::IsPathRooted($BuildPath)) {
        throw '-BuildPath must be an explicit absolute path in GUI-built-player mode.'
    }
}
elseif (-not $hasUnityPath -or
    -not (Test-Path -LiteralPath $UnityPath -PathType Leaf)) {
    throw "Unity executable does not exist: $UnityPath"
}
if ($null -eq $nodeCommand) {
    throw "Node executable is unavailable: $NodePath"
}
if ($SkipBuild) {
    throw '-SkipBuild is prohibited: every evidence run must build a fresh player.'
}
if (-not (Test-Path -LiteralPath $ProvenancePath -PathType Container)) {
    throw "Provenance worktree does not exist: $ProvenancePath"
}
$NodePath = $nodeCommand.Source

if ($UseGuiBuiltPlayer) {
    $BuildPath = Get-FullPath $BuildPath
    if (Test-IsWithin $BuildPath $ProjectPath) {
        throw 'The capture player must be built outside the repository.'
    }
    if (Test-IsWithin $BuildPath $ProvenancePath) {
        throw 'The capture player must be outside the provenance worktree.'
    }
    if (-not (Test-Path -LiteralPath $BuildPath -PathType Leaf)) {
        throw "GUI-built capture player is missing: $BuildPath"
    }
    $receiptPath = Get-BuildReceiptPath $BuildPath
    if (-not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "GUI capture build receipt is missing: $receiptPath"
    }
}

$lockfile = Join-Path $ProjectPath 'Temp\UnityLockfile'
Assert-UnityClosed 'Solo evidence preflight'
if (Test-Path -LiteralPath $lockfile) {
    throw "Unity lockfile already exists: $lockfile"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $stamp = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    $OutputRoot = Join-Path `
        (Split-Path -Parent $ProjectPath) `
        ("HOL_solo_vs_ai_evidence_" + $stamp)
}
$OutputRoot = Get-FullPath $OutputRoot
if (Test-IsWithin $OutputRoot $ProjectPath) {
    throw 'Solo evidence must be written outside the repository.'
}
if (Test-IsWithin $OutputRoot $ProvenancePath) {
    throw 'Solo evidence must be outside the provenance worktree.'
}
if (Test-Path -LiteralPath $OutputRoot) {
    throw "Evidence directory already exists; evidence is never overwritten: $OutputRoot"
}
New-Item -ItemType Directory -Path $OutputRoot | Out-Null

$captureRoot = Join-Path $OutputRoot 'captures'
$logRoot = Join-Path $OutputRoot 'logs'
$buildRoot = Join-Path $OutputRoot 'player'
New-Item -ItemType Directory -Path $captureRoot | Out-Null
New-Item -ItemType Directory -Path $logRoot | Out-Null
if (-not $UseGuiBuiltPlayer) {
    New-Item -ItemType Directory -Path $buildRoot | Out-Null
}

if ([string]::IsNullOrWhiteSpace($BuildPath)) {
    $BuildPath = Join-Path $buildRoot 'HOLSoloCapture.exe'
}
if (-not $UseGuiBuiltPlayer) {
    $BuildPath = Get-FullPath $BuildPath
    if (Test-IsWithin $BuildPath $ProjectPath) {
        throw 'The capture player must be built outside the repository.'
    }
    if (Test-IsWithin $BuildPath $ProvenancePath) {
        throw 'The capture player must be outside the provenance worktree.'
    }
    $receiptPath = Get-BuildReceiptPath $BuildPath
}

$head = (& git -C $ProvenancePath rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to record the Solo worktree HEAD.'
}
$tree = (& git -C $ProvenancePath rev-parse 'HEAD^{tree}').Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to record the Solo worktree tree.'
}
$branch = (& git -C $ProvenancePath branch --show-current).Trim()
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to record the Solo worktree branch.'
}

$sourceManifestBefore = Get-SourceManifest $ProjectPath
$provenanceManifestBefore = Get-ProvenanceManifest $ProvenancePath
$sourceFingerprintBefore = Get-CheckpointSourceFingerprint $ProjectPath
$provenanceFingerprintBefore = Get-CheckpointSourceFingerprint $ProvenancePath
if ($sourceFingerprintBefore.FileCount -ne
        $provenanceFingerprintBefore.FileCount -or
    $sourceFingerprintBefore.Sha256 -ne
        $provenanceFingerprintBefore.Sha256) {
    throw 'Disposable Solo source does not match the provenance worktree.'
}
$sourceManifestPath = Join-Path $OutputRoot 'solo-source-manifest-before.json'
$provenanceManifestPath = Join-Path $OutputRoot 'solo-provenance-manifest-before.json'
$sourceManifestBefore.Records | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath $sourceManifestPath -Encoding utf8
$provenanceManifestBefore.Records | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath $provenanceManifestPath -Encoding utf8

$buildLog = $null
if ($UseGuiBuiltPlayer) {
    if (-not (Test-Path -LiteralPath $BuildPath -PathType Leaf)) {
        throw "GUI-built capture player is missing: $BuildPath"
    }
}
else {
    if (Test-Path -LiteralPath $BuildPath) {
        throw "Capture player already exists; build output is never overwritten: $BuildPath"
    }
    $buildLog = Join-Path $logRoot 'build-windows-player.log'
    $previousBuildOutput = $env:HOL_SOLO_WINDOWS_BUILD
    try {
        $env:HOL_SOLO_WINDOWS_BUILD = $BuildPath
        Invoke-Process `
            -FilePath $UnityPath `
            -Arguments @(
                '-batchmode',
                '-quit',
                '-projectPath', $ProjectPath,
                '-executeMethod', 'SoloDuelLocalCaptureBuild.Build',
                '-logFile', $buildLog
            ) `
            -Context 'Solo capture player build' `
            -TimeoutSeconds 1800
    }
    finally {
        if ($null -eq $previousBuildOutput) {
            Remove-Item Env:HOL_SOLO_WINDOWS_BUILD -ErrorAction SilentlyContinue
        }
        else {
            $env:HOL_SOLO_WINDOWS_BUILD = $previousBuildOutput
        }
    }
}

Assert-UnityClosed 'Post-build verification'
if (Test-Path -LiteralPath $lockfile) {
    throw "Unity lockfile remained after capture build: $lockfile"
}

$sourceAfterBuild = Get-SourceManifest $ProjectPath
$provenanceAfterBuild = Get-ProvenanceManifest $ProvenancePath
$sourceFingerprintAfterBuild = Get-CheckpointSourceFingerprint $ProjectPath
$provenanceFingerprintAfterBuild = Get-CheckpointSourceFingerprint $ProvenancePath
Assert-ProvenanceIdentity `
    -Repository $ProvenancePath `
    -ExpectedBranch $branch `
    -ExpectedHead $head `
    -ExpectedTree $tree `
    -Context 'Post-build verification'
if ($sourceAfterBuild.Sha256 -ne $sourceManifestBefore.Sha256) {
    throw 'Unity changed source inputs while building the Solo capture player.'
}
if ($provenanceAfterBuild.Sha256 -ne $provenanceManifestBefore.Sha256) {
    throw 'The Solo provenance worktree changed while building the capture player.'
}
if ($sourceFingerprintAfterBuild.FileCount -ne
        $sourceFingerprintBefore.FileCount -or
    $sourceFingerprintAfterBuild.Sha256 -ne
        $sourceFingerprintBefore.Sha256 -or
    $provenanceFingerprintAfterBuild.FileCount -ne
        $provenanceFingerprintBefore.FileCount -or
    $provenanceFingerprintAfterBuild.Sha256 -ne
        $provenanceFingerprintBefore.Sha256) {
    throw 'Solo source fingerprint changed while building the capture player.'
}

if (-not (Test-Path -LiteralPath $BuildPath -PathType Leaf)) {
    throw "Capture player is missing: $BuildPath"
}
$guiBuildEvidence = if ($UseGuiBuiltPlayer) {
    Assert-GuiBuildReceipt `
        -ReceiptPath $receiptPath `
        -ExecutablePath $BuildPath `
        -ProjectRoot $ProjectPath `
        -ExpectedSourceFingerprint $sourceFingerprintBefore
}
else {
    $null
}
$playerSha256 = (Get-FileHash -LiteralPath $BuildPath -Algorithm SHA256).Hash.ToLowerInvariant()
$buildOutputManifestBefore = Get-BuildOutputManifest $BuildPath
$receiptSha256 = if ($null -ne $guiBuildEvidence) {
    $guiBuildEvidence.ReceiptSha256
}
else {
    $null
}

$resolutions = @(
    [pscustomobject]@{ Width = 720; Height = 1280; Scale = 2 },
    [pscustomobject]@{ Width = 1080; Height = 1920; Scale = 3 },
    [pscustomobject]@{ Width = 1080; Height = 2400; Scale = 3 },
    [pscustomobject]@{ Width = 1179; Height = 2556; Scale = 3 }
)
$languages = @('en', 'el')
$baseStates = @(
    'preparation',
    'active-input',
    'ai-feedback',
    'history',
    'result',
    'rematch'
)
$primaryExtras = @(
    'difficulty-easy',
    'difficulty-normal',
    'difficulty-hard',
    'difficulty-adaptive',
    'outcome-win',
    'outcome-loss',
    'outcome-draw',
    'outcome-lock'
)

$lanes = [System.Collections.Generic.List[object]]::new()
foreach ($resolution in $resolutions) {
    foreach ($captureLanguage in $languages) {
        foreach ($captureState in $baseStates) {
            $lanes.Add([pscustomobject]@{
                Width = $resolution.Width
                Height = $resolution.Height
                Scale = $resolution.Scale
                Language = $captureLanguage
                State = $captureState
            })
        }
    }
}
foreach ($captureLanguage in $languages) {
    foreach ($captureState in $primaryExtras) {
        $lanes.Add([pscustomobject]@{
            Width = 1080
            Height = 1920
            Scale = 3
            Language = $captureLanguage
            State = $captureState
        })
    }
}
if ($lanes.Count -ne 64) {
    throw "Internal capture matrix error: expected 64 lanes, got $($lanes.Count)."
}

$laneNumber = 0
foreach ($lane in $lanes) {
    $laneNumber++
    $resolutionName = "$($lane.Width)x$($lane.Height)"
    $laneDirectory = Join-Path `
        (Join-Path $captureRoot $resolutionName) `
        $lane.Language
    New-Item -ItemType Directory -Path $laneDirectory -Force | Out-Null

    $stem = "solo-$($lane.State)-$($lane.Language)-$resolutionName"
    $png = Join-Path $laneDirectory ($stem + '.png')
    $layout = Join-Path $laneDirectory ($stem + '.layout.json')
    $log = Join-Path $logRoot ($stem + '.log')
    foreach ($target in @($png, $layout, $log)) {
        if (Test-Path -LiteralPath $target) {
            throw "Evidence target already exists: $target"
        }
    }

    Write-Host (
        "[$laneNumber/$($lanes.Count)] $($lane.State) " +
        "$($lane.Language) $resolutionName")

    Invoke-Process `
        -FilePath $BuildPath `
        -Arguments @(
            '-screen-fullscreen', '0',
            '-screen-width', [string]($lane.Width / $lane.Scale),
            '-screen-height', [string]($lane.Height / $lane.Scale),
            '-popupwindow',
            '-force-d3d11',
            '-logFile', $log,
            '-holSoloCapturePath', $png,
            '-holSoloCaptureLayoutPath', $layout,
            '-holSoloCaptureState', $lane.State,
            '-holSoloCaptureLanguage', $lane.Language,
            '-holSoloCaptureWidth', [string]$lane.Width,
            '-holSoloCaptureHeight', [string]$lane.Height,
            '-holSoloCaptureScale', [string]$lane.Scale
        ) `
        -Context "Solo capture $stem" `
        -TimeoutSeconds 180

    if (-not (Test-Path -LiteralPath $png -PathType Leaf) -or
        (Get-Item -LiteralPath $png).Length -le 1024) {
        throw "Solo capture PNG is missing or empty: $png"
    }
    if (-not (Test-Path -LiteralPath $layout -PathType Leaf) -or
        (Get-Item -LiteralPath $layout).Length -le 64) {
        throw "Solo capture layout sidecar is missing or empty: $layout"
    }
}

$auditTool = Join-Path $ProjectPath 'tools\solo\audit-solo-duel-captures.mjs'
$inventory = Join-Path $OutputRoot 'solo-capture-sha256.json'
Invoke-Process `
    -FilePath $NodePath `
    -Arguments @($auditTool, $captureRoot, '--inventory', $inventory) `
    -Context 'Solo capture evidence audit' `
    -TimeoutSeconds 300

$sourceManifestAfter = Get-SourceManifest $ProjectPath
$provenanceManifestAfter = Get-ProvenanceManifest $ProvenancePath
$sourceFingerprintAfter = Get-CheckpointSourceFingerprint $ProjectPath
$provenanceFingerprintAfter = Get-CheckpointSourceFingerprint $ProvenancePath
Assert-ProvenanceIdentity `
    -Repository $ProvenancePath `
    -ExpectedBranch $branch `
    -ExpectedHead $head `
    -ExpectedTree $tree `
    -Context 'Final evidence verification'
$sourceManifestAfterPath = Join-Path $OutputRoot 'solo-source-manifest-after.json'
$provenanceManifestAfterPath = Join-Path $OutputRoot 'solo-provenance-manifest-after.json'
$sourceManifestAfter.Records | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath $sourceManifestAfterPath -Encoding utf8
$provenanceManifestAfter.Records | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath $provenanceManifestAfterPath -Encoding utf8
if ($sourceManifestAfter.Sha256 -ne $sourceManifestBefore.Sha256) {
    throw 'Solo capture source inputs changed during the evidence matrix.'
}
if ($provenanceManifestAfter.Sha256 -ne $provenanceManifestBefore.Sha256) {
    throw 'The Solo provenance worktree changed during the evidence matrix.'
}
if ($sourceFingerprintAfter.FileCount -ne
        $sourceFingerprintBefore.FileCount -or
    $sourceFingerprintAfter.Sha256 -ne
        $sourceFingerprintBefore.Sha256 -or
    $provenanceFingerprintAfter.FileCount -ne
        $provenanceFingerprintBefore.FileCount -or
    $provenanceFingerprintAfter.Sha256 -ne
        $provenanceFingerprintBefore.Sha256) {
    throw 'Solo source fingerprint changed during the evidence matrix.'
}
$playerSha256After = (Get-FileHash -LiteralPath $BuildPath -Algorithm SHA256).Hash.ToLowerInvariant()
if ($playerSha256After -ne $playerSha256) {
    throw 'The Solo capture player changed during the evidence matrix.'
}
$buildOutputManifestAfter = Get-BuildOutputManifest $BuildPath
if ($buildOutputManifestAfter.FileCount -ne
        $buildOutputManifestBefore.FileCount -or
    $buildOutputManifestAfter.Sha256 -ne
        $buildOutputManifestBefore.Sha256) {
    throw 'The Solo capture player output bundle changed during the evidence matrix.'
}
if ($UseGuiBuiltPlayer) {
    $receiptSha256After = (Get-FileHash -LiteralPath $receiptPath -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($receiptSha256After -ne $receiptSha256) {
        throw 'The GUI capture build receipt changed during the evidence matrix.'
    }
}

$runRecord = [ordered]@{
    schemaVersion = 1
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    projectPath = $ProjectPath
    provenancePath = $ProvenancePath
    branch = $branch
    head = $head
    tree = $tree
    sourceManifestBefore = $sourceManifestPath
    sourceManifestAfter = $sourceManifestAfterPath
    sourceManifestSha256 = $sourceManifestBefore.Sha256
    provenanceManifestBefore = $provenanceManifestPath
    provenanceManifestAfter = $provenanceManifestAfterPath
    provenanceManifestSha256 = $provenanceManifestBefore.Sha256
    sourceFileCount = $sourceFingerprintBefore.FileCount
    sourceFingerprintSha256 = $sourceFingerprintBefore.Sha256
    buildMode = if ($UseGuiBuiltPlayer) {
        'gui-built-player'
    }
    else {
        'unity-command-line'
    }
    unityPath = $UnityPath
    playerPath = $BuildPath
    playerSha256 = $playerSha256
    buildReceiptPath = if ($UseGuiBuiltPlayer) {
        $receiptPath
    }
    else {
        $null
    }
    buildReceiptSha256 = $receiptSha256
    buildOutputFileCount = $buildOutputManifestBefore.FileCount
    buildOutputManifestSha256 = $buildOutputManifestBefore.Sha256
    unityVersion = if ($null -ne $guiBuildEvidence) {
        $guiBuildEvidence.Receipt.unityVersion
    }
    else {
        $null
    }
    laneCount = $lanes.Count
    captureRoot = $captureRoot
    sha256Inventory = $inventory
}
$runRecord | ConvertTo-Json -Depth 5 | Set-Content `
    -LiteralPath (Join-Path $OutputRoot 'solo-capture-run.json') `
    -Encoding utf8

Assert-UnityClosed 'Solo evidence final verification'
if (Test-Path -LiteralPath $lockfile) {
    throw "Unity lockfile remained after Solo capture matrix: $lockfile"
}

Write-Host "Solo capture matrix complete: $OutputRoot"
