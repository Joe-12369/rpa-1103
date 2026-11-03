$ErrorActionPreference = "Stop"

$currentDir = $PSScriptRoot
if ([string]::IsNullOrEmpty($currentDir)) {
    $currentDir = (Get-Location).Path
}

# Define paths relative to the current script location (src folder)
$extensionDir = Join-Path $currentDir "..\tools\BrowserExtension"
$targetDir = Join-Path $currentDir "II.RPA.WebBrowser.Common\ExtensionScripts"

Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "1. Calculating initial file hashes..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
$initialStates = @{}

if (Test-Path $targetDir) {
    $files = Get-ChildItem -Path $targetDir -Filter "*.js" -File
    foreach ($file in $files) {
        $hash = (Get-FileHash -Path $file.FullName -Algorithm MD5).Hash
        $initialStates[$file.Name] = @{
            Hash = $hash
            LastWriteTime = $file.LastWriteTime
        }
    }
    Write-Host "Found $($initialStates.Count) .js files in $targetDir."
} else {
    Write-Warning "Target directory does not exist: $targetDir"
}

Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "2. Executing npm scripts..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

if (-not (Test-Path $extensionDir)) {
    Write-Error "BrowserExtension directory not found at: $extensionDir"
    exit 1
}

Push-Location $extensionDir
try {
    Write-Host "Installing/Updating npm dependencies..."
    cmd /c "npm install"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm install failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }

    # Execute npm commands
    Write-Host "Running 'npm run ap' and 'npm run aps'..."
    cmd /c "npm run ap && npm run aps"

    if ($LASTEXITCODE -ne 0) {
        Write-Error "npm scripts failed with exit code $LASTEXITCODE"
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}

Write-Host "`n=====================================" -ForegroundColor Cyan
Write-Host "3. Verifying file changes..." -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan

if (Test-Path $targetDir) {
    $hasUnchanged = $false
    $files = Get-ChildItem -Path $targetDir -Filter "*.js" -File
    foreach ($file in $files) {
        $newHash = (Get-FileHash -Path $file.FullName -Algorithm MD5).Hash
        $newTime = $file.LastWriteTime
        $oldState = $initialStates[$file.Name]

        $paddedName = $file.Name.PadRight(35)
        if ($null -eq $oldState) {
            Write-Host ("{0,-15} {1} - {2}" -f "[NEW]", $paddedName, "File created successfully.") -ForegroundColor Green
        } elseif ($newHash -ne $oldState.Hash) {
            Write-Host ("{0,-15} {1} - {2} (New Hash: {3})" -f "[CHANGED    ]", $paddedName, "Script content updated.", $newHash) -ForegroundColor Green
        } elseif ($newTime -ne $oldState.LastWriteTime) {
            Write-Host ("{0,-15} {1} - {2} (Hash: {3})" -f "[OVERWRITTEN]", $paddedName, "Copied but content identical.", $newHash) -ForegroundColor Cyan
        } else {
            Write-Host ("{0,-15} {1} - {2} (Hash: {3})" -f "[UNCHANGED  ]", $paddedName, "Hash and timestamp did not change.", $oldState.Hash) -ForegroundColor Yellow
            $hasUnchanged = $true
        }

        # Remove from dictionary to track deleted files
        if ($initialStates.ContainsKey($file.Name)) {
            $initialStates.Remove($file.Name)
        }
    }

    # Files that were in the directory before but are missing now
    foreach ($fileName in $initialStates.Keys) {
        Write-Host ("{0,-15} {1} - {2}" -f "[DELETED]", $fileName.PadRight(35), "File was removed during the build.") -ForegroundColor Red
    }
} else {
    Write-Error "Target directory not found after build: $targetDir"
}

if ($hasUnchanged) {
    Throw "Build failed! There are UNCHANGED files detected. Stopping CI flow."
}

Write-Host "`nDone!" -ForegroundColor Cyan