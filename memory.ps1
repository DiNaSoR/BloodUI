<#
setup-cursor-memory.ps1 (Memory v3.2.2)
Windows-first, token-safe, scalable repo memory for Cursor (or any AI agent).

Merged from v3 (our helpers) + GPT v3.1 (BOM handling, tag validation, portable hooks):
- Curated "always read" memory: hot-rules.md + active-context.md + memo.md
- Atomic lessons (individual files) with strict YAML frontmatter
- Monthly journal + auto-generated digest + journal index
- Cursor rule (.mdc) to enforce behavior
- Helper scripts: rebuild, query (SQLite+grep), lint, add-lesson, add-journal-entry
- Tag validation against tag-vocabulary.md
- BOM-tolerant parsing
- Portable hooks via .githooks/ + .git/hooks/
- Lint runs on pre-commit

USAGE (from repo root):
  powershell -ExecutionPolicy Bypass -File .\memory.ps1
  powershell -ExecutionPolicy Bypass -File .\memory.ps1 -ProjectName "MyProject"
  powershell -ExecutionPolicy Bypass -File .\memory.ps1 -Force

After setup, run:
  git config core.hooksPath .githooks
#>

[CmdletBinding()]
param(
  [string]$RepoRoot = (Get-Location).Path,
  [string]$ProjectName = "",
  [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Dir {
  param([Parameter(Mandatory=$true)][string]$Path)
  if (!(Test-Path $Path)) {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    Write-Host "DIR: $Path" -ForegroundColor Green
  }
}

function Write-TextFile {
  param(
    [Parameter(Mandatory=$true)][string]$Path,
    [Parameter(Mandatory=$true)][string]$Content,
    [ValidateSet("CRLF","LF")][string]$LineEndings = "CRLF",
    [switch]$ForceWrite
  )

  $dir = Split-Path -Parent $Path
  if ($dir -and !(Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }

  if ((Test-Path $Path) -and (-not $ForceWrite)) {
    Write-Host "SKIP (exists): $Path" -ForegroundColor DarkYellow
    return
  }

  $normalized = $Content
  if ($LineEndings -eq "CRLF") {
    $normalized = ($normalized -replace "`r?`n", "`r`n")
  } else {
    $normalized = ($normalized -replace "`r?`n", "`n")
  }

  $enc = New-Object System.Text.UTF8Encoding($false)
  [System.IO.File]::WriteAllText($Path, $normalized, $enc)

  Write-Host "WROTE: $Path" -ForegroundColor Green
}

$RepoRoot = (Resolve-Path $RepoRoot).Path
if ([string]::IsNullOrWhiteSpace($ProjectName)) {
  $ProjectName = Split-Path -Leaf $RepoRoot
}

# Paths
$CursorDir    = Join-Path $RepoRoot ".cursor"
$MemoryDir    = Join-Path $CursorDir "memory"
$RulesDir     = Join-Path $CursorDir "rules"
$JournalDir   = Join-Path $MemoryDir "journal"
$DigestsDir   = Join-Path $MemoryDir "digests"
$AdrDir       = Join-Path $MemoryDir "adr"
$LessonsDir   = Join-Path $MemoryDir "lessons"
$TemplatesDir = Join-Path $MemoryDir "templates"
$ScriptsDir   = Join-Path $RepoRoot "scripts"
$MemScripts   = Join-Path $ScriptsDir "memory"
$GitDir       = Join-Path $RepoRoot ".git"
$GitHooksDir  = Join-Path $GitDir "hooks"
$GithooksDir  = Join-Path $RepoRoot ".githooks"

Ensure-Dir $CursorDir
Ensure-Dir $MemoryDir
Ensure-Dir $RulesDir
Ensure-Dir $JournalDir
Ensure-Dir $DigestsDir
Ensure-Dir $AdrDir
Ensure-Dir $LessonsDir
Ensure-Dir $TemplatesDir
Ensure-Dir $ScriptsDir
Ensure-Dir $MemScripts
Ensure-Dir $GithooksDir

$month = (Get-Date -Format "yyyy-MM")
$today = (Get-Date -Format "yyyy-MM-dd")

# -------------------------
# Memory files
# -------------------------

$indexMd = @"
# Memory Index

Entry point for repo memory.

## Read order (token-safe)

ALWAYS READ (in order):
1) ``hot-rules.md`` (tiny invariants, <20 lines)
2) ``active-context.md`` (this session only)
3) ``memo.md`` (long-term current truth + ownership)

SEARCH FIRST, THEN OPEN ONLY WHAT MATCHES:
4) ``lessons/index.md`` -> find lesson ID(s)
5) ``lessons/L-XXX-*.md`` -> open only specific lesson(s)
6) ``digests/YYYY-MM.digest.md`` -> before raw journal
7) ``journal/YYYY-MM.md`` -> only for archaeology

## Files

- Hot rules: ``hot-rules.md``
- Active context: ``active-context.md``
- Memo: ``memo.md``
- Lessons: ``lessons/``
- Lesson index (generated): ``lessons/index.md`` + ``lessons-index.json``
- Journal monthly: ``journal/YYYY-MM.md``
- Journal index (generated): ``journal-index.md`` + ``journal-index.json``
- Digests (generated): ``digests/YYYY-MM.digest.md``
- Tag vocabulary: ``tag-vocabulary.md``
- Regression checklist: ``regression-checklist.md``
- ADRs: ``adr/``

## Maintenance commands

Helper scripts:
- Add lesson: ``scripts/memory/add-lesson.ps1 -Title "..." -Tags "..." -Rule "..."``
- Add journal: ``scripts/memory/add-journal-entry.ps1 -Tags "..." -Title "..."``
- Rebuild indexes: ``scripts/memory/rebuild-memory-index.ps1``
- Lint: ``scripts/memory/lint-memory.ps1``
- Query (grep): ``scripts/memory/query-memory.ps1 -Query "..."``
- Query (SQLite): ``scripts/memory/query-memory.ps1 -Query "..." -UseSqlite``
- Clear session: ``scripts/memory/clear-active.ps1``
"@

$hotRules = @"
# Hot Rules (MUST READ)

Keep this file under ~20 lines. If it grows, move content into memo or lessons.

## Authority Order (highest to lowest)
1) Lessons override EVERYTHING (including active-context)
2) active-context.md overrides memo/journal (but NOT lessons)
3) memo.md is long-term project truth
4) journal is history

## Retrieval Rules
5) Do NOT scan raw journals. Use indexes/digests first.
6) Reuse existing patterns. Check memo.md ownership before creating new systems.
7) When done: clear active-context.md, add journal entry if significant.
"@

$activeContext = @"
# Active Context (Session Scratchpad)

Priority: this overrides older journal history *for this session only*.

CLEAR this file when the task is done:
- Run ``scripts/memory/clear-active.ps1``

## Current Goal
-

## Files in Focus
-

## Findings / Decisions
-

## Temporary Constraints
-

## Blockers
-
"@

$memo = @"
# Project Memo - $ProjectName

Last updated: $today

## Ownership map (fill early)

- UI / Frontend owner: <path/module>
- Backend / Server owner: <path/module>
- Data parsing / protocol owner: <path/module>
- Build/CI owner: <path/module>

## Current truth (high-signal)

- <invariants that must stay true>
- <important defaults/toggles>
- <timing/lifecycle rules>
- <anything that prevents regressions>

## Open questions / TODO
- <unknowns / risks>
"@

$lessonsReadme = @"
# Lessons (Atomic)

Each lesson is a separate file with strict YAML frontmatter (controlled schema).

Naming:
- ``L-001-short-title.md``

Why:
- token efficiency (open only the one lesson you need)
- fast lookup via ``lessons/index.md``
- easy pruning / superseding

Create a lesson:
- Run ``scripts/memory/add-lesson.ps1 -Title "..." -Tags "..." -Rule "..."``
- Or copy ``templates/lesson.template.md`` -> ``lessons/L-XXX-title.md``
- Then run ``scripts/memory/rebuild-memory-index.ps1``
"@

$lessonsIndex = @"
# Lessons Index (generated)

Generated by ``scripts/memory/rebuild-memory-index.ps1``.

Format: ID | [Tags] | AppliesTo | Rule | File

(No lessons yet.)
"@

$journalReadme = @"
# Journal

Monthly file: ``YYYY-MM.md``

Rules:
- Each date appears ONCE per file: ``## YYYY-MM-DD``
- Put multiple entries under that header as bullets.
- Keep it high-signal: what changed, why, key files.
- Put long narratives in Docs/WorkLogs and link them.

Add entries via:
- ``scripts/memory/add-journal-entry.ps1 -Tags "UI,Fix" -Title "..."``
"@

$journalMonth = @"
# Development Journal - $ProjectName ($month)

## $today

- [Process] Initialized memory system (Memory v3.2.2)
  - Why: token-safe AI memory + indexed retrieval + portable hooks
  - Key files:
    - ``.cursor/memory/*``
    - ``.cursor/rules/00-memory-system.mdc``
    - ``scripts/memory/*``
"@

$digestsReadme = @"
# Digests

Generated summaries of journal months.
AI should read digests before raw journal.

Generated by:
- ``scripts/memory/rebuild-memory-index.ps1``
"@

$adrReadme = @"
# ADRs

Architecture Decision Records: why we did it this way.

Naming:
- ``ADR-001-short-title.md``

Format:
- Context
- Decision
- Consequences
"@

$tagVocab = @"
# Tag Vocabulary (fixed set)

Use a small vocabulary so retrieval stays reliable.
Linter validates tags against this list.

- [UI] - UI behavior, rendering, interaction
- [Layout] - layout groups, anchors, sizing, rects
- [Input] - mouse/keyboard/controller input rules
- [Data] - parsing, payloads, formats, state sync
- [Server] - server-side logic and lifecycle
- [Init] - initialization / load order / startup
- [Build] - compilation, MSBuild, project files
- [CI] - automation, pipelines
- [Release] - packaging, artifacts, uploads
- [Compat] - IL2CPP, runtime constraints, environment quirks
- [Integration] - optional plugins, reflection bridges, external systems
- [Docs] - documentation and changelog work
- [Architecture] - module boundaries, refactors, ownership
- [DX] - developer experience, tooling, maintainability
- [Reliability] - crash prevention, guardrails, self-healing
- [Process] - workflow, memory system, tooling changes

# Common "type" tags (used by templates/examples)
- [Fix] - bug fixes, regressions, patches
- [Feature] - new behavior/capability
- [Refactor] - restructuring without behavior changes
"@

$regChecklist = @"
# Regression Checklist

Run only what is relevant.

## Build
- [ ] Build solution / affected projects
- [ ] No new warnings (or documented)

## Runtime (if applicable)
- [ ] Core UI renders
- [ ] Core interactions work
- [ ] No obvious errors/log spam

## Data (if applicable)
- [ ] Parsing works on known payloads
- [ ] State updates do not regress

## Docs (if applicable)
- [ ] Journal updated
- [ ] Memo updated (if truth changed)
- [ ] Lesson added (if pitfall discovered)
"@

Write-TextFile (Join-Path $MemoryDir "index.md") $indexMd -ForceWrite:$Force
Write-TextFile (Join-Path $MemoryDir "hot-rules.md") $hotRules -ForceWrite:$Force
Write-TextFile (Join-Path $MemoryDir "active-context.md") $activeContext -ForceWrite:$Force
Write-TextFile (Join-Path $MemoryDir "memo.md") $memo -ForceWrite:$Force
Write-TextFile (Join-Path $LessonsDir "README.md") $lessonsReadme -ForceWrite:$Force
Write-TextFile (Join-Path $LessonsDir "index.md") $lessonsIndex -ForceWrite:$Force
Write-TextFile (Join-Path $JournalDir "README.md") $journalReadme -ForceWrite:$Force
Write-TextFile (Join-Path $JournalDir "$month.md") $journalMonth -ForceWrite:$Force
Write-TextFile (Join-Path $DigestsDir "README.md") $digestsReadme -ForceWrite:$Force
Write-TextFile (Join-Path $AdrDir "README.md") $adrReadme -ForceWrite:$Force
Write-TextFile (Join-Path $MemoryDir "tag-vocabulary.md") $tagVocab -ForceWrite:$Force
Write-TextFile (Join-Path $MemoryDir "regression-checklist.md") $regChecklist -ForceWrite:$Force

# -------------------------
# Templates
# -------------------------

$templateLesson = @"
---
id: L-XXX
title: Short descriptive title
status: Active
tags: [UI, Reliability]
introduced: YYYY-MM-DD
applies_to:
  - path/or/glob/**
triggers:
  - error keyword
  - crash signature
rule: One sentence. Imperative. Testable.
supersedes: ""
---

# L-XXX - Short descriptive title

## Symptom
What broke / what was observed.

## Root cause
The real reason.

## Wrong approach (DO NOT REPEAT)
- What not to do
- Why it fails

## Correct approach
- What to do instead

## References
- Files: ``path/to/file``
- Journal: ``journal/YYYY-MM.md#YYYY-MM-DD``
"@

$templateJournal = @"
# Journal Entry Template (paste under an existing date header)

- [Area][Type] Title
  - Why: ...
  - Key files:
    - ``path/to/file``
  - Notes: <optional>
  - Verification: Build PASS/FAIL/NOT RUN; Runtime PASS/FAIL/NOT RUN
  - Related: Lesson L-XXX; ADR ADR-XXX
"@

$templateAdr = @"
# ADR-XXX - Title

Date: YYYY-MM-DD
Status: Proposed | Accepted | Deprecated

## Context
What problem are we solving?

## Decision
What did we choose?

## Consequences
Tradeoffs, risks, follow-ups.
"@

Write-TextFile (Join-Path $TemplatesDir "lesson.template.md") $templateLesson -ForceWrite:$Force
Write-TextFile (Join-Path $TemplatesDir "journal-entry.template.md") $templateJournal -ForceWrite:$Force
Write-TextFile (Join-Path $TemplatesDir "adr.template.md") $templateAdr -ForceWrite:$Force

# -------------------------
# Cursor rule: enforce memory usage
# -------------------------

$memoryRule = @"
---
description: Memory System v3.2.2 - Authority + Atomic Retrieval + Token Safety
globs:
  - "**/*"
alwaysApply: true
---

# Memory System (MANDATORY)

## Authority Order (highest to lowest)
1) Lessons override EVERYTHING (including active-context)
2) ``active-context.md`` overrides memo/journal (but NOT lessons)
3) ``memo.md`` is long-term project truth
4) Journal is history
5) Existing codebase
6) New suggestions (lowest priority)

## Token-Safe Retrieval

ALWAYS READ (in order):
1. ``.cursor/memory/hot-rules.md`` (tiny, <20 lines)
2. ``.cursor/memory/active-context.md`` (current session state)
3. ``.cursor/memory/memo.md`` (project truth + ownership)

SEARCH FIRST, THEN FETCH:
4. ``.cursor/memory/lessons/index.md`` -> find relevant lesson ID
5. ``.cursor/memory/lessons/L-XXX-title.md`` -> load ONLY the specific file
6. ``.cursor/memory/digests/YYYY-MM.digest.md`` -> before raw journal
7. ``.cursor/memory/journal/YYYY-MM.md`` -> only for archaeology

## After Any Feature/Fix

1. Update ``active-context.md`` during work (scratchpad)
2. Add journal entry to ``journal/YYYY-MM.md`` when done
3. Create ``lessons/L-XXX-title.md`` if you discovered a pitfall
4. Update ``memo.md`` if project truth changed
5. Clear ``active-context.md`` when task is merged

## Helper Scripts

- Add lesson: ``scripts/memory/add-lesson.ps1 -Title "..." -Tags "..." -Rule "..."``
- Add journal: ``scripts/memory/add-journal-entry.ps1 -Tags "..." -Title "..."``
- Rebuild: ``scripts/memory/rebuild-memory-index.ps1``
- Lint: ``scripts/memory/lint-memory.ps1``
- Query: ``scripts/memory/query-memory.ps1 -Query "..." [-UseSqlite]``
- Clear: ``scripts/memory/clear-active.ps1``

## AI Behavior

- When user says "I'm done" or "merge this" -> remind to clear active-context
- When you discover a bug pattern -> suggest creating a lesson
- When unsure about architecture -> check lessons/index.md first
- Don't create parallel systems -> check memo.md ownership map
"@

Write-TextFile (Join-Path $RulesDir "00-memory-system.mdc") $memoryRule -ForceWrite:$Force

# -------------------------
# Helper scripts
# -------------------------

$rebuildIndex = @'
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
'@

$linter = @'
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
'@

$queryScript = @'
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
'@

$buildSqlitePy = @'
#!/usr/bin/env python3
"""Build SQLite FTS5 index from memory JSON indexes."""
import argparse
import json
import sqlite3
from pathlib import Path

def read_text(p: Path) -> str:
    return p.read_text(encoding="utf-8-sig", errors="replace")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    args = ap.parse_args()

    repo = Path(args.repo)
    mem = repo / ".cursor" / "memory"
    out_db = mem / "memory.sqlite"

    lessons_index = mem / "lessons-index.json"
    journal_index = mem / "journal-index.json"

    lessons = []
    if lessons_index.exists():
        t = read_text(lessons_index).strip()
        if t:
            lessons = json.loads(t)
            if not isinstance(lessons, list):
                lessons = [lessons] if lessons else []

    journal = []
    if journal_index.exists():
        t = read_text(journal_index).strip()
        if t:
            journal = json.loads(t)
            if not isinstance(journal, list):
                journal = [journal] if journal else []

    if out_db.exists():
        out_db.unlink()

    con = sqlite3.connect(str(out_db))
    cur = con.cursor()
    cur.execute("CREATE VIRTUAL TABLE memory_fts USING fts5(kind, id, date, tags, title, content, path);")

    # Always-read docs
    for kind, fid, path in [
        ("hot_rules", "HOT", mem / "hot-rules.md"),
        ("active", "ACTIVE", mem / "active-context.md"),
        ("memo", "MEMO", mem / "memo.md"),
    ]:
        if path.exists():
            cur.execute(
                "INSERT INTO memory_fts(kind,id,date,tags,title,content,path) VALUES (?,?,?,?,?,?,?)",
                (kind, fid, None, "", path.name, read_text(path), str(path)),
            )

    # Lessons: insert full file content if possible
    lessons_dir = mem / "lessons"
    for l in lessons:
        lid = l.get("Id")
        title = l.get("Title","")
        tags = " ".join(l.get("Tags") or [])
        date = l.get("Introduced")
        file = l.get("File","")
        path = lessons_dir / file if file else (mem / "lessons.md")
        content = read_text(path) if path.exists() else f"{title}\nRule: {l.get('Rule','')}"
        cur.execute(
            "INSERT INTO memory_fts(kind,id,date,tags,title,content,path) VALUES (?,?,?,?,?,?,?)",
            ("lesson", lid, date, tags, title, content, str(path)),
        )

    # Journal index lines
    for e in journal:
        tags = " ".join(e.get("Tags") or [])
        files = e.get("Files") or []
        if isinstance(files, dict):
            files = []
        content = f"{e.get('Title','')}\nFiles: {', '.join(files)}"
        path = mem / "journal" / (e.get("MonthFile") or "")
        cur.execute(
            "INSERT INTO memory_fts(kind,id,date,tags,title,content,path) VALUES (?,?,?,?,?,?,?)",
            ("journal", None, e.get("Date"), tags, e.get("Title"), content, str(path)),
        )

    # Digests
    digests = mem / "digests"
    if digests.exists():
        for p in digests.glob("*.digest.md"):
            cur.execute(
                "INSERT INTO memory_fts(kind,id,date,tags,title,content,path) VALUES (?,?,?,?,?,?,?)",
                ("digest", None, None, "", p.name, read_text(p), str(p)),
            )

    con.commit()
    con.close()
    print(f"Built: {out_db}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
'@

$querySqlitePy = @'
#!/usr/bin/env python3
"""Query memory SQLite FTS index."""
import argparse
import sqlite3
from pathlib import Path

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--repo", required=True)
    ap.add_argument("--q", required=True)
    ap.add_argument("--area", default="All")
    ap.add_argument("--format", default="Human")
    args = ap.parse_args()

    repo = Path(args.repo)
    db = repo / ".cursor" / "memory" / "memory.sqlite"
    if not db.exists():
        print("SQLite DB not found. Run rebuild-memory-index.ps1 first.")
        return 2

    area = args.area.lower()
    kind_filter = None
    if area == "hotrules": kind_filter = "hot_rules"
    elif area == "active": kind_filter = "active"
    elif area == "memo": kind_filter = "memo"
    elif area == "lessons": kind_filter = "lesson"
    elif area == "journal": kind_filter = "journal"
    elif area == "digests": kind_filter = "digest"

    con = sqlite3.connect(str(db))
    cur = con.cursor()

    sql = "SELECT kind, id, date, title, path, snippet(memory_fts, 5, '[', ']', '...', 12) FROM memory_fts WHERE memory_fts MATCH ?"
    params = [args.q]
    if kind_filter:
        sql += " AND kind = ?"
        params.append(kind_filter)
    sql += " LIMIT 20"

    rows = cur.execute(sql, params).fetchall()
    con.close()

    if args.format.lower() == "ai":
        paths = []
        for r in rows:
            p = r[4]
            try:
                rel = str(Path(p).resolve().relative_to(repo.resolve()))
            except Exception:
                rel = p
            paths.append(rel.replace("\\","/"))
        uniq = []
        for p in paths:
            if p not in uniq:
                uniq.append(p)
        if not uniq:
            print(f"No matches for: {args.q}")
        else:
            print("Files to read:")
            for p in uniq:
                print(f"  @{p}")
        return 0

    if not rows:
        print(f"No matches for: {args.q}")
        return 0

    for kind, idv, date, title, path, snip in rows:
        print(f"==> {kind} | {idv or '-'} | {date or '-'} | {title}")
        print(f"    {path}")
        print(f"    {snip}")
        print("")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
'@

$clearActive = @'
<#
clear-active.ps1
Resets active-context.md to blank template.
#>

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

if ($PSScriptRoot) {
  $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
} else {
  $RepoRoot = (Get-Location).Path
}

$ActivePath = Join-Path $RepoRoot ".cursor\memory\active-context.md"

$Template = @"
# Active Context (Session Scratchpad)

Priority: this overrides older journal history *for this session only*.

CLEAR this file when the task is done:
- Run ``scripts/memory/clear-active.ps1``

## Current Goal
-

## Files in Focus
-

## Findings / Decisions
-

## Temporary Constraints
-

## Blockers
-
"@

$enc = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllText($ActivePath, ($Template -replace "`r?`n", "`r`n"), $enc)

Write-Host "Cleared: $ActivePath" -ForegroundColor Green
'@

$addLesson = @'
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
'@

$addJournalEntry = @'
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
'@

Write-TextFile (Join-Path $MemScripts "rebuild-memory-index.ps1") $rebuildIndex -ForceWrite:$Force
Write-TextFile (Join-Path $MemScripts "lint-memory.ps1") $linter -ForceWrite:$Force
Write-TextFile (Join-Path $MemScripts "query-memory.ps1") $queryScript -ForceWrite:$Force
Write-TextFile (Join-Path $MemScripts "build-memory-sqlite.py") $buildSqlitePy -ForceWrite:$Force -LineEndings "LF"
Write-TextFile (Join-Path $MemScripts "query-memory-sqlite.py") $querySqlitePy -ForceWrite:$Force -LineEndings "LF"
Write-TextFile (Join-Path $MemScripts "clear-active.ps1") $clearActive -ForceWrite:$Force
Write-TextFile (Join-Path $MemScripts "add-lesson.ps1") $addLesson -ForceWrite:$Force
Write-TextFile (Join-Path $MemScripts "add-journal-entry.ps1") $addJournalEntry -ForceWrite:$Force

# -------------------------
# Git hooks (portable + immediate)
# -------------------------

$hookBody = @'
#!/bin/sh
# Cursor Memory: auto-rebuild indexes + lint before commit

set -e

ROOT="$(git rev-parse --show-toplevel 2>/dev/null || pwd)"
cd "$ROOT"

echo "[CursorMemory] Rebuilding indexes..."
if command -v powershell.exe >/dev/null 2>&1; then
  powershell.exe -ExecutionPolicy Bypass -File "./scripts/memory/rebuild-memory-index.ps1"
  powershell.exe -ExecutionPolicy Bypass -File "./scripts/memory/lint-memory.ps1"
elif command -v pwsh >/dev/null 2>&1; then
  pwsh -ExecutionPolicy Bypass -File "./scripts/memory/rebuild-memory-index.ps1"
  pwsh -ExecutionPolicy Bypass -File "./scripts/memory/lint-memory.ps1"
else
  echo "[CursorMemory] PowerShell not found; skipping memory rebuild/lint."
fi

git add .cursor/memory/lessons/index.md 2>/dev/null || true
git add .cursor/memory/lessons-index.json 2>/dev/null || true
git add .cursor/memory/journal-index.md 2>/dev/null || true
git add .cursor/memory/journal-index.json 2>/dev/null || true
git add .cursor/memory/digests/*.digest.md 2>/dev/null || true

exit 0
'@

# Write .githooks/pre-commit (portable path)
$githookPath = Join-Path $GithooksDir "pre-commit"
Write-TextFile $githookPath $hookBody -ForceWrite:$Force -LineEndings "LF"

# Also write .git/hooks/pre-commit for immediate effect
if (Test-Path $GitHooksDir) {
  $legacyHookPath = Join-Path $GitHooksDir "pre-commit"
  if ((Test-Path $legacyHookPath) -and (-not $Force)) {
    $existing = Get-Content -Raw -ErrorAction SilentlyContinue $legacyHookPath
    if ($existing -match "Cursor Memory: auto-rebuild") {
      Write-Host "SKIP (exists): $legacyHookPath" -ForegroundColor DarkYellow
    } else {
      # append safely
      $combined = ($existing.TrimEnd() + "`n`n" + $hookBody)
      Write-TextFile $legacyHookPath $combined -ForceWrite:$true -LineEndings "LF"
    }
  } else {
    Write-TextFile $legacyHookPath $hookBody -ForceWrite:$Force -LineEndings "LF"
  }
}

# -------------------------
# Token budget check
# -------------------------

$alwaysRead = @(
  (Join-Path $MemoryDir "hot-rules.md"),
  (Join-Path $MemoryDir "active-context.md"),
  (Join-Path $MemoryDir "memo.md")
)

$totalChars = 0
foreach ($p in $alwaysRead) {
  if (Test-Path $p) {
    $t = Get-Content -Raw -ErrorAction SilentlyContinue $p
    if ($t) { $totalChars += $t.Length }
  }
}
$estimatedTokens = [math]::Round($totalChars / 4)

Write-Host ""
if ($totalChars -gt 8000) {
  Write-Host "WARNING: Always-read layer is $totalChars chars (~$estimatedTokens tokens)." -ForegroundColor Yellow
} else {
  Write-Host "Always-read layer: $totalChars chars (~$estimatedTokens tokens) - Healthy" -ForegroundColor Green
}

# Auto-add memory.sqlite to .gitignore (BOM-safe, preserves existing line endings)
$giPath = Join-Path $RepoRoot ".gitignore"
$sqliteLine = ".cursor/memory/memory.sqlite"
$giHeader = "# Cursor Memory System (generated)"
$block = "$giHeader`n$sqliteLine"

$giLineEndings = "CRLF"
$giContent = ""

if (Test-Path $giPath) {
  $giContent = Get-Content -Raw -Encoding UTF8 -ErrorAction SilentlyContinue $giPath
  if ($giContent.Length -gt 0 -and [int]$giContent[0] -eq 0xFEFF) { $giContent = $giContent.Substring(1) }
  $giLineEndings = if ($giContent -match "`r`n") { "CRLF" } else { "LF" }

  if ($giContent -notmatch [regex]::Escape($sqliteLine)) {
    $trimmed = $giContent.TrimEnd("`r","`n")
    $newContent = $trimmed + "`n`n" + $block.Trim() + "`n"
    Write-TextFile $giPath $newContent -ForceWrite:$true -LineEndings $giLineEndings
    Write-Host "Updated .gitignore: $sqliteLine" -ForegroundColor Green
  } else {
    Write-Host "SKIP (.gitignore already contains): $sqliteLine" -ForegroundColor DarkYellow
  }
} else {
  $newContent = $block.Trim() + "`n"
  Write-TextFile $giPath $newContent -ForceWrite:$true -LineEndings "CRLF"
  Write-Host "Created .gitignore with: $sqliteLine" -ForegroundColor Green
}

Write-Host ""
Write-Host "Setup complete. (Memory System v3.2.2)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1) Run: powershell -ExecutionPolicy Bypass -File scripts/memory/rebuild-memory-index.ps1" -ForegroundColor White
Write-Host "  2) Run: powershell -ExecutionPolicy Bypass -File scripts/memory/lint-memory.ps1" -ForegroundColor White
Write-Host "  3) Enable portable hooks: git config core.hooksPath .githooks" -ForegroundColor White
Write-Host ""
Write-Host "Helper scripts:" -ForegroundColor Cyan
Write-Host "  Add lesson:  scripts\memory\add-lesson.ps1 -Title ""..."" -Tags ""..."" -Rule ""...""" -ForegroundColor DarkGray
Write-Host "  Add journal: scripts\memory\add-journal-entry.ps1 -Tags ""..."" -Title ""...""" -ForegroundColor DarkGray
Write-Host "  Query:       scripts\memory\query-memory.ps1 -Query ""..."" [-UseSqlite]" -ForegroundColor DarkGray
Write-Host "  Lint:        scripts\memory\lint-memory.ps1" -ForegroundColor DarkGray
Write-Host "  Clear:       scripts\memory\clear-active.ps1" -ForegroundColor DarkGray
Write-Host ""
