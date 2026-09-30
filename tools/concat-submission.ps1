$ErrorActionPreference = "Stop"

$repo = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$scriptsDir = Join-Path $repo "Scripts"
$outDir = Join-Path $repo "submission"
$outFile = Join-Path $outDir "FootballPenaltyChallenge.cs"
$csprojPath = Join-Path $repo "FootballPenaltyChallenge.csproj"

$files = Get-ChildItem -Path $scriptsDir -Filter *.cs -Recurse -File |
	Where-Object { $_.FullName -notmatch '[\\/]Verify[\\/]' } |
	Sort-Object FullName

if ($files.Count -eq 0) {
	throw "No program source files found under Scripts/"
}

New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$nl = "`r`n"
$parts = [System.Collections.Generic.List[string]]::new()
$parts.Add("// SUBMISSION COPY. Generated from Scripts/. Do not edit.")
$parts.Add("// Work stays in the separate files. Regenerate: powershell -File tools/concat-submission.ps1")
$parts.Add("")

foreach ($file in $files) {
	$rel = $file.FullName.Substring($repo.Length).TrimStart('\', '/').Replace('\', '/')
	$body = (Get-Content -Raw -LiteralPath $file.FullName).TrimEnd("`r", "`n")
	$parts.Add("// ===== $rel =====")
	$parts.Add($body)
	$parts.Add("")
}

$text = (($parts -join $nl).TrimEnd() + $nl)
[System.IO.File]::WriteAllText($outFile, $text)

$concat = [System.IO.File]::ReadAllText($outFile)
$missing = @()
foreach ($file in $files) {
	$body = (Get-Content -Raw -LiteralPath $file.FullName).TrimEnd("`r", "`n")
	if (-not $concat.Contains($body)) {
		$missing += $file.FullName
	}
}
if ($missing.Count -gt 0) {
	throw "Concat is missing source: $($missing -join ', ')"
}

$csproj = Get-Content -Raw -LiteralPath $csprojPath
if ($csproj -notmatch 'Compile\s+Remove\s*=\s*"submission/') {
	throw "FootballPenaltyChallenge.csproj must Compile Remove submission/ so Godot does not compile the concat copy"
}

Write-Host "Wrote $outFile ($($files.Count) files)"
foreach ($file in $files) {
	$rel = $file.FullName.Substring($repo.Length).TrimStart('\', '/')
	Write-Host "  $rel"
}
