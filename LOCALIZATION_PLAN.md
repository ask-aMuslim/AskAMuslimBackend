# Localization Plan

## Supported Languages

| Language   | Enum Value | Code |
|------------|-----------|------|
| English    | 1         | en   |
| Arabic     | 2         | ar   |
| French     | 3         | fr   |
| Spanish    | 4         | es   |
| German     | 5         | de   |
| Portuguese | 6         | pt   |

The primary target languages are **English**, **Arabic**, **Spanish**, and **Portuguese**.

---

## Architecture Overview

The project uses two localization patterns depending on the entity:

### 1. Translation-Table Pattern (Q&A)

Used by the **QA** entity. Each QA has a companion `QATranslation` row per language.

```
QA
  └── QATranslation
        ├── Language  (Language enum stored as string)
        ├── QuestionText
        └── AnswerText / AnswerTextJson
```

A unique index on `(QAId, Language)` enforces one translation per language per QA.  
Callers pass an optional `language` query parameter; when omitted all translations are returned.

### 2. Language-Attribute Pattern (Courses, Levels, Lessons, …)

For content entities (Level, Course, Lesson, etc.) a `Language` property is added **directly on the entity**. Each row represents content in one specific language. Queries read the `Accept-Language` HTTP request header and automatically filter records to the matching language.

```
Course
  ├── Language  ← attribute marking the language of this record
  ├── Title
  └── Description
```

**Querying:** `GET /Courses`, `GET /Levels`, `GET /Lessons` inspect the `Accept-Language` header (e.g. `Accept-Language: ar`) and apply a `WHERE Language = <resolved>` filter. If the header is absent or the language has no content, the API falls back to **English**.

---

## Current Status

### ✅ Implemented

- **QA / QATranslation** – Full CRUD with multi-language translation support.
  - `QuestionText`, `AnswerText`, `AnswerTextJson` (Tiptap rich-text) are translated per language.
  - Endpoints accept a `language` filter to retrieve a specific language's translations.
  - Statistics endpoint (`GET /Statistics`) reports QA translation counts per language.

---

## Pending Work

### Phase 1 – Core Content Entities (Language-Attribute Pattern)

Add a `Language` property directly to each content entity so that each row represents content in one specific language. Queries use the `Accept-Language` HTTP header to filter results automatically.

| Entity    | Fields                                              | Status  |
|-----------|-----------------------------------------------------|---------|
| Level     | `Title`, `Description`                              | Pending |
| Course    | `Title`, `Description`                              | Pending |
| Lesson    | `Title`, `Description`, `ContentJson`, `Transcript` | Pending |
| Question  | `QuestionText`                                      | Pending |
| Option    | `OptionText`                                        | Pending |
| Tag       | `Title`, `Description`                              | Pending |

For each entity the implementation steps are:
1. Add a `Language Language { get; set; }` property to the entity.
2. Add an EF Core migration.
3. In every "Get All" query handler, resolve the desired language from the `Accept-Language` HTTP header (falling back to `Language.English` when absent or unrecognised).
4. Apply `.Where(e => e.Language == resolvedLanguage)` inside the query.
5. Update Create / Update commands to accept a `Language` field.
6. Update Read DTOs to expose the `Language` field.

### Phase 2 – User-Facing Notifications

- Notification messages (`Notification.Message`) should be sent in the user's preferred language.
- Store the user's preferred language on the `StudentProfile` (or `User`) entity.
- Notification dispatch logic selects the appropriate translation at send-time.

### Phase 3 – Meeting & Inquiry Requests

- `MeetingRequest.Languages` and `InquiryRequest.Languages` already store a list of `Language` values representing the languages the requester is comfortable with.
- Moderator assignment can optionally be filtered by language preference.

---

## Database Considerations

- `Language` is stored as a **string** in all translation tables (via EF Core `HasConversion<string>()`). Adding a new `Language` enum value therefore requires **no schema migration** – only an application deployment.
- Translation tables use a **composite unique index** on `(ParentId, Language)` to prevent duplicates.

---

## API Design Guidelines

- **Content entities (Courses, Levels, Lessons, …):** Clients set the `Accept-Language` header (e.g. `Accept-Language: ar`). The API resolves the language from this header and filters results accordingly. If the header is absent or no content exists for that language, the response defaults to **English**.
- **Q&A:** Accepts an optional `language` query parameter. When omitted, all translations are returned (useful for admin/editor UIs).

---

## Deployment Notes

This project is deployed on **GitHub**. The `Language` enum values are stored as strings, so new languages are backward-compatible with existing deployments – no database downtime or data migration is required when adding a new language.
