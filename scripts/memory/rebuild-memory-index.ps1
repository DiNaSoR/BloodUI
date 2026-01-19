<#
rebuild-memory-index.ps1
Generates:
- lessons/index.md + lessons-index.json
- journal-index.md + journal-index.json
- digests/YYYY-MM.digest.md
Optionally:
- memory.sqlite (if Python exists)

Works on PowerShell 5.1+ (no external deps).
BOM-tolerant parsing.
#>

[CmdletBinding()]
param([string]$RepoRoot = "")

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($RepoRoot)) {
  if ($PSScriptRoot) {
    $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
  } else {
    $RepoRoot = (Get-Location).Path
  }
}

function Write-Utf8NoBom([string]$FilePath, [string]$Content) {
  $dir = Split-Path -Parent $FilePath
  if ($dir -and !(Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
  $enc = New-Object System.Text.UTF8Encoding($false)
  $normalized = ($Content -replace "`r?`n", "`r`n")
  [System.IO.File]::WriteAllText($FilePath, $normalized, $enc)
}

function Read-Utf8([string]$Path) {
  $raw = Get-Content -Raw -Encoding UTF8 $Path
  # Strip BOM if present
  if ($raw.Length -gt 0 -and [int]$raw[0] -eq 0xFEFF) { $raw = $raw.Substring(1) }
  return $raw
}

# Strict YAML frontmatter parser (BOM-tolerant, tracks current list key)
function Parse-Frontmatter([string]$FilePath) {
  $raw = Read-Utf8 $FilePath
  $result = @{}
  $lines = $raw -split "`r?`n"

  if ($lines.Count -lt 3 -or $lines[0].Trim() -ne "---") { return $null }

  $i = 1
  $currentListKey = $null
  while ($i -lt $lines.Count) {
    $line = $lines[$i]
    if ($line.Trim() -eq "---") { break }

    # ignore comments/blank
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) { $i++; continue }

    # list item
    if ($line -match '^\s*-\s+(.+)$' -and $currentListKey) {
      if (-not ($result.ContainsKey($currentListKey))) { $result[$currentListKey] = @() }
      $result[$currentListKey] += $Matches[1].Trim()
      $i++; continue
    }

    # key: value
    if ($line -match '^\s*([A-Za-z0-9_]+)\s*:\s*(.*)\s*$') {
      $key = $Matches[1].Trim().ToLower()
      $val = $Matches[2]

      # key: (empty, start of list)
      if ([string]::IsNullOrWhiteSpace($val)) {
        $result[$key] = @()
        $currentListKey = $key
        $i++; continue
      }

      $currentListKey = $null
      $v = $val.Trim()

      # strip quotes
      if (($v.StartsWith('"') -and $v.EndsWith('"')) -or ($v.StartsWith("'") -and $v.EndsWith("'"))) {
        $v = $v.Substring(1, $v.Length-2)
      }

      # inline array: [a, b]
      if ($v -match '^\[(.*)\]$') {
        $inner = $Matches[1]
        if ([string]::IsNullOrWhiteSpace($inner)) {
          $result[$key] = @()
        } else {
          $items = $inner -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
          $result[$key] = @($items)
        }
      } else {
        $result[$key] = $v
      }

      $i++; continue
    }

    # unknown line -> skip
    $i++
  }

  return $result
}

$MemoryDir  = Join-Path $RepoRoot ".cursor\memory"
$LessonsDir = Join-Path $MemoryDir "lessons"
$JournalDir = Join-Path $MemoryDir "journal"
$DigestsDir = Join-Path $MemoryDir "digests"

if (!(Test-Path $LessonsDir)) { New-Item -ItemType Directory -Force -Path $LessonsDir | Out-Null }
if (!(Test-Path $JournalDir)) { throw "Missing: $JournalDir" }
if (!(Test-Path $DigestsDir)) { New-Item -ItemType Directory -Force -Path $DigestsDir | Out-Null }

# Lessons -> index.md + lessons-index.json
$lessonFiles = Get-ChildItem -Path $LessonsDir -File -Filter "L-*.md" -ErrorAction SilentlyContinue | Sort-Object Name
$lessons = @()

foreach ($lf in $lessonFiles) {
  $yaml = Parse-Frontmatter $lf.FullName
  if ($null -eq $yaml) { continue }
  if (-not $yaml.ContainsKey("id")) { continue }

  $id = [string]$yaml["id"]
  $num = 0
  if ($id -match 'L-(\d+)') { $num = [int]$Matches[1] }

  $title = if ($yaml.ContainsKey("title")) { [string]$yaml["title"] } else { $lf.BaseName }
  $status = if ($yaml.ContainsKey("status")) { [string]$yaml["status"] } else { "Active" }
  $introduced = if ($yaml.ContainsKey("introduced")) { [string]$yaml["introduced"] } else { "" }

  $tags = @()
  if ($yaml.ContainsKey("tags")) { $tags = @($yaml["tags"]) }

  $applies = @()
  if ($yaml.ContainsKey("applies_to")) { $applies = @($yaml["applies_to"]) }

  $rule = ""
  if ($yaml.ContainsKey("rule")) { $rule = [string]$yaml["rule"] } else { $rule = $title }

  $lessons += [pscustomobject]@{
    Id = $id
    Num = $num
    Title = $title
    Status = $status
    Introduced = $introduced
    Tags = $tags
    AppliesTo = $applies
    Rule = $rule
    File = $lf.Name
  }
}

$lessons = $lessons | Sort-Object Num

$gen = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssK")
$idx = @()
$idx += "# Lessons Index (generated)"
$idx += ""
$idx += "Generated: $gen"
$idx += ""
$idx += "Format: ID | [Tags] | AppliesTo | Rule | File"
$idx += ""

if (-not $lessons -or @($lessons).Count -eq 0) {
  $idx += "(No lessons yet.)"
} else {
  foreach ($l in $lessons) {
    $tagText = ($l.Tags | ForEach-Object { "[$_]" }) -join ""
    $appliesText = "(any)"
    if ($l.AppliesTo -and @($l.AppliesTo).Count -gt 0) { $appliesText = ($l.AppliesTo -join ", ") }
    $idx += "$($l.Id) | $tagText | $appliesText | $($l.Rule) | ``$($l.File)``"
  }
}

Write-Utf8NoBom (Join-Path $LessonsDir "index.md") ($idx -join "`n")
$lessonsJson = if (-not $lessons -or @($lessons).Count -eq 0) { "[]" } else { ConvertTo-Json -InputObject @($lessons) -Depth 6 }
Write-Utf8NoBom (Join-Path $MemoryDir "lessons-index.json") $lessonsJson

# Journal -> journal-index.md + journal-index.json + digests
$journalFiles = Get-ChildItem -Path $JournalDir -File | Where-Object {
  $_.Name -match '^\d{4}-\d{2}\.md$' -and $_.Name -ne 'README.md'
} | Sort-Object Name

$journalEntries = New-Object System.Collections.Generic.List[object]

foreach ($jf in $journalFiles) {
  $text = Read-Utf8 $jf.FullName

  $datePattern = '(?m)^##\s+(\d{4}-\d{2}-\d{2}).*$'
  $dateMatches = [regex]::Matches($text, $datePattern)

  for ($i=0; $i -lt $dateMatches.Count; $i++) {
    $date = $dateMatches[$i].Groups[1].Value
    $start = $dateMatches[$i].Index
    $end = if ($i -lt $dateMatches.Count - 1) { $dateMatches[$i+1].Index } else { $text.Length }
    $block = $text.Substring($start, $end - $start)

    $entryPattern = '(?m)^-\s+(\[[^\]]+\]){1,}.*$'
    $entryMatches = [regex]::Matches($block, $entryPattern)

    for ($j=0; $j -lt $entryMatches.Count; $j++) {
      $eStart = $entryMatches[$j].Index
      $eEnd = if ($j -lt $entryMatches.Count - 1) { $entryMatches[$j+1].Index } else { $block.Length }
      $eBlock = $block.Substring($eStart, $eEnd - $eStart).Trim()

      $firstLine = ($eBlock -split "`r?`n")[0].Trim()

      $tags = @()
      foreach ($tm in [regex]::Matches($firstLine, '\[([^\]]+)\]')) { $tags += $tm.Groups[1].Value.Trim() }

      $title = ($firstLine -replace '^-\s+(\[[^\]]+\])+\s*', '').Trim()

      $files = @()
      foreach ($fm in [regex]::Matches($eBlock, '`([^`]+)`')) {
        $v = $fm.Groups[1].Value.Trim()
        if ($v -match '[/\\]' -or $v -match '\.(cs|md|mdx|yml|yaml|csproj|ps1|ts|tsx|json|py)$') {
          $files += $v
        }
      }
      $files = $files | Select-Object -Unique

      $journalEntries.Add([pscustomobject]@{
        MonthFile = $jf.Name
        Date = $date
        Tags = $tags
        Title = $title
        Files = $files
      })
    }
  }

  # digest for this month
  $monthName = [System.IO.Path]::GetFileNameWithoutExtension($jf.Name)
  $digestPath = Join-Path $DigestsDir "$monthName.digest.md"

  $digest = @()
  $digest += "# Monthly Digest - $monthName (generated)"
  $digest += ""
  $digest += "Generated: $gen"
  $digest += ""
  $digest += "Token-cheap summary. See ``.cursor/memory/journal/$($jf.Name)`` for details."
  $digest += ""

  $dates = [regex]::Matches($text, '(?m)^##\s+(\d{4}-\d{2}-\d{2}).*$') | ForEach-Object { $_.Groups[1].Value }
  $uniqueDates = $dates | Select-Object -Unique
  foreach ($d in $uniqueDates) {
    $digest += "## $d"
    $digest += ""
    $entriesForDay = $journalEntries | Where-Object { $_.MonthFile -eq $jf.Name -and $_.Date -eq $d }
    foreach ($e in $entriesForDay) {
      $tagText = ($e.Tags | ForEach-Object { "[$_]" }) -join ""
      $digest += "- $tagText $($e.Title)"
    }
    $digest += ""
  }

  Write-Utf8NoBom $digestPath ($digest -join "`n")
}

$ji = @()
$ji += "# Journal Index (generated)"
$ji += ""
$ji += "Generated: $gen"
$ji += ""
$ji += "Format: YYYY-MM-DD | [Tags] | Title | Files"
$ji += ""

foreach ($e in ($journalEntries | Sort-Object Date, Title)) {
  $tagText = ($e.Tags | ForEach-Object { "[$_]" }) -join ""
  $fileText = "-"
  if ($e.Files -and @($e.Files).Count -gt 0) { $fileText = ($e.Files -join ", ") }
  $ji += "$($e.Date) | $tagText | $($e.Title) | $fileText"
}

Write-Utf8NoBom (Join-Path $MemoryDir "journal-index.md") ($ji -join "`n")
$journalJson = if (-not $journalEntries -or $journalEntries.Count -eq 0) { "[]" } else { ConvertTo-Json -InputObject @($journalEntries.ToArray()) -Depth 6 }
Write-Utf8NoBom (Join-Path $MemoryDir "journal-index.json") $journalJson

# Optional: build SQLite index if Python exists
$python = Get-Command python -ErrorAction SilentlyContinue
if ($null -ne $python) {
  $pyScript = Join-Path $RepoRoot "scripts\memory\build-memory-sqlite.py"
  if (Test-Path $pyScript) {
    Write-Host "Python detected; building SQLite FTS index..." -ForegroundColor Cyan
    & $python.Source $pyScript --repo $RepoRoot | Out-Host
  }
} else {
  Write-Host "Python not found; skipping SQLite build." -ForegroundColor DarkYellow
}

# Token usage monitoring
$hotFiles = @(
  (Join-Path $MemoryDir "hot-rules.md"),
  (Join-Path $MemoryDir "active-context.md"),
  (Join-Path $MemoryDir "memo.md")
)
$totalChars = 0
foreach ($hf in $hotFiles) {
  if (Test-Path $hf) {
    $t = Get-Content -Raw -ErrorAction SilentlyContinue $hf
    if ($t) { $totalChars += $t.Length }
  }
}

$estimatedTokens = [math]::Round($totalChars / 4)

Write-Host ""
if ($totalChars -gt 8000) {
  Write-Host "WARNING: Always-read layer is $totalChars chars (~$estimatedTokens tokens)" -ForegroundColor Yellow
} else {
  Write-Host "Always-read layer: $totalChars chars (~$estimatedTokens tokens) - Healthy" -ForegroundColor Green
}

Write-Host ""
Write-Host "Rebuild complete." -ForegroundColor Green