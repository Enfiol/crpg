## ADDED Requirements

### Requirement: README displays a project tagline

The repository `README.md` SHALL display a single-line tagline immediately under the top-level `# cRPG` title that concisely describes the project as a persistent multiplayer mod for Mount & Blade II: Bannerlord.

#### Scenario: Tagline appears directly under the title

- **WHEN** a reader opens `README.md`
- **THEN** the line immediately following the `# cRPG` title is a single-line tagline describing what the project is
- **AND** the tagline reads `A persistent multiplayer mod for Mount & Blade II: Bannerlord`

#### Scenario: Existing README content is preserved

- **WHEN** the tagline is added
- **THEN** the `# cRPG` title, the existing description paragraph, and the Contributing and License sections remain unchanged
- **AND** no file other than `README.md` is modified
