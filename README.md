# Multiplayer Conway's Game of Life

A minimalist C# implementation of Conway's Game of Life with a single authoritative server, multiple observing clients, a sparse toroidal universe (`2^64 × 2^64`), console UI, and RLE persistence.

## Architecture

```
┌─────────────┐     SignalR (WebSocket)      ┌─────────────┐
│   Client    │ ◄──────────────────────────► │   Server    │
│  (Console)  │   snapshots + commands       │  (ASP.NET)  │
└─────────────┘                              └──────┬──────┘
                                                    │
                                             ┌──────▼──────┐
                                             │ GameSession │
                                             │ SparseEngine│
                                             └─────────────┘
```

- **Core** — sparse `HashSet<Coordinate>` universe, Conway rules, RLE load/save
- **Server** — one shared simulation; broadcasts generation updates to all connected clients
- **Client** — 100×100 console viewport for editing, loading patterns, starting/stopping simulation

### Large toroidal universe

Coordinates are `ulong`. Neighbor lookup uses unsigned overflow, so the grid wraps on both axes without modulo arithmetic. Only live cells are stored, so mostly empty universes are cheap.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download)

## Run

**Terminal 1 — start the server**

```bash
dotnet run --project src/GameOfLife.Server
```

Server listens on `http://localhost:5050` and bootstraps **Gosper's glider gun** at the center of the `2^64 × 2^64` torus (`2^63`, `2^63`).

**Terminal 2+ — start one or more clients**

```bash
dotnet run --project src/GameOfLife.Client
```

Optional arguments:

```bash
dotnet run --project src/GameOfLife.Client -- http://localhost:5050 patterns/gosper_glider_gun.rle
```

## Client controls

| Key | Action |
|-----|--------|
| Arrow keys | Move cursor (edit mode) |
| Space | Toggle cell |
| `c` | Clear viewport |
| `l` | Load pattern file (default: Gosper glider gun) |
| `s` | Save viewport to `saved_pattern.rle` |
| `a` | Apply viewport to server |
| `r` | Run simulation |
| `p` | Pause simulation |
| `o` | Observe live updates |
| `e` | Switch to edit mode |
| `g` / `h` / `j` / `k` | Pan viewport right / left / down / up |
| `q` | Quit |

Open multiple clients to watch the same simulation evolve in real time.

## Example pattern

`patterns/gosper_glider_gun.rle` — Gosper's glider gun in standard RLE format.

## Tests

```bash
dotnet test
```

## Design notes

- **SignalR** for multiplayer fan-out and automatic reconnection
- **Single writer** — server owns state; clients send commands, never simulate locally during observe mode
- **Viewport decoupling** — simulation runs on sparse global coordinates; UI renders a 100×100 window
- **Persistence** — RLE is the Life community standard and keeps example files human-readable

## AI-assisted development

This project was built with AI agent assistance (Cursor). Core design decisions — sparse torus representation, SignalR protocol, and separation of engine vs. host — were reviewed and validated with unit tests for rules, wrapping, and RLE round-trips.
