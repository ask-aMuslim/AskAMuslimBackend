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

Localization is handled via **per-entity translation tables**. Each translatable entity has a companion `*Translation` entity that stores language-specific fields. The `Language` enum (in `Domain/Enums/Language.cs`) is the single source of truth for supported languages.

### Translation Pattern

```
Entity (e.g. QA)
  └── EntityTranslation (e.g. QATranslation)
        ├── Language  (Language enum stored as string)
        ├── <field1>  (translated field)
        └── <field2>  (translated field)
```

A unique index on `(EntityId, Language)` enforces one translation per language per entity.

---

## Current Status

### ✅ Implemented

- **QA / QATranslation** – Full CRUD with multi-language translation support.
  - `QuestionText`, `AnswerText`, `AnswerTextJson` (Tiptap rich-text) are translated per language.
  - Endpoints accept a `language` filter to retrieve a specific language's translations.
  - Statistics endpoint (`GET /Statistics`) reports QA translation counts per language.

---

## Pending Work

### Phase 1 – Core Content Entities

These entities contain user-facing content and need translation tables:

| Entity    | Fields to Translate                   | Status  |
|-----------|---------------------------------------|---------|
| Level     | `Title`, `Description`                | Pending |
| Course    | `Title`, `Description`                | Pending |
| Lesson    | `Title`, `Description`, `ContentJson`, `Transcript` | Pending |
| Question  | `QuestionText`                        | Pending |
| Option    | `OptionText`                          | Pending |
| Tag       | `Title`, `Description`                | Pending |

For each entity the implementation steps are:
1. Create a `*Translation` domain entity (e.g. `CourseTranslation`).
2. Add a `Translations` navigation property to the parent entity.
3. Add an EF Core configuration class (`*TranslationConfiguration`).
4. Register the new `DbSet` in `IApplicationDbContext` and `ApplicationDbContext`.
5. Add an EF Core migration.
6. Update Create / Update commands to accept `List<*TranslationInput>`.
7. Update Read queries / DTOs to expose translations (optionally filtered by language).
8. Update endpoint summaries.

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

- Endpoints that return translatable content accept an optional `language` query parameter.
- When `language` is omitted, **all** translations are returned (useful for admin/editor UIs).
- When `language` is provided, only the matching translation is included (useful for client apps).

---

## Deployment Notes

This project is deployed on **Azure** (cloud). The `Language` enum values are stored as strings, so new languages are backward-compatible with existing deployments – no database downtime or data migration is required when adding a new language.
