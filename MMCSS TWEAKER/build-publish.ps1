param(
    [ValidateSet("both", "with-net", "without-net")]
    [string]$Flavor = "both",
    [ValidateSet("Release", "Debug")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "MMCSSTweaker.csproj"
$publishRoot = Join-Path $PSScriptRoot "bin\Publish"
$workRoot = Join-Path $publishRoot "_work"
$standaloneExeName = "MMCSS-TWEAKER-NET-RUNTIME.exe"
$frameworkDependentExeName = "MMCSS-TWEAKER.exe"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI is not available in PATH."
}

$releaseOut = $publishRoot
$withNetOut = Join-Path $workRoot "standalone"
$withoutNetOut = Join-Path $workRoot "framework-dependent"

function Get-PublishedExe {
    param(
        [string]$OutputDir,
        [string]$SourceName = "MMCSS TWEAKER.exe"
    )

    $allExe = Get-ChildItem -LiteralPath $OutputDir -File -Filter *.exe

    $publishedExe = $allExe | Where-Object { $_.Name -ieq $SourceName } | Select-Object -First 1
    if (-not $publishedExe) {
        $publishedExe = $allExe |
            Sort-Object Length -Descending |
            Select-Object -First 1
    }

    if (-not $publishedExe) {
        throw "No EXE found in publish output: $OutputDir"
    }

    return $publishedExe
}

function Invoke-PublishFlavor {
    param(
        [string]$Label,
        [bool]$SelfContained,
        [bool]$PublishSingleFile,
        [bool]$IncludeNativeLibrariesForSelfExtract,
        [string]$OutputDir,
        [string]$TargetExeName
    )

    if ($Clean -and (Test-Path -LiteralPath $OutputDir)) {
        Remove-Item -LiteralPath $OutputDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    Write-Host ""
    Write-Host "=== Publishing: $Label ==="
    Write-Host "Temporary output: $OutputDir"

    $selfContainedText = $SelfContained.ToString().ToLowerInvariant()
    $singleFileText = $PublishSingleFile.ToString().ToLowerInvariant()
    $nativeExtractText = $IncludeNativeLibrariesForSelfExtract.ToString().ToLowerInvariant()
    $compressionText = $SelfContained.ToString().ToLowerInvariant()
    $allContentExtractText = $SelfContained.ToString().ToLowerInvariant()

    dotnet publish $projectPath `
        -c $Configuration `
        -r $Runtime `
        -p:SelfContained=$selfContainedText `
        -p:PublishSingleFile=$singleFileText `
        -p:IncludeNativeLibrariesForSelfExtract=$nativeExtractText `
        -p:IncludeAllContentForSelfExtract=$allContentExtractText `
        -p:EnableCompressionInSingleFile=$compressionText `
        -p:PublishTrimmed=false `
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:UseAppHost=true `
        -o $OutputDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for flavor '$Label' (exit code $LASTEXITCODE)"
    }

    $publishedExe = Get-PublishedExe -OutputDir $OutputDir
    $targetPath = Join-Path $releaseOut $TargetExeName

    if (Test-Path -LiteralPath $targetPath) {
        Remove-Item -LiteralPath $targetPath -Force
    }

    Copy-Item -LiteralPath $publishedExe.FullName -Destination $targetPath -Force
    Write-Host "Release asset: $TargetExeName"
}

if ($Clean) {
    foreach ($path in @($releaseOut, $workRoot)) {
        if (Test-Path -LiteralPath $path) {
            Remove-Item -LiteralPath $path -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Path $releaseOut -Force | Out-Null

Write-Host "Restoring project..."
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed (exit code $LASTEXITCODE)"
}

switch ($Flavor) {
    "both" {
        Invoke-PublishFlavor -Label "standalone (with .NET runtime)" -SelfContained $true -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $true -OutputDir $withNetOut -TargetExeName $standaloneExeName
        Invoke-PublishFlavor -Label "framework-dependent (requires installed .NET runtime)" -SelfContained $false -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $false -OutputDir $withoutNetOut -TargetExeName $frameworkDependentExeName
    }
    "with-net" {
        Invoke-PublishFlavor -Label "standalone (with .NET runtime)" -SelfContained $true -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $true -OutputDir $withNetOut -TargetExeName $standaloneExeName
    }
    "without-net" {
        Invoke-PublishFlavor -Label "framework-dependent (requires installed .NET runtime)" -SelfContained $false -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $false -OutputDir $withoutNetOut -TargetExeName $frameworkDependentExeName
    }
}

if (Test-Path -LiteralPath $workRoot) {
    Remove-Item -LiteralPath $workRoot -Recurse -Force
}

Write-Host ""
Write-Host "Done."
Write-Host "Release files: $releaseOut"
Get-ChildItem -LiteralPath $releaseOut -File -Filter *.exe |
    Sort-Object Name |
    ForEach-Object {
        Write-Host ("  {0} ({1:N2} MB)" -f $_.Name, ($_.Length / 1MB))
    }
