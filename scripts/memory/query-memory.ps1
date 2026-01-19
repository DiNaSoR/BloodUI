<#
query-memory.ps1
Search memory quickly.

Default: grep (Select-String)
Optional: SQLite FTS (requires python + memory.sqlite), via -UseSqlite

Examples:
  powershell -File scripts/memory/query-memory.ps1 -Query "IL2CPP"
  powershell -File scripts/memory/query-memory.ps1 -Query "IL2CPP" -UseSqlite
  powershell -File scripts/memory/query-memory.ps1 -Query "bind" -Format AI
#>

[CmdletBinding()]
param(
  [Parameter(Mandatory=$true)][string]$Query,
  [ValidateSet("All","HotRules","Active","Memo","Lessons","Journal","Digests")][string]$Area = "All",
  [ValidateSet("Human","AI")][string]$Format = "Human",
  [switch]$UseSqlite
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
$SqlitePath = Join-Path $MemoryDir "memory.sqlite"

# If SQLite mode requested, use python helper if possible.
if ($UseSqlite) {
  $python = Get-Command python -ErrorAction SilentlyContinue
  $py = Join-Path $RepoRoot "scripts\memory\query-memory-sqlite.py"
  if ($null -ne $python -and (Test-Path $SqlitePath) -and (Test-Path $py)) {
    Write-Host "Using SQLite FTS search..." -ForegroundColor Cyan
    & $python.Source $py --repo $RepoRoot --q $Query --area $Area --format $Format
    exit $LASTEXITCODE
  }
  Write-Host "SQLite mode unavailable (need python + memory.sqlite + query-memory-sqlite.py). Falling back to grep." -ForegroundColor DarkYellow
}

Write-Host "Using file-based search..." -ForegroundColor Cyan

$targets = @()
switch ($Area) {
  "HotRules" { $targets += (Join-Path $MemoryDir "hot-rules.md") }
  "Active"   { $targets += (Join-Path $MemoryDir "active-context.md") }
  "Memo"     { $targets += (Join-Path $MemoryDir "memo.md") }
  "Lessons"  {
    $targets += (Join-Path $LessonsDir "index.md")
    $lessonFiles = Get-ChildItem -Path $LessonsDir -Filter "L-*.md" -ErrorAction SilentlyContinue
    foreach ($lf in $lessonFiles) { $targets += $lf.FullName }
  }
  "Journal"  { $targets += (Join-Path $MemoryDir "journal-index.md") }
  "Digests"  { $targets += (Join-Path $MemoryDir "digests\*.digest.md") }
  "All" {
    $targets += (Join-Path $MemoryDir "hot-rules.md")
    $targets += (Join-Path $MemoryDir "active-context.md")
    $targets += (Join-Path $MemoryDir "memo.md")
    $targets += (Join-Path $LessonsDir "index.md")
    $targets += (Join-Path $MemoryDir "journal-index.md")
    $targets += (Join-Path $MemoryDir "digests\*.digest.md")
  }
}

$allMatches = @()
foreach ($t in $targets) {
  $results = Select-String -Path $t -Pattern $Query -SimpleMatch -ErrorAction SilentlyContinue
  if ($results) { $allMatches += $results }
}

$matchCount = @($allMatches).Count

if ($Format -eq "AI") {
  if ($matchCount -eq 0) {
    Write-Host "No matches found for: $Query"
  } else {
    $uniqueFiles = $allMatches | ForEach-Object { $_.Path } | Sort-Object -Unique
    Write-Host "Files to read:"
    foreach ($f in $uniqueFiles) {
      $relative = $f.Replace($RepoRoot, "").TrimStart('\','/')
      Write-Host "  @$relative"
    }
  }
} else {
  Write-Host "Searching: $Query" -ForegroundColor Cyan
  Write-Host "Area: $Area" -ForegroundColor Cyan
  Write-Host ""
  if ($matchCount -eq 0) {
    Write-Host "No matches found." -ForegroundColor Yellow
  } else {
    $grouped = $allMatches | Group-Object Path
    foreach ($g in $grouped) {
      Write-Host "==> $($g.Name)" -ForegroundColor Green
      foreach ($m in $g.Group) {
        Write-Host "  $($m.LineNumber): $($m.Line.Trim())"
      }
      Write-Host ""
    }
  }
}