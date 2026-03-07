# Plant Records TSV Format

Upload tab-separated files to `POST /api/import/plant-records` to import or update plant records.

## Required columns

| Column | Description |
|--------|-------------|
| `AccessionNumber` | Unique identifier for the plant record (primary key). |
| `PlantName` | Scientific or common name of the plant. |
| `Location` | Physical location within the collection (garden, section, etc.). |

## Optional columns

| Column | Description |
|--------|-------------|
| `CollectionTrip` | Name/date of the collection expedition. Leave blank if unknown. |

## Rules

- **Delimiter:** Tab (`\t`). Do not use commas.
- **Header row:** Required. Column names are case-insensitive.
- **Upsert:** If an `AccessionNumber` already exists, the record is updated. Otherwise a new record is created.
- **Blank lines:** Skipped silently.
- **Validation:** Each row is validated independently. Rows with errors are skipped and reported in the response.

## Example

See `plant-records-sample.tsv` in this folder.

## API Response

```json
{
"imported": 7,
  "updated": 3,
  "errors": 0,
  "errorDetails": []
}
```

If rows have errors:

```json
{
  "imported": 5,
  "updated": 2,
  "errors": 1,
  "errorDetails": [
    "Line 9: AccessionNumber is empty"
  ]
}
```
