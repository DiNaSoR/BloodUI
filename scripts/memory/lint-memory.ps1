<#
lint-memory.ps1
Validates memory health:
- lesson YAML frontmatter required fields
- unique lesson IDs
- tags must exist in tag-vocabulary.md
- journal date headings should not repeat within a month file
- token budget check
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

$mem = Join-Path $RepoRoot ".cursor\memory"
$lessonsDir = Join-Path $mem "lessons"
$journalDir = Join-Path $mem "journal"
$tagVocabPath = Join-Path $mem "tag-vocabulary.md"

$errors = @()
$warnings = @()

function Fail([string]$msg) {
  $script:errors += $msg
}

function Warn([string]$msg) {
  $script:warnings += $msg
}

function ReadText([string]$p) {
  $t = Get-Content -Raw -Encoding UTF8 -ErrorAction Stop $p
  if ($t.Length -gt 0 -and [int]$t[0] -eq 0xFEFF) { $t = $t.Substring(1) } # strip BOM
  return $t
}

Write-Host "Linting Cursor Memory System..." -ForegroundColor Cyan
Write-Host ""

# Load allowed tags from tag-vocabulary.md
$allowed = @{}
if (Test-Path $tagVocabPath) {
  $tv = ReadText $tagVocabPath
  foreach ($m in [regex]::Matches($tv, '(?m)^\-\s+\[([^\]]+)\]')) {
    $allowed[$m.Groups[1].Value.Trim()] = $true
  }
} else {
  Warn "Missing tag vocabulary: $tagVocabPath"
}

Write-Host "Checking lessons..." -ForegroundColor White

# Minimal YAML frontmatter parse
function ParseFrontmatter([string]$file) {
  $raw = ReadText $file
  $lines = $raw -split "`r?`n"
  if ($lines.Count -lt 3 -or $lines[0].Trim() -ne "---") { return $null }

  $result = @{}
  $i = 1
  $currentListKey = $null
  while ($i -lt $lines.Count) {
    $line = $lines[$i]
    if ($line.Trim() -eq "---") { break }
    if ([string]::IsNullOrWhiteSpace($line) -or $line.TrimStart().StartsWith("#")) { $i++; continue }

    if ($line -match '^\s*-\s+(.+)$' -and $currentListKey) {
      $result[$currentListKey] += @($Matches[1].Trim())
      $i++; continue
    }

    if ($line -match '^\s*([A-Za-z0-9_]+)\s*:\s*(.*)\s*$') {
      $key = $Matches[1].Trim().ToLower()
      $val = $Matches[2]
      if ([string]::IsNullOrWhiteSpace($val)) {
        $result[$key] = @()
        $currentListKey = $key
        $i++; continue
      }
      $currentListKey = $null
      $v = $val.Trim()
      if (($v.StartsWith('"') -and $v.EndsWith('"')) -or ($v.StartsWith("'") -and $v.EndsWith("'"))) {
        $v = $v.Substring(1, $v.Length-2)
      }
      if ($v -match '^\[(.*)\]$') {
        $inner = $Matches[1]
        $items = @()
        if (-not [string]::IsNullOrWhiteSpace($inner)) {
          $items = $inner -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
        }
        $result[$key] = @($items)
      } else {
        $result[$key] = $v
      }
      $i++; continue
    }

    $i++
  }

  return $result
}

# Validate lessons
$ids = @{}
$lessonFiles = Get-ChildItem -Path $lessonsDir -File -Filter "L-*.md" -ErrorAction SilentlyContinue
$lessonCount = if ($lessonFiles) { @($lessonFiles).Count } else { 0 }

foreach ($lf in $lessonFiles) {
  $yaml = ParseFrontmatter $lf.FullName
  if ($null -eq $yaml) {
    Fail "[$($lf.Name)] Missing YAML frontmatter"
    continue
  }

  foreach ($req in @("id","title","status","tags","introduced","rule")) {
    if (-not $yaml.ContainsKey($req) -or [string]::IsNullOrWhiteSpace([string]$yaml[$req])) {
      Fail "[$($lf.Name)] Missing required field: $req"
    }
  }

  $id = [string]$yaml["id"]
  if ($id -notmatch '^L-\d{3}$') { Warn "[$($lf.Name)] ID '$id' doesn't match format L-XXX (3 digits)" }

  if ($ids.ContainsKey($id)) { Fail "[$($lf.Name)] Duplicate lesson ID $id (also in $($ids[$id]))" }
  else { $ids[$id] = $lf.Name }

  # tags must be allowed
  $tags = @($yaml["tags"])
  foreach ($t in $tags) {
    $tag = [string]$t
    if ($allowed.Count -gt 0 -and -not $allowed.ContainsKey($tag)) {
      Fail "[$($lf.Name)] Unknown tag [$tag]. Add it to tag-vocabulary.md or fix the lesson."
    }
  }

  # Check filename matches ID
  $expectedPrefix = $id.ToLower()
  if (-not $lf.Name.ToLower().StartsWith($expectedPrefix)) {
    Warn "[$($lf.Name)] Filename doesn't start with ID '$id'"
  }
}

Write-Host "  Found $lessonCount lesson files" -ForegroundColor Gray

# Validate journal: duplicate date headings per month file
Write-Host "Checking journals..." -ForegroundColor White

$journalFiles = Get-ChildItem -Path $journalDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -match '^\d{4}-\d{2}\.md$' }
$journalCount = if ($journalFiles) { @($journalFiles).Count } else { 0 }

foreach ($jf in $journalFiles) {
  $txt = ReadText $jf.FullName
  $dates = @([regex]::Matches($txt, '(?m)^##\s+(\d{4}-\d{2}-\d{2})') | ForEach-Object { $_.Groups[1].Value })
  if ($dates -and $dates.Count -gt 0) {
    $g = $dates | Group-Object | Where-Object { $_.Count -gt 1 }
    foreach ($dup in $g) {
      Fail "[$($jf.Name)] Duplicate date heading $($dup.Name) x$($dup.Count). Merge into one section."
    }
  }
}

Write-Host "  Found $journalCount journal files" -ForegroundColor Gray

# Token budget check
Write-Host "Checking token budget..." -ForegroundColor White

$hotFiles = @(
  (Join-Path $mem "hot-rules.md"),
  (Join-Path $mem "active-context.md"),
  (Join-Path $mem "memo.md")
)

$totalChars = 0
foreach ($hf in $hotFiles) {
  if (Test-Path $hf) {
    $chars = (Get-Content $hf -Raw -ErrorAction SilentlyContinue).Length
    $totalChars += $chars
    if ($chars -gt 3000) {
      Warn "[$(Split-Path $hf -Leaf)] File is $chars chars (~$([math]::Round($chars/4)) tokens) - consider trimming"
    }
  }
}

$estimatedTokens = [math]::Round($totalChars / 4)
Write-Host "  Always-read layer: $totalChars chars (~$estimatedTokens tokens)" -ForegroundColor Gray

if ($totalChars -gt 8000) {
  Fail "[Token Budget] Always-read layer exceeds 8000 chars (~2000 tokens)"
} elseif ($totalChars -gt 6000) {
  Warn "[Token Budget] Always-read layer is $totalChars chars - approaching limit"
}

# Check for missing indexes
Write-Host "Checking for orphans..." -ForegroundColor White

if (-not (Test-Path (Join-Path $lessonsDir "index.md"))) {
  Warn "[lessons/index.md] Missing - run rebuild-memory-index.ps1"
}
if (-not (Test-Path (Join-Path $mem "journal-index.md"))) {
  Warn "[journal-index.md] Missing - run rebuild-memory-index.ps1"
}

# Report
Write-Host ""
Write-Host "====== LINT RESULTS ======" -ForegroundColor White

$errorCount = @($errors).Count
$warningCount = @($warnings).Count

if ($errorCount -eq 0 -and $warningCount -eq 0) {
  Write-Host "All checks passed!" -ForegroundColor Green
} else {
  if ($errorCount -gt 0) {
    Write-Host ""
    Write-Host "ERRORS ($errorCount):" -ForegroundColor Red
    foreach ($e in $errors) {
      Write-Host "  ERROR: $e" -ForegroundColor Red
    }
  }
  if ($warningCount -gt 0) {
    Write-Host ""
    Write-Host "WARNINGS ($warningCount):" -ForegroundColor Yellow
    foreach ($w in $warnings) {
      Write-Host "  WARN: $w" -ForegroundColor Yellow
    }
  }
}

Write-Host ""

if ($errorCount -gt 0) {
  Write-Host "Lint FAILED with $errorCount error(s)" -ForegroundColor Red
  exit 1
} else {
  Write-Host "Lint passed" -ForegroundColor Green
  exit 0
}