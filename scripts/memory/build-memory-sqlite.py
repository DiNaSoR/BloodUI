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