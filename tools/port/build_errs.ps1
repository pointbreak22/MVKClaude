param([string]$Project = "src\Mvk.Engine", [int]$Top = 60)
Set-Location E:\Projects\Games\MvkClaude
$lines = dotnet build $Project -clp:NoSummary 2>&1 | Select-String ': error' | ForEach-Object {
    ($_.Line -replace ' \[E:.*$','') -replace '^.*\\src\\[^\\]+\\',''
} | Sort-Object -Unique
$lines | Out-File -Encoding utf8 $env:TEMP\mvk_errs.txt
"errors: " + $lines.Count
$lines | ForEach-Object {
    if ($_ -match '^(.*?)\((\d+),\d+\): error (CS\d+): .*?"([^"]+)"') { "{0} {1}" -f $matches[3], $matches[4] }
    elseif ($_ -match 'error (CS\d+): (.{0,70})') { "{0} {1}" -f $matches[1], $matches[2] }
} | Group-Object | Sort-Object Count -Descending | Select-Object -First $Top | ForEach-Object { "{0,4} {1}" -f $_.Count, $_.Name }
