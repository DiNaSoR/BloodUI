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