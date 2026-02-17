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
$publishRoot = Join-Path $PSScriptRoot "bin\\Publish"
$withNetOut = Join-Path $publishRoot "self-contained"
$withoutNetOut = Join-Path $publishRoot "framework-dependent"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file not found: $projectPath"
}

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "dotnet CLI is not available in PATH."
}

function Rename-PublishedExe {
    param(
        [string]$OutputDir,
        [string]$FriendlyExeName
    )

    $publishedExe = Get-ChildItem -LiteralPath $OutputDir -File -Filter *.exe |
        Sort-Object Length -Descending |
        Select-Object -First 1

    if (-not $publishedExe) {
        throw "No EXE found in publish output: $OutputDir"
    }

    $targetFileName = "$FriendlyExeName.exe"
    if ($publishedExe.Name -ieq $targetFileName) {
        return
    }

    $targetPath = Join-Path $OutputDir $targetFileName
    if (Test-Path -LiteralPath $targetPath) {
        Remove-Item -LiteralPath $targetPath -Force
    }

    Rename-Item -LiteralPath $publishedExe.FullName -NewName $targetFileName
    Write-Host "Renamed EXE: $targetFileName"
}

function Invoke-PublishFlavor {
    param(
        [string]$Label,
        [bool]$SelfContained,
        [bool]$PublishSingleFile,
        [bool]$IncludeNativeLibrariesForSelfExtract,
        [string]$OutputDir,
        [string]$FriendlyExeName
    )

    if ($Clean -and (Test-Path -LiteralPath $OutputDir)) {
        Remove-Item -LiteralPath $OutputDir -Recurse -Force
    }

    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

    Write-Host ""
    Write-Host "=== Publishing: $Label ==="
    Write-Host "Output: $OutputDir"

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
        -p:DebugType=None `
        -p:DebugSymbols=false `
        -p:UseAppHost=true `
        -o $OutputDir

    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish failed for flavor '$Label' (exit code $LASTEXITCODE)"
    }

    Rename-PublishedExe -OutputDir $OutputDir -FriendlyExeName $FriendlyExeName
}

Write-Host "Restoring project..."
dotnet restore $projectPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet restore failed (exit code $LASTEXITCODE)"
}

switch ($Flavor) {
    "both" {
        Invoke-PublishFlavor -Label "with .NET (self-contained)" -SelfContained $true -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $true -OutputDir $withNetOut -FriendlyExeName "MMCSS TWEAKER (Portable, .NET Included)"
        Invoke-PublishFlavor -Label "without .NET (framework-dependent)" -SelfContained $false -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $false -OutputDir $withoutNetOut -FriendlyExeName "MMCSS TWEAKER (.NET Runtime Required)"
    }
    "with-net" {
        Invoke-PublishFlavor -Label "with .NET (self-contained)" -SelfContained $true -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $true -OutputDir $withNetOut -FriendlyExeName "MMCSS TWEAKER (Portable, .NET Included)"
    }
    "without-net" {
        Invoke-PublishFlavor -Label "without .NET (framework-dependent)" -SelfContained $false -PublishSingleFile $true -IncludeNativeLibrariesForSelfExtract $false -OutputDir $withoutNetOut -FriendlyExeName "MMCSS TWEAKER (.NET Runtime Required)"
    }
}

Write-Host ""
Write-Host "Done."
Write-Host "with .NET:    $(Join-Path $withNetOut 'MMCSS TWEAKER (Portable, .NET Included).exe')"
Write-Host "without .NET: $(Join-Path $withoutNetOut 'MMCSS TWEAKER (.NET Runtime Required).exe')"
