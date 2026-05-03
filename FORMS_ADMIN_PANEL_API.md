# Forms API for Admin Panel Frontend

Base route: `/api/Forms`

This document covers the forms endpoints the admin panel should use to manage form definitions and review submissions.

## Data Shapes

### FormFieldType

Accepted values:

- `Text`
- `TextArea`
- `Email`
- `Number`
- `Date`
- `Checkbox`
- `Radio`
- `Select`

### Form DTO

```json
{
  "id": "guid",
  "title": "string",
  "description": "string | null",
  "isPublished": true,
  "fields": [
    {
      "id": "guid",
      "label": "string",
      "type": "Text",
      "isRequired": true,
      "order": 0,
      "options": [
        {
          "id": "guid",
          "label": "string",
          "value": "string",
          "order": 0
        }
      ]
    }
  ]
}
```

### Form Submission DTO

```json
{
  "id": "guid",
  "formId": "guid",
  "userId": "string | null",
  "answersJson": "string",
  "created": "2026-04-18T10:30:00Z"
}
```

## Response Envelope

The read endpoints return the shared `Result<T>` wrapper on success and failure.

Success example:

```json
{
  "succeeded": true,
  "errors": [],
  "data": []
}
```

Failure example:

```json
{
  "succeeded": false,
  "errors": ["Form not found"],
  "data": null
}
```

Create endpoints return `201 Created` with the created GUID in the response body.

## Endpoints

### Get all forms

`GET /api/Forms`

No authorization required.

Response: `200 OK`

```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "title": "Contact Us",
      "description": "General questions and support",
      "isPublished": true,
      "fields": []
    }
  ]
}
```

### Get form by id

`GET /api/Forms/{id}`

No authorization required.

Response: `200 OK`

```json
{
  "succeeded": true,
  "errors": [],
  "data": {
    "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "title": "Contact Us",
    "description": "General questions and support",
    "isPublished": true,
    "fields": [
      {
        "id": "a1111111-1111-1111-1111-111111111111",
        "label": "Your name",
        "type": "Text",
        "isRequired": true,
        "order": 0,
        "options": []
      }
    ]
  }
}
```

If the form does not exist, the API returns `400 Bad Request` with:

```json
{
  "succeeded": false,
  "errors": ["Form not found"],
  "data": null
}
```

### Create form

`POST /api/Forms`

Authorization: `Policies.ManageForms`

Request body:

```json
{
  "title": "Volunteer Registration",
  "description": "Collect volunteer details",
  "isPublished": false,
  "fields": [
    {
      "label": "Full name",
      "type": "Text",
      "isRequired": true,
      "order": 0,
      "options": []
    },
    {
      "label": "Preferred contact method",
      "type": "Select",
      "isRequired": true,
      "order": 1,
      "options": [
        { "label": "Email", "value": "email", "order": 0 },
        { "label": "Phone", "value": "phone", "order": 1 }
      ]
    }
  ]
}
```

Response: `201 Created`

```json
"f7f6ce1d-1b7c-4c5f-b71a-6fdf6db4d0d4"
```

The `Location` header points to `/Forms/{createdId}`.

### Get form submissions

`GET /api/Forms/{formId}/submissions`

Authorization: `Policies.ViewForms`

Response: `200 OK`

```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    {
      "id": "5d0e3b2c-8e1c-4f3b-9f5e-2b1e3d2e9d10",
      "formId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "userId": "user-123",
      "answersJson": "{\"fullName\":\"Amina Khan\",\"preferredContactMethod\":\"email\"}",
      "created": "2026-04-18T10:30:00Z"
    }
  ]
}
```

## Admin Panel Usage Notes

- Use `Get all forms` to populate the form list page.
- Use `Get form by id` to load the edit screen.
- Use `Create form` to save a new form definition from the builder UI.
- Use `Get form submissions` to show the submissions table and the submission detail drawer.
- `answersJson` is stored as a string; the backend does not validate its internal schema.
- For options-based fields such as `Select` and `Radio`, send the `options` array in the same order the UI should display.
