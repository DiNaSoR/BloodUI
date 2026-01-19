# Lessons (Atomic)

Each lesson is a separate file with strict YAML frontmatter (controlled schema).

Naming:
- `L-001-short-title.md`

Why:
- token efficiency (open only the one lesson you need)
- fast lookup via `lessons/index.md`
- easy pruning / superseding

Create a lesson:
- Run `scripts/memory/add-lesson.ps1 -Title "..." -Tags "..." -Rule "..."`
- Or copy `templates/lesson.template.md` -> `lessons/L-XXX-title.md`
- Then run `scripts/memory/rebuild-memory-index.ps1`