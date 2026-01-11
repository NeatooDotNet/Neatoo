param([string]$Path = "docs/samples")

$snippets = @{}
$duplicates = @()

Get-ChildItem -Path $Path -Recurse -Include "*.cs" | ForEach-Object {
    $file = $_.FullName
    $content = Get-Content $file -Raw

    # Find all #region {name} patterns
    $regions = [regex]::Matches($content, '#region\s+([^\r\n]+)')

    foreach ($match in $regions) {
        $id = $match.Groups[1].Value.Trim()

        # Skip non-snippet regions (like standard Visual Studio regions)
        if ($id -match '^(Fields|Properties|Methods|Constructor|Private|Public)$') {
            continue
        }

        if ($snippets.ContainsKey($id)) {
            $duplicates += [PSCustomObject]@{
                Id = $id
                File1 = $snippets[$id]
                File2 = $file
            }
        } else {
            $snippets[$id] = $file
        }
    }
}

if ($duplicates) {
    Write-Host "DUPLICATE SNIPPET IDs FOUND:" -ForegroundColor Red
    $duplicates | Format-Table -AutoSize
    exit 1
} else {
    Write-Host "All $($snippets.Count) snippet IDs are unique" -ForegroundColor Green
}
