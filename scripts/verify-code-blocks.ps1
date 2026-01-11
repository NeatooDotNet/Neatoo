# verify-code-blocks.ps1
# Verifies all C# code blocks have appropriate markers
param(
    [string]$DocsPath = "docs",
    [switch]$Verbose
)

$errors = @()
$stats = @{
    Files = 0
    CompiledSnippets = 0
    PseudoSnippets = 0
    InvalidSnippets = 0
    Unmarked = 0
}

Get-ChildItem -Path $DocsPath -Recurse -Include "*.md" |
    Where-Object { $_.FullName -notmatch '[\\/](todos|release-notes)[\\/]' } |
    ForEach-Object {
    $file = $_
    $stats.Files++
    $content = Get-Content $file.FullName -Raw
    $lines = Get-Content $file.FullName

    # Count snippet types
    # MarkdownSnippets compiled snippets
    $stats.CompiledSnippets += ([regex]'(?m)^snippet:\s+\S+').Matches($content).Count
    $stats.CompiledSnippets += ([regex]'<!-- snippet: (?!pseudo:|invalid:|generated:)').Matches($content).Count
    # Manual markers (no 'snippet:' prefix to avoid MarkdownSnippets processing)
    $stats.PseudoSnippets += ([regex]'<!-- pseudo:').Matches($content).Count
    $stats.InvalidSnippets += ([regex]'<!-- invalid:').Matches($content).Count
    $generatedSnippets = ([regex]'<!-- generated:').Matches($content).Count

    # Check for unclosed manual snippets
    $pseudoOpens = ([regex]'<!-- pseudo:').Matches($content).Count
    $invalidOpens = ([regex]'<!-- invalid:').Matches($content).Count
    $generatedOpens = ([regex]'<!-- generated:').Matches($content).Count
    $manualCloses = ([regex]'<!-- /snippet -->').Matches($content).Count

    if (($pseudoOpens + $invalidOpens + $generatedOpens) -ne $manualCloses) {
        $errors += "$($file.Name): Unclosed snippet (pseudo:$pseudoOpens + invalid:$invalidOpens + generated:$generatedOpens opens, $manualCloses closes)"
    }

    # Find unmarked code blocks
    $lineNum = 0
    $inManagedSnippet = $false

    foreach ($line in $lines) {
        $lineNum++

        # Track managed snippets (MarkdownSnippets output or manual pseudo/invalid/generated)
        if ($line -match '^snippet:\s+\S+' -or $line -match '<!-- snippet:' -or $line -match '<!-- pseudo:' -or $line -match '<!-- invalid:' -or $line -match '<!-- generated:') {
            $inManagedSnippet = $true
        }
        if ($line -match '<!-- endSnippet -->' -or $line -match '<!-- /snippet -->') {
            $inManagedSnippet = $false
        }

        # Find ```csharp or ```cs blocks
        if ($line -match '^```(csharp|cs)') {
            if (-not $inManagedSnippet) {
                # Check if previous line has any snippet marker
                $prevLine = if ($lineNum -gt 1) { $lines[$lineNum - 2] } else { "" }
                if ($prevLine -notmatch 'snippet:|pseudo:|invalid:|generated:') {
                    $stats.Unmarked++
                    $errors += "$($file.Name):$lineNum - Unmarked C# code block"
                }
            }
        }
    }
}

# Output results
Write-Host "`n=== Code Block Verification ===" -ForegroundColor Cyan
Write-Host "Files scanned: $($stats.Files)"
Write-Host "Compiled snippets (MarkdownSnippets): $($stats.CompiledSnippets)" -ForegroundColor Green
Write-Host "Pseudo-code blocks: $($stats.PseudoSnippets)" -ForegroundColor Yellow
Write-Host "Invalid/anti-pattern blocks: $($stats.InvalidSnippets)" -ForegroundColor Yellow
Write-Host "Unmarked blocks: $($stats.Unmarked)" -ForegroundColor $(if ($stats.Unmarked -gt 0) { 'Red' } else { 'Green' })

if ($errors) {
    Write-Host "`nErrors found:" -ForegroundColor Red
    $errors | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
} else {
    Write-Host "`nAll code blocks are properly marked" -ForegroundColor Green
    exit 0
}
