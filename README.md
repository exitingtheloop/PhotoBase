# PhotoBase

A secure, internal-first photo and asset bank built with .NET 8. Public users can search and browse thumbnails with metadata. Authenticated internal users can download originals. Admins can upload images and import plant records via TSV.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | ASP.NET Core .NET 8 (Controllers) |
| Client | Blazor WebAssembly |
| Shared | Class library for DTOs/contracts |
| Database | SQLite (MVP) via EF Core |
| UI | USWDS (United States Web Design System) |
| Thumbnails | SixLabors.ImageSharp |
| Auth | JWT with seeded users |

## Projects

- **PhotoBase.API** — Web API, data layer, file storage, auth
- **PhotoBase.Client** — Blazor WASM front-end (USWDS)
- **PhotoBase.Shared** — DTOs, enums, constants shared between API and Client

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run locally

```bash
# From solution root
dotnet build
dotnet run --project PhotoBase.API
```

The API hosts the Blazor WASM client. Navigate to `https://localhost:7245` (or `http://localhost:5075`).

### Seeded Users (MVP)

| Email | Password | Role |
|-------|----------|------|
| `admin@photobase.local` | `Admin123!` | Admin |
| `user@photobase.local` | `User123!` | Internal |

> Public access requires no login.

### Storage

Originals and thumbnails are stored on the local filesystem:

```
./data/originals/
./data/thumbs/
```

These directories are created automatically on first upload and are git-ignored.

### Sample TSV Format

Place a TSV file in `samples/` for testing plant record import. Required columns:

```
AccessionNumber	PlantName	Location	CollectionTrip
2024-001	Magnolia zenii	Garden A	Spring 2024
2024-002	Quercus alba	Garden B	Fall 2023
```

## API Endpoints

| Method | Route | Auth | Description |
|--------|-------|------|-------------|
| `POST` | `/api/auth/login` | — | Login, returns JWT |
| `GET` | `/api/assets` | Public | Search + paging |
| `GET` | `/api/assets/{id}` | Public | Asset detail |
| `GET` | `/api/assets/{id}/thumb` | Public | Thumbnail image |
| `GET` | `/api/assets/{id}/download` | Internal+ | Download original |
| `POST` | `/api/assets` | Admin | Upload image + metadata |
| `POST` | `/api/import/plant-records` | Admin | Import TSV |

## Definition of Done (MVP Demo)

- [ ] Public: search "Magnolia zenii" ? results with thumbnail + metadata
- [ ] Public: open detail ? no download button
- [ ] Internal login: download button appears and works
- [ ] Admin: import TSV ? summary (Imported X / Updated Y / Errors Z)
- [ ] Admin: upload image + accession ? auto-fill from PlantRecord
- [ ] Upload duplicate image ? warning/blocked

## License

Internal use only.
