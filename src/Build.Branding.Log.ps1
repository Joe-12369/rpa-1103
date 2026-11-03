param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('reset', 'append', 'summary', 'clear')]
    [string]$Action,

    [Parameter(Mandatory = $true)]
    [string]$LogPath,

    [string]$ProjectName,

    [string]$FieldList
)

function Get-MutexName {
    param([string]$Path)

    $bytes = [System.Text.Encoding]::UTF8.GetBytes($Path)
    $token = [System.Convert]::ToBase64String($bytes).TrimEnd('=')
    $token = $token.Replace('+', '-').Replace('/', '_')
    return 'Global\II.RPA.Branding.' + $token
}

if ([string]::IsNullOrWhiteSpace($LogPath)) {
    exit 0
}

$directory = Split-Path -Parent $LogPath
if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
    New-Item -ItemType Directory -Path $directory -Force | Out-Null
}

$mutex = [System.Threading.Mutex]::new($false, (Get-MutexName -Path $LogPath))
$lockTaken = $false

try {
    $lockTaken = $mutex.WaitOne()

    switch ($Action) {
        'reset' {
            if (Test-Path -LiteralPath $LogPath) {
                Remove-Item -LiteralPath $LogPath -Force
            }
        }
        'append' {
            if (-not [string]::IsNullOrWhiteSpace($ProjectName)) {
                Add-Content -LiteralPath $LogPath -Value ($ProjectName + "`t" + [string]$FieldList) -Encoding UTF8
            }
        }
        'summary' {
            Write-Host '[Branding] Dynamic processing summary begin'

            if (-not (Test-Path -LiteralPath $LogPath)) {
                Write-Host '[Branding] Applied projects: 0'
                Write-Host '[Branding] No projects participated in dynamic branding.'
                Write-Host '[Branding] Dynamic processing summary end'
                break
            }

            $projectGroups = Get-Content -LiteralPath $LogPath |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
            ForEach-Object {
                $parts = [string]$_ -split "`t", 2
                [pscustomobject]@{
                    ProjectName = $parts[0].Trim()
                    FieldList   = if ($parts.Count -gt 1) { $parts[1].Trim() } else { '' }
                }
            } |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_.ProjectName) } |
            Group-Object -Property ProjectName |
            Sort-Object -Property Name

            Write-Host ('[Branding] Applied projects: ' + $projectGroups.Count)
            if ($projectGroups.Count -eq 0) {
                Write-Host '[Branding] No projects participated in dynamic branding.'
                Write-Host '[Branding] Dynamic processing summary end'
                break
            }

            Write-Host '[Branding] Project list:'
            foreach ($projectGroup in $projectGroups) {
                $appliedFields = @(
                    $projectGroup.Group |
                    ForEach-Object {
                        if (-not [string]::IsNullOrWhiteSpace($_.FieldList)) {
                            $_.FieldList -split '\s*,\s*'
                        }
                    } |
                    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                    Sort-Object -Unique
                )

                if ($appliedFields.Count -eq 0) {
                    Write-Host ('[Branding]   ' + $projectGroup.Name)
                    continue
                }

                Write-Host ('[Branding]   ' + $projectGroup.Name + ' fields: ' + ($appliedFields -join ', '))
            }

            Write-Host '[Branding] Dynamic processing summary end'
        }
        'clear' {
            if (Test-Path -LiteralPath $LogPath) {
                Remove-Item -LiteralPath $LogPath -Force
            }
        }
    }
}
finally {
    if ($lockTaken) {
        $mutex.ReleaseMutex() | Out-Null
    }

    $mutex.Dispose()
}