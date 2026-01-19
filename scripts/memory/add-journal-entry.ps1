<#
add-journal-entry.ps1
Adds a journal entry to the current month's journal file.
Ensures only one heading per date (appends to existing date if present).
Tags are canonicalized against tag-vocabulary.md.
BOM-safe file reading.

USAGE:
  powershell -File .\scripts\memory\add-journal-entry.ps1 -Tags "UI,Fix" -Title "Fixed button alignment"
  powershell -File .\scripts\memory\add-journal-entry.ps1 -Tags "Build" -Title "Updated dependencies" -Files "package.json"
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Tags,
  [Parameter(Mandatory=$true)][string]$Title,
  [string]$Files = "",
  [string]$Why = "",
  [string]$Date = (Get-Date -Format "yyyy-MM-dd")
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($PSScriptRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
  $RepoRoot = (Get-Location).Path
}

$MemoryDir = Join-Path $RepoRoot ".cursor\memory"
$JournalDir = Join-Path $MemoryDir "journal"
$TagVocabPath = Join-Path $MemoryDir "tag-vocabulary.md"

if (-not (Test-Path $JournalDir)) {
  New-Item -ItemType Directory -Force -Path $JournalDir | Out-Null
}

function ReadText([string]$p) {
  $t = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $p
  if ($t.Length -gt 0 -and [int]$t[0] -eq 0xFEFF) { $t = $t.Substring(1) }
  return $t
}

# Load canonical tags
$canonTags = @{}
if (Test-Path $TagVocabPath) {
  $tv = ReadText $TagVocabPath
  foreach ($m in [regex]::Matches($tv, '(?m)^\-\s+\[([^\]]+)\]')) {
    $canon = $m.Groups[1].Value.Trim()
    $canonTags[$canon.ToLower()] = $canon
  }
}

$month = $Date.Substring(0, 7)
$journalFile = Join-Path $JournalDir "$month.md"

# Normalize tags
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
$tagString = ($finalTags | ForEach-Object { "[$_]" }) -join ""

# Build entry
$entryLines = @()
$entryLines += "- $tagString $Title"
if ($Why) { $entryLines += "  - Why: $Why" }
if ($Files) {
  $entryLines += "  - Key files:"
  foreach ($f in ($Files -split ',')) { $entryLines += "    - ``$($f.Trim())``" }
}
$entry = $entryLines -join "`r`n"

$enc = New-Object System.Text.UTF8Encoding($false)
$dateHeading = "## $Date"
$safeDate = [regex]::Escape($Date)

if (Test-Path $journalFile) {
  $content = ReadText $journalFile

  if ($content -match "(?m)^##\s+$safeDate\s*$") {
    # Append to existing date block
    $pattern = "(?ms)(^##\s+$safeDate\s*\r?\n)(.*?)(?=^##\s+\d{4}-\d{2}-\d{2}\s*$|\z)"
    $rx = New-Object System.Text.RegularExpressions.Regex($pattern, [System.Text.RegularExpressions.RegexOptions]::Multiline)
    $content = $rx.Replace($content, {
      param($m)
      $block = $m.Value.TrimEnd()
      return $block + "`r`n`r`n" + $entry + "`r`n"
    }, 1)
  } else {
    $content = $content.TrimEnd() + "`r`n`r`n$dateHeading`r`n`r`n$entry`r`n"
  }

  [System.IO.File]::WriteAllText($journalFile, ($content -replace "`r?`n", "`r`n"), $enc)
} else {
  $projectName = Split-Path -Leaf $RepoRoot
  $header = @"
# Development Journal - $projectName ($month)

## $Date

$entry
"@
  [System.IO.File]::WriteAllText($journalFile, ($header -replace "`r?`n", "`r`n"), $enc)
}

Write-Host "Added journal entry to: $journalFile" -ForegroundColor Green
Write-Host "  Date: $Date" -ForegroundColor Gray
Write-Host "  Tags: $tagString" -ForegroundColor Gray
Write-Host "  Title: $Title" -ForegroundColor Gray