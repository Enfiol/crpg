## Context

`README.md` opens with the `# cRPG` title followed immediately by a blank line, then a paragraph describing the project. The task is to add a one-line tagline directly under the title. This is a documentation-only change scoped to a single file; the analyst confirmed the wording and placement.

## Goals / Non-Goals

**Goals:**
- Add a single tagline line directly under the `# cRPG` title.
- Preserve all existing README content and formatting.

**Non-Goals:**
- Rewriting or restructuring the existing description paragraph.
- Editing any file other than `README.md`.
- Adding badges, links, or additional documentation.

## Decisions

- **Wording**: `A persistent multiplayer mod for Mount & Blade II: Bannerlord`. This mirrors the project's own description (per `README.md` and `AGENTS.md`) while staying to a single scannable line. Alternative wordings (e.g. naming individual game modes or persistence details like "xp, gold, items, stats") were rejected as too long for a tagline; that detail already lives in the paragraph below.
- **Placement**: Insert the tagline on line 2, on the currently blank line directly under `# cRPG`, keeping a blank line between the tagline and the existing description paragraph that begins on line 3. This keeps the title, tagline, and body visually separated and Markdown-valid. Alternative placement below the description paragraph was rejected because the tagline should be the first thing a reader sees.
- **Format**: Plain text on its own line (no heading, no blockquote, no emphasis), matching the README's existing plain-prose style.

## Risks / Trade-offs

- [Markdown spacing — tagline could visually merge with the title if no blank line follows] → Keep the blank line between the tagline and the description paragraph so the rendered output stays cleanly separated.
- [Scope creep into adjacent README edits] → Restrict the change to inserting one line; make no other edits to `README.md`.
