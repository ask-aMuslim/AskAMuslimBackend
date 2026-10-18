# Support Mails API — Mobile Integration Guide

This guide covers the backend APIs behind the **Contact Us**, **Mails History** and **Mail #N** screens.

> These are **new** endpoints under `/api/SupportMails`. Do **not** use the old `/api/InquiryRequests` endpoints (or the current `InquiryRepository`) for these screens.

The feature is a mail-style thread, **not** a live chat. The user sends an inquiry; staff (an admin or an assigned moderator) reply; the user can reply again at any time. There are no websockets; refresh the thread when the screen opens, on pull-to-refresh, and when a push notification arrives.

---

## 1. Basics

| | |
|---|---|
| Base path | `/api/SupportMails` |
| Auth | `Authorization: Bearer <accessToken>` on every endpoint (same token as the rest of the app) |
| JSON | camelCase |
| Enums | Sent and received as **integers** (see [Enums](#5-enums)) |
| Dates | ISO-8601 with offset, UTC (e.g. `"2026-10-11T10:42:00+00:00"`). Convert to local time for display |

Every endpoint only ever returns the **current user's** mails. Another user's mail id returns `400` with `"Mail not found"`.

### Response shapes

**Success with data** (`200 OK`): wrapped in a `Result`:

```json
{ "succeeded": true, "errors": [], "data": { ... } }
```

**Success without data** (`204 No Content`): empty body.

**Business error** (`400 Bad Request`):

```json
{ "succeeded": false, "errors": ["Mail not found"], "data": null }
```

Some endpoints return only `{ "errors": [...] }`; always read the `errors` array.

**Validation error** (`400 Bad Request`): ProblemDetails with per-field errors:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": { "Subject": ["'Subject' must not be empty."] }
}
```

**Unauthenticated**: `401`. Refresh the token as usual.

### Pagination (list endpoint)

`data` is:

```json
{
  "items": [ ... ],
  "pageNumber": 1,
  "totalPages": 3,
  "totalCount": 27,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

---

## 2. Screen → endpoint map

| Screen | Calls |
|---|---|
| **Contact Us** (form) | `GET /topics` to fill the dropdown, `GET /eligibility` to enable/disable Send, `POST /` on Send |
| **Contact Us** (success) | Show the success state, then `GET /eligibility` for the "You can send a new inquiry after one hour" hint |
| **Mails History** (top-right mail icon) | `GET /?pageNumber=1&pageSize=10` |
| **Mail #N** (detail) | `GET /{id}`, then `PUT /{id}/read` |
| **Mail #N** (Reply box) | `POST /{id}/replies`, then reload `GET /{id}` |

The "Welcome, Noah" name comes from the existing user-profile data the app already has; no new API is needed.

---

## 3. Endpoints

### 3.1 Get inquiry types

`GET /api/SupportMails/topics`

Fills the **Select your inquiry type** dropdown.

**200**

```json
{
  "succeeded": true,
  "errors": [],
  "data": [
    { "value": 1, "key": "GeneralInquiry", "name": "General Inquiry" },
    { "value": 2, "key": "Quran", "name": "Quran" },
    { "value": 3, "key": "Hadith", "name": "Hadith" },
    { "value": 4, "key": "ComparativeReligion", "name": "Comparative Religion" },
    { "value": 5, "key": "ConversionGuidance", "name": "Conversion Guidance" },
    { "value": 6, "key": "IslamicHistory", "name": "Islamic History" },
    { "value": 7, "key": "Fiqh", "name": "Fiqh" },
    { "value": 8, "key": "Theology", "name": "Theology" }
  ]
}
```

Send `value` back as `topic` when creating. `name` is English; if the app localises labels, key them on `key`.

---

### 3.2 Check whether the user can send a new mail

`GET /api/SupportMails/eligibility`

A user can create **one new mail per hour**. Replies are not limited.

**200**

```json
{ "succeeded": true, "errors": [], "data": { "canCreate": false, "nextAllowedAt": "2026-10-19T11:42:00+00:00" } }
```

- `canCreate: true` → `nextAllowedAt` is `null`.
- `canCreate: false` → disable **Send** and show *"You can send a new inquiry after one hour."* (you can also show the exact local time from `nextAllowedAt`).

---

### 3.3 Create a mail (Send)

`POST /api/SupportMails`

**Body**

```json
{
  "topic": 7,
  "subject": "Music Enquiry",
  "explanation": "Scholars argue that music distracts from remembrance of Allah..."
}
```

| Field | Type | Rules | UI field |
|---|---|---|---|
| `topic` | int | Required, a value from `/topics` | Select your inquiry type |
| `subject` | string | Required, max **200** chars. A one-sentence summary of the question | Write your question |
| `explanation` | string? | Optional, max **500** chars | Further explanation (the `0/500 characters` counter) |

If `explanation` is empty, the backend uses `subject` as the first message so the thread is never empty.

**201 Created**: the body is the new mail id as a **plain JSON string** (not wrapped in `Result`):

```json
"3f2b8f0e-6b1c-4c1e-9a55-0c1f2d3e4a5b"
```

The `Location` header is `/SupportMails/{id}`.

**400** when:
- the user sent a mail less than an hour ago: `errors: ["You can send a new inquiry after 2026-10-19T11:42:00.0000000+00:00."]`. Prefer calling `/eligibility` to decide what to show.
- validation fails (ProblemDetails, see above).

On success, show the **"Your Inquiry has been sent successfully."** screen.

---

### 3.4 List my mails (Mails History)

`GET /api/SupportMails`

| Query param | Type | Default | Notes |
|---|---|---|---|
| `pageNumber` | int | 1 | |
| `pageSize` | int | 10 | |
| `status` | int? | none | Filter by [status](#supportmailstatus) |
| `searchTerm` | string? | none | Searches the subject |

Sorted by **latest activity first** (`lastMessageAt` descending).

**200** (`data.items[]`)

```json
{
  "id": "3f2b8f0e-6b1c-4c1e-9a55-0c1f2d3e4a5b",
  "number": 4,
  "topic": 7,
  "subject": "Music Enquiry",
  "preview": "Scholars argue that music distracts from remembrance of Allah, and often promotes...",
  "status": 2,
  "hasUnreadReply": true,
  "created": "2026-10-11T10:42:00+00:00",
  "lastMessageAt": "2026-10-12T14:15:00+00:00"
}
```

Mapping to the card in the design:

| Card element | Field |
|---|---|
| Title ("Music Enquiry") | `subject` |
| Time ("10:42 AM") | `lastMessageAt` (or `created` if you prefer the send time) |
| 2-line body text | `preview`: the first ~150 chars of the user's original message; ellipsize in the UI |
| Orange **"Will Respond Soon"** + orange icon | `status == 1` (AwaitingResponse) |
| Green **"Responded"** + green icon | `status == 2` (Responded) |
| Closed (no design yet) | `status == 3`, suggest a grey "Closed" label |
| Unread dot/bold (optional) | `hasUnreadReply == true`: staff replied and the user hasn't opened it since |
| "View Details >" | Navigate to Mail detail with `id` |

---

### 3.5 Get one mail (Mail #N)

`GET /api/SupportMails/{id}`

**200** (`data`)

```json
{
  "id": "3f2b8f0e-6b1c-4c1e-9a55-0c1f2d3e4a5b",
  "number": 4,
  "topic": 7,
  "subject": "Music Enquiry",
  "status": 2,
  "created": "2026-10-11T10:42:00+00:00",
  "lastMessageAt": "2026-10-12T14:15:00+00:00",
  "messages": [
    {
      "id": "a1...",
      "senderType": 1,
      "senderName": "Noah Smith",
      "senderProfilePictureUrl": "https://.../noah.jpg",
      "body": "Scholars argue that music distracts from remembrance of Allah, and often promotes sinful themes. Where can I learn more about this view?",
      "created": "2026-10-11T10:42:00+00:00"
    },
    {
      "id": "b2...",
      "senderType": 2,
      "senderName": null,
      "senderProfilePictureUrl": null,
      "body": "Hello Noah,\n\nThank you for your inquiry. Scholars hold different views on music...",
      "created": "2026-10-12T14:15:00+00:00"
    }
  ]
}
```

- `messages` are in **chronological order** (oldest first). The design shows the newest on top; reverse the list in the UI if you keep that layout.
- Header: `"Mail #" + number`; title: `subject`; badge: `status` (same labels as the list).
- `senderType == 1` (Requester) → the user's own message: show `senderName`, `senderProfilePictureUrl`, and the subtitle "Your inquiry".
- `senderType == 2` (Staff) → show **"Support Team"** with the app logo and the subtitle "Support reply". Staff names are intentionally `null` here.
- Each message's date/time line ("12 Oct 2026 · 2:15 PM") comes from `created`.
- `body` is plain text and may contain `\n` line breaks; render them.

---

### 3.6 Mark a mail as read

`PUT /api/SupportMails/{id}/read`

No body. Returns **204**. Call it after the detail screen loads so `hasUnreadReply` clears in the history list.

---

### 3.7 Reply to a mail (Reply box)

`POST /api/SupportMails/{id}/replies`

**Body**

```json
{ "body": "Thank you. Could you recommend a book on this?" }
```

| Field | Rules |
|---|---|
| `body` | Required, not blank, max **4000** chars |

**200**: `data` is the new message id:

```json
{ "succeeded": true, "errors": [], "data": "c3..." }
```

Effects:
- The mail status becomes **AwaitingResponse** (`1`), i.e. "Will Respond Soon".
- Replying is allowed **at any time**, including on a **Closed** mail, which reopens it.
- The reply also counts as "read" for the user.

After success, clear the input and reload `GET /{id}` (or append the message locally).

---

## 4. Push notifications

When staff reply, the user gets a push notification (plus an in-app notification and an email):

- **Title**: `New reply on Mail #4`
- **Body**: `The support team replied to your inquiry "Music Enquiry".`
- **Data payload**:

```json
{ "type": "support_mail", "mailId": "3f2b8f0e-6b1c-4c1e-9a55-0c1f2d3e4a5b" }
```

On tap (or while the app is open) with `type == "support_mail"`, open / refresh **Mail detail** for `mailId`. Push uses the FCM token the app already registers.

---

## 5. Enums

### `SupportMailStatus`

| Value | Name | UI label |
|---|---|---|
| 1 | AwaitingResponse | Will Respond Soon (orange) |
| 2 | Responded | Responded (green) |
| 3 | Closed | Closed (grey; not in the design yet) |

### `SupportMailSenderType`

| Value | Name | UI |
|---|---|---|
| 1 | Requester | The user ("Your inquiry") |
| 2 | Staff | "Support Team" ("Support reply") |

### `MeetingInquiryTopic` (inquiry type)

Use `GET /topics` rather than hard-coding these: `1` General Inquiry, `2` Quran, `3` Hadith, `4` Comparative Religion, `5` Conversion Guidance, `6` Islamic History, `7` Fiqh, `8` Theology.

---

## 6. Limits summary

| Rule | Value |
|---|---|
| New mails per user | 1 per hour (`/eligibility` tells you when) |
| `subject` | max 200 chars, required |
| `explanation` | max 500 chars, optional |
| Reply `body` | max 4000 chars, required |
| Replies | unlimited, any time, also on closed mails |

---

## 7. Suggested flow

```
Contact Us opens
  ├─ GET /topics
  └─ GET /eligibility ── canCreate=false → disable Send, show "after one hour" hint
Send
  └─ POST /  → 201 → success screen → GET /eligibility (for the hint)
Mail icon (top-right)
  └─ GET /?pageNumber=1  (load more while hasNextPage)
View Details
  ├─ GET /{id}
  └─ PUT /{id}/read
Reply
  └─ POST /{id}/replies → GET /{id}
Push "support_mail"
  └─ open Mail detail(mailId) → GET /{id} → PUT /{id}/read
```
