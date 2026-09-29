$ErrorActionPreference = "Stop"
$root = Resolve-Path (Join-Path $PSScriptRoot "..\..\..\..")
Set-Location $root

$godot = "C:\godot\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"

if (-not (Test-Path $godot)) { throw "Godot missing: $godot" }
if (-not (Test-Path $dotnet)) { throw "dotnet missing: $dotnet" }

$project = Get-Content project.godot -Raw
if ($project -notmatch 'run/main_scene="res://Scenes/MainMenu/MainMenu.tscn"') {
	throw "main_scene is not MainMenu.tscn"
}

& $dotnet build FootballPenaltyChallenge.sln
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed" }

$log = Join-Path $root "verify-output\last-run.log"
New-Item -ItemType Directory -Force -Path (Join-Path $root "verify-output") | Out-Null

& $godot --path $root -- --verify *>&1 | Tee-Object -FilePath $log
if ($LASTEXITCODE -ne 0) { throw "Godot verify exited $LASTEXITCODE" }

$text = Get-Content $log -Raw
if ($text -notmatch "VERIFY passed") { throw "log missing VERIFY passed" }
if ($text -notmatch "outcome=Missed score=0 lives=0") { throw "log missing third miss" }

foreach ($name in @("menu-hard.png", "game-start.png", "game-over.png", "restart.png")) {
	$path = Join-Path $root "verify-output\$name"
	if (-not (Test-Path $path)) { throw "missing snapshot $name" }
}

Write-Host "verify-penalty-game ok"
