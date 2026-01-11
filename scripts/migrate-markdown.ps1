param(
    [string]$Path = "docs",
    [switch]$WhatIf
)

$files = Get-ChildItem -Path $Path -Recurse -Include "*.md" -Exclude "*.source.md"
$converted = 0
$snippetsConverted = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Pattern to match old snippet blocks:
    # <!-- snippet: docs:{file}:{id} -->
    # ```csharp
    # ... any content ...
    # ```
    # <!-- /snippet -->

    $pattern = '(?s)<!-- snippet: docs:[^:]+:([^\s]+)\s*-->\r?\n```(?:csharp|razor)\r?\n.*?```\r?\n<!-- /snippet -->'

    $matches = [regex]::Matches($content, $pattern)
    $snippetsConverted += $matches.Count

    # Replace with new format
    $content = [regex]::Replace($content, $pattern, 'snippet: $1')

    if ($content -ne $original) {
        $converted++
        Write-Host "Converting: $($file.FullName) ($($matches.Count) snippets)" -ForegroundColor Yellow

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
        }
    }
}

Write-Host "`nConverted $snippetsConverted snippets in $converted files" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "(WhatIf mode - no files were modified)" -ForegroundColor Yellow
}
