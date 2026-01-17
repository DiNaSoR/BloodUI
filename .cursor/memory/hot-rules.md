# Hot Rules (read this first)

- Do not duplicate ownership: each subsystem has one "owner" module.
- Do not introduce new global state/singletons/hooks without confirming an established pattern.
- Prefer extending existing helpers and patterns over inventing new ones.
- If a change is version-sensitive or depends on external APIs, verify via primary docs/tools or implement the lowest-risk change and document assumptions.
- Lessons > Memo > Existing Codebase > New Code (authority order).
- After changes: journal entry + relevant regression checks; add a lesson if the bug was non-obvious; update memo if "current truth" changed.

## Documentation & Changelog (MANDATORY)

- **After ANY feature/fix**: Update the relevant docs page in `Docs/src/content/`.
- **After ANY release-worthy change**: Add entry to `Docs/src/content/reference/changelog.mdx` using `<ChangelogSection>`.
- **If adding new commands/config**: Update `Docs/src/content/reference/commands.mdx` or `config.mdx`.
- **If changing UI/UX**: Update the relevant client page (`Docs/src/content/client/*.mdx`) and add/update screenshots.
- **If changing server systems**: Update the relevant server page (`Docs/src/content/server/*.mdx`).
- **Never ship a feature without documenting it** — the docs site IS part of the deliverable.
