<#
add-lesson.ps1
Creates a new lesson file with proper ID and YAML frontmatter.
Automatically assigns the next available lesson ID.
Tags are canonicalized against tag-vocabulary.md.

USAGE:
  powershell -File .\scripts\memory\add-lesson.ps1 -Title "Always validate input" -Tags "Reliability,Data" -Rule "Validate all user input before processing"
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Title,
  [Parameter(Mandatory=$true)][string]$Tags,
  [Parameter(Mandatory=$true)][string]$Rule,
  [string]$AppliesTo = "*",
  [string]$Triggers = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($PSScriptRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
  $RepoRoot = (Get-Location).Path
}

$MemoryDir = Join-Path $RepoRoot ".cursor\memory"
$LessonsDir = Join-Path $MemoryDir "lessons"
$TagVocabPath = Join-Path $MemoryDir "tag-vocabulary.md"

if (-not (Test-Path $LessonsDir)) {
  New-Item -ItemType Directory -Force -Path $LessonsDir | Out-Null
}

function ReadText([string]$p) {
  $t = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $p
  if ($t.Length -gt 0 -and [int]$t[0] -eq 0xFEFF) { $t = $t.Substring(1) }
  return $t
}

# Load canonical tags (case-insensitive lookup -> canonical casing)
$canonTags = @{}
if (Test-Path $TagVocabPath) {
  $tv = ReadText $TagVocabPath
  foreach ($m in [regex]::Matches($tv, '(?m)^\-\s+\[([^\]]+)\]')) {
    $canon = $m.Groups[1].Value.Trim()
    $canonTags[$canon.ToLower()] = $canon
  }
}

# Find next available ID
$existingLessons = Get-ChildItem -Path $LessonsDir -Filter "L-*.md" -ErrorAction SilentlyContinue
$maxId = 0
foreach ($lf in $existingLessons) {
  if ($lf.Name -match '^L-(\d{3})') {
    $id = [int]$Matches[1]
    if ($id -gt $maxId) { $maxId = $id }
  }
}

$lessonId = "L-{0:D3}" -f ($maxId + 1)

# Create kebab-case filename (fallback if empty)
$kebabTitle = ($Title.ToLower() -replace '[^a-z0-9]+', '-' -replace '^-|-$', '')
if ([string]::IsNullOrWhiteSpace($kebabTitle)) { $kebabTitle = "lesson" }
if ($kebabTitle.Length -gt 50) { $kebabTitle = $kebabTitle.Substring(0, 50) }
$fileName = "$lessonId-$kebabTitle.md"
$filePath = Join-Path $LessonsDir $fileName

# Normalize tags using vocabulary if present
$rawTags = $Tags -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
$finalTags = @()
foreach ($t in $rawTags) {
  $k = $t.ToLower()
  if ($canonTags.Count -gt 0) {
    if ($canonTags.ContainsKey($k)) { $finalTags += $canonTags[$k] }
    else { throw "Unknown tag '$t'. Add it to tag-vocabulary.md or fix the tag." }
  } else {
    $finalTags += $t
  }
}
$finalTags = $finalTags | Select-Object -Unique
$tagsYaml = "[$($finalTags -join ', ')]"

# Format applies_to
$appliesLines = @()
foreach ($a in ($AppliesTo -split ',')) { $appliesLines += "  - $($a.Trim())" }
$appliesYaml = $appliesLines -join "`r`n"

# Format triggers
if ($Triggers) {
  $triggerLines = @()
  foreach ($t in ($Triggers -split ',')) { $triggerLines += "  - $($t.Trim())" }
  $triggersYaml = "triggers:`r`n" + ($triggerLines -join "`r`n")
} else {
  $triggersYaml = "triggers:`r`n  - TODO: add error messages or keywords"
}

$today = Get-Date -Format "yyyy-MM-dd"

$content = @"
---
id: $lessonId
title: $Title
status: Active
tags: $tagsYaml
introduced: $today
applies_to:
$appliesYaml
$triggersYaml
rule: $Rule
---

# $lessonId - $Title

## Symptom

TODO: Describe what happened

## Root Cause

TODO: Describe why it happened

## Wrong Approach (DO NOT REPEAT)

- TODO: What not to do

## Correct Approach

- TODO: What to do instead

## References

- Files: ``TODO``
- Journal: ``journal/$($today.Substring(0,7)).md#$today``
"@

$enc = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($filePath, ($content -replace "`r?`n", "`r`n"), $enc)

Write-Host "Created lesson: $filePath" -ForegroundColor Green
Write-Host "  ID: $lessonId" -ForegroundColor Gray
Write-Host "  Title: $Title" -ForegroundColor Gray
Write-Host "  Tags: $tagsYaml" -ForegroundColor Gray
Write-Host ""
Write-Host "Next: run scripts\memory\rebuild-memory-index.ps1" -ForegroundColor Cyan