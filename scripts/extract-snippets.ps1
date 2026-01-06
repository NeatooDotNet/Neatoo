<#
.SYNOPSIS
    Extracts code snippets from Neatoo.Samples and updates documentation.

.DESCRIPTION
    This script scans the docs/samples/ projects for #region docs:* markers,
    extracts the code snippets, and can optionally update the corresponding markdown
    documentation files.

.PARAMETER Verify
    Only verify that snippets exist and report status. Does not modify any files.

.PARAMETER Update
    Update the markdown documentation files with extracted snippets.

.PARAMETER SamplesPath
    Path to the samples directory. Defaults to docs/samples

.PARAMETER DocsPath
    Path to the docs directory. Defaults to docs/

.EXAMPLE
    .\extract-snippets.ps1 -Verify
    Verifies all snippet markers are valid without modifying files.

.EXAMPLE
    .\extract-snippets.ps1 -Update
    Extracts snippets and updates documentation files.
#>

param(
    [switch]$Verify,
    [switch]$Update,
    [string]$SamplesPath = "docs/samples",
    [string]$DocsPath = "docs"
)

$ErrorActionPreference = "Stop"

# Get the repository root
$RepoRoot = Split-Path -Parent $PSScriptRoot
$SamplesFullPath = Join-Path $RepoRoot $SamplesPath
$DocsFullPath = Join-Path $RepoRoot $DocsPath

Write-Host "Neatoo Documentation Snippet Extractor" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Samples Path: $SamplesFullPath"
Write-Host "Docs Path: $DocsFullPath"
Write-Host ""

# Pattern to match region markers: #region docs:{doc-file}:{snippet-id}
$regionPattern = '#region\s+docs:([^:\s]+):([^\s]+)'

# Find all C# and Razor files in samples
$sourceFiles = Get-ChildItem -Path $SamplesFullPath -Recurse -Include "*.cs", "*.razor" |
    Where-Object { $_.FullName -notmatch '[\\/](obj|bin|Generated)[\\/]' }

$snippets = @{}
$errors = @()

Write-Host "Scanning source files..." -ForegroundColor Yellow

foreach ($file in $sourceFiles) {
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    # Find all region markers
    $matches = [regex]::Matches($content, $regionPattern)

    foreach ($match in $matches) {
        $docFile = $match.Groups[1].Value
        $snippetId = $match.Groups[2].Value
        $key = "${docFile}:${snippetId}"

        # Find the line number of the region start
        $regionStartIndex = $content.Substring(0, $match.Index).Split("`n").Count - 1

        # Find the matching #endregion
        $afterRegion = $content.Substring($match.Index + $match.Length)
        $endRegionMatch = [regex]::Match($afterRegion, '#endregion')

        if (-not $endRegionMatch.Success) {
            $errors += "Missing #endregion for '$key' in $($file.Name)"
            continue
        }

        # Extract content between region and endregion
        $snippetContent = $afterRegion.Substring(0, $endRegionMatch.Index).Trim()

        # Remove leading/trailing blank lines
        $snippetContent = $snippetContent -replace '^\s*\r?\n', ''
        $snippetContent = $snippetContent -replace '\r?\n\s*$', ''

        if ($snippets.ContainsKey($key)) {
            $errors += "Duplicate snippet key '$key' found in $($file.Name)"
        } else {
            $snippets[$key] = @{
                Content = $snippetContent
                SourceFile = $file.Name
                DocFile = $docFile
                SnippetId = $snippetId
            }
        }
    }
}

Write-Host ""
Write-Host "Found $($snippets.Count) snippets:" -ForegroundColor Green

# Group by doc file
$byDocFile = $snippets.GetEnumerator() | Group-Object { $_.Value.DocFile }

foreach ($group in $byDocFile | Sort-Object Name) {
    Write-Host "  $($group.Name).md:" -ForegroundColor White
    foreach ($snippet in $group.Group | Sort-Object { $_.Value.SnippetId }) {
        Write-Host "    - $($snippet.Value.SnippetId) ($($snippet.Value.SourceFile))" -ForegroundColor Gray
    }
}

if ($errors.Count -gt 0) {
    Write-Host ""
    Write-Host "Errors:" -ForegroundColor Red
    foreach ($error in $errors) {
        Write-Host "  - $error" -ForegroundColor Red
    }
    exit 1
}

if ($Verify) {
    Write-Host ""
    Write-Host "Verifying documentation is in sync with samples..." -ForegroundColor Yellow

    $outOfSync = @()
    $orphanSnippets = @()
    $verifiedCount = 0

    foreach ($group in $byDocFile) {
        $docFileName = "$($group.Name).md"
        $docFilePath = Join-Path $DocsFullPath $docFileName

        if (-not (Test-Path $docFilePath)) {
            Write-Host "  Warning: Doc file not found: $docFileName" -ForegroundColor Yellow
            continue
        }

        $docContent = Get-Content $docFilePath -Raw

        foreach ($snippet in $group.Group) {
            $snippetId = $snippet.Value.SnippetId
            $expectedContent = $snippet.Value.Content

            # Pattern to extract current content from docs
            $markerPattern = "<!--\s*snippet:\s*docs:$($group.Name):$snippetId\s*-->\s*\r?\n``````(?:csharp|razor)?\r?\n([\s\S]*?)``````\s*\r?\n<!--\s*/snippet\s*-->"

            if ($docContent -match $markerPattern) {
                $currentContent = $Matches[1].Trim()
                $expectedTrimmed = $expectedContent.Trim()

                # Normalize line endings for comparison
                $currentNormalized = $currentContent -replace '\r\n', "`n"
                $expectedNormalized = $expectedTrimmed -replace '\r\n', "`n"

                if ($currentNormalized -ne $expectedNormalized) {
                    $outOfSync += "  - ${docFileName}: ${snippetId}"
                } else {
                    $verifiedCount++
                }
            } else {
                # Snippet exists in samples but no marker in docs - track as orphan (warning only)
                $orphanSnippets += "  - ${docFileName}: ${snippetId}"
            }
        }
    }

    if ($orphanSnippets.Count -gt 0) {
        Write-Host ""
        Write-Host "Orphan snippets (in samples but not in docs):" -ForegroundColor Yellow
        foreach ($item in $orphanSnippets) {
            Write-Host $item -ForegroundColor Yellow
        }
    }

    if ($outOfSync.Count -gt 0) {
        Write-Host ""
        Write-Host "Documentation out of sync with samples:" -ForegroundColor Red
        foreach ($item in $outOfSync) {
            Write-Host $item -ForegroundColor Red
        }
        Write-Host ""
        Write-Host "Run '.\scripts\extract-snippets.ps1 -Update' to sync documentation." -ForegroundColor Yellow
        exit 1
    }

    Write-Host ""
    Write-Host "Verification complete. $verifiedCount snippets verified, $($orphanSnippets.Count) orphan snippets." -ForegroundColor Green
    exit 0
}

if ($Update) {
    Write-Host ""
    Write-Host "Updating documentation files..." -ForegroundColor Yellow

    $updatedFiles = 0
    $snippetsUpdated = 0

    foreach ($group in $byDocFile) {
        $docFileName = "$($group.Name).md"
        $docFilePath = Join-Path $DocsFullPath $docFileName

        if (-not (Test-Path $docFilePath)) {
            Write-Host "  Warning: Doc file not found: $docFileName" -ForegroundColor Yellow
            continue
        }

        $docContent = Get-Content $docFilePath -Raw
        $originalContent = $docContent
        $fileUpdated = $false

        foreach ($snippet in $group.Group) {
            $snippetId = $snippet.Value.SnippetId
            $snippetContent = $snippet.Value.Content

            # Pattern to match snippet markers in markdown:
            # <!-- snippet: docs:doc-file:snippet-id -->
            # ```csharp
            # ... content ...
            # ```
            # <!-- /snippet -->

            $markerPattern = "<!--\s*snippet:\s*docs:$($group.Name):$snippetId\s*-->\s*\r?\n``````(?:csharp|razor)?\r?\n([\s\S]*?)``````\s*\r?\n<!--\s*/snippet\s*-->"

            if ($docContent -match $markerPattern) {
                $replacement = "<!-- snippet: docs:$($group.Name):$snippetId -->`n``````csharp`n$snippetContent`n```````n<!-- /snippet -->"
                $docContent = $docContent -replace $markerPattern, $replacement
                $snippetsUpdated++
                $fileUpdated = $true
            }
        }

        if ($fileUpdated -and $docContent -ne $originalContent) {
            Set-Content -Path $docFilePath -Value $docContent -NoNewline
            $updatedFiles++
            Write-Host "  Updated: $docFileName" -ForegroundColor Green
        }
    }

    Write-Host ""
    Write-Host "Update complete. $updatedFiles files updated, $snippetsUpdated snippets processed." -ForegroundColor Green
}

if (-not $Verify -and -not $Update) {
    Write-Host ""
    Write-Host "Usage:" -ForegroundColor Cyan
    Write-Host "  .\extract-snippets.ps1 -Verify    # Verify snippets without updating"
    Write-Host "  .\extract-snippets.ps1 -Update    # Update documentation files"
}
