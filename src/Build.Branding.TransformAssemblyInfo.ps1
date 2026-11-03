param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [switch]$RemoveAssemblyCompany,
    [switch]$RemoveAssemblyCopyright,
    [switch]$RemoveAssemblyVersion,
    [switch]$RemoveAssemblyFileVersion,
    [switch]$RemoveAssemblyInformationalVersion
)

$patterns = @()

if ($RemoveAssemblyCompany) {
    $patterns += '^\s*\[assembly:\s*AssemblyCompany\(.*\)\]\s*$'
}

if ($RemoveAssemblyCopyright) {
    $patterns += '^\s*\[assembly:\s*AssemblyCopyright\(.*\)\]\s*$'
}

if ($RemoveAssemblyVersion) {
    $patterns += '^\s*\[assembly:\s*AssemblyVersion\(.*\)\]\s*$'
}

if ($RemoveAssemblyFileVersion) {
    $patterns += '^\s*\[assembly:\s*AssemblyFileVersion\(.*\)\]\s*$'
}

if ($RemoveAssemblyInformationalVersion) {
    $patterns += '^\s*\[assembly:\s*AssemblyInformationalVersion\(.*\)\]\s*$'
}

$reader = [System.IO.StreamReader]::new($InputPath, $true)

try {
    $content = $reader.ReadToEnd()
    $encoding = $reader.CurrentEncoding
}
finally {
    $reader.Dispose()
}

$outputLines = [System.Collections.Generic.List[string]]::new()

foreach ($line in [System.Text.RegularExpressions.Regex]::Split($content, "`r?`n")) {
    $skipLine = $false

    foreach ($pattern in $patterns) {
        if ($line -match $pattern) {
            $skipLine = $true
            break
        }
    }

    if (-not $skipLine) {
        $outputLines.Add($line)
    }
}

$outputDirectory = Split-Path -Parent $OutputPath

if (-not [string]::IsNullOrWhiteSpace($outputDirectory)) {
    [System.IO.Directory]::CreateDirectory($outputDirectory) | Out-Null
}

$normalizedContent = ($outputLines -join [System.Environment]::NewLine).TrimEnd()

if ($normalizedContent.Length -gt 0) {
    $normalizedContent += [System.Environment]::NewLine
}

[System.IO.File]::WriteAllText($OutputPath, $normalizedContent, $encoding)