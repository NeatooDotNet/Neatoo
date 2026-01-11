param(
    [string]$Path = "docs/samples",
    [switch]$WhatIf
)

$files = Get-ChildItem -Path $Path -Recurse -Include "*.cs"
$converted = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $original = $content

    # Convert: #region docs:{anything}:{id} -> #region {id}
    $content = $content -replace '#region docs:[^:]+:([^\r\n]+)', '#region $1'

    if ($content -ne $original) {
        $converted++
        Write-Host "Converting: $($file.FullName)" -ForegroundColor Yellow

        # Show what changed
        $oldRegions = [regex]::Matches($original, '#region docs:[^:]+:([^\r\n]+)')
        $newRegions = [regex]::Matches($content, '#region ([^\r\n]+)')

        for ($i = 0; $i -lt $oldRegions.Count; $i++) {
            Write-Host "  $($oldRegions[$i].Value) -> #region $($oldRegions[$i].Groups[1].Value)" -ForegroundColor Gray
        }

        if (-not $WhatIf) {
            Set-Content -Path $file.FullName -Value $content -NoNewline
        }
    }
}

Write-Host "`nConverted $converted files" -ForegroundColor Cyan
if ($WhatIf) {
    Write-Host "(WhatIf mode - no files were modified)" -ForegroundColor Yellow
}
