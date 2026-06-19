# M3U Rewrite Fixtures

Golden fixtures for the in-house M3U rewrite (`PlaylistRewriter`). Each case has an
`*.input.m3u` (a representative TvHeadend playlist) and an `*.expected.m3u` (the
byte-exact output the rewrite must produce). Tests assert byte-equality.

## Contract: verbatim passthrough (decided in Phase 2 planning)

Only **stream-URL lines** are transformed; everything else is passed through
**unchanged**, including line endings.

URL-line transform:
- Inject `user:pass@` into the authority (test credentials are `user` / `pass`).
- Drop the expiring `ticket` query parameter.
- Keep every other query parameter (profile, etc.) in its original order, with a
  spec-correct leading `?`, e.g. `.../stream/channelid/1234?profile=pass`.

Non-URL lines (`#EXTM3U`, `#EXTINF`, comments, blank lines) and the original line
endings are preserved **exactly as the input**. We do NOT reproduce the old
vendored playlist-serializer reformatting (it inserted a space before the `#EXTINF`
comma and normalized CRLF→LF — both incidental artifacts, not desired behavior).

## Encoding / line endings
- UTF-8, no BOM.
- `lf.*` and most cases use `\n`; `crlf.*` uses `\r\n` and the expected output
  preserves `\r\n` (passthrough). Trailing newline is preserved per input.

## Cases
| case | exercises |
|------|-----------|
| `ticket-and-profile` | URL with `?ticket=...&profile=pass` → creds + profile kept |
| `ticket-only` | URL with `?ticket=...` only → creds, query dropped |
| `multi-channel` | several channels, mixed profile/no-profile |
| `comments-blanks` | `#EXTM3U`, a `#` comment, a blank line, `#EXTINF` with attributes → all verbatim |
| `lf` | LF line endings preserved |
| `crlf` | CRLF line endings preserved |
| `profile-extra-params` | params follow profile → ticket dropped, the rest kept in order with a leading `?`, e.g. `...?profile=pass&foo=bar` |

Credentials in fixtures are dummy (`user`/`pass`); no real secrets.
