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
| **Mail icon badge** (top-right) | `GET /unread-count` |
| **Mails History** (top-right mail icon) | `GET /?pageNumber=1&pageSize=10` |
| **Mail #N** (detail) | `GET /{id}`, then `PUT /{id}/read` |
| **Mail #N** (Reply box) | `POST /{id}/replies`, then reload `GET /{id}` |
| **App start / login / logout** | Push token registration, see [Notifications](#4-notifications) |
| **Notifications list / bell** (if the app has one) | `GET /api/Notifications/me`, see [In-app notifications](#45-in-app-notifications) |

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

- `messages` are in **chronological order** (oldest first). Show them in that order, oldest at the top and newest at the bottom (the website does the same). Don't reverse the list, even though the design shows the newest on top. After loading or replying, scroll to the newest message.
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

### 3.8 Unread mails count (mail icon badge)

`GET /api/SupportMails/unread-count`

**200**

```json
{ "succeeded": true, "errors": [], "data": 2 }
```

The number of the user's mails where staff replied and the user hasn't opened the mail since (same rule as `hasUnreadReply`). Show it as a badge on the top-right mail icon; hide the badge at `0`. Refresh it on app resume, after `PUT /{id}/read`, and when a `support_mail` push arrives.

---

## 4. Notifications

The backend notifies the user about their mails in three ways. All of them are sent in the background a few seconds after the change, never inside the API call.

| Event | Push | In-app notification | Email |
|---|---|---|---|
| User sends a new mail | — | — | ✅ "We received your inquiry (Mail #4)" |
| Staff reply | ✅ | ✅ | ✅ includes the reply text |
| Mail closed by staff | ✅ | ✅ | — |

(Staff also get notified about new mails and user replies; that's not something the app needs to handle.)

> **Status today:** the backend side is done. The app does **not** receive pushes yet: it has no FCM client, never registers its token, and doesn't handle taps. Sections 4.1–4.4 are what the app needs to add.
>
> To test pushes against a backend you run locally, the backend also needs its Firebase key (see the backend README, "Firebase push notifications"). The production key is set up by DevOps.

### 4.1 Android setup (one time)

1. Add Firebase Cloud Messaging: `implementation(platform("com.google.firebase:firebase-bom:<version>"))` and `implementation("com.google.firebase:firebase-messaging")` in `app/build.gradle.kts`. The `com.google.gms.google-services` plugin is already applied.
2. Put `google-services.json` in `Mobile-App/app/`. **It was sent to you on WhatsApp.**
   - It's for the Firebase project **`ask-a-muslim-mobile-app`** and the Android app **`com.abdallamusa.ask_a_muslim`** (the current `applicationId`). Check that `project_id` in the file is `ask-a-muslim-mobile-app`.
   - The backend sends pushes with a key from this same project. A `google-services.json` from any other project won't receive them (FCM "sender ID mismatch").
   - It's gitignored: don't commit it. Every developer machine and the CI/release build needs its own copy.
   - If `applicationId` ever changes to `com.askamuslim.app` (also registered in the same project), download that app's `google-services.json` from Firebase console → Project settings → General. No backend change is needed.
3. Create a notification channel for these pushes (e.g. id `support_mails`, name "Support mails") with `NotificationUtils`.
4. On Android 13+ request `POST_NOTIFICATIONS` at runtime (it's already declared in the manifest). A good moment is right after the user sends their first mail, or after login.
5. Add a `FirebaseMessagingService` subclass and register it in the manifest with the `com.google.firebase.MESSAGING_EVENT` intent filter. It handles `onNewToken` (4.2) and `onMessageReceived` (4.3).

### 4.2 Register and unregister the device token

A user can be signed in on several devices; each device registers its own token.

**Register:** call after every successful login, on app start while signed in, and from `onNewToken`:

`POST /api/Notifications/register-token`

```json
{ "fcmToken": "<token from FirebaseMessaging.getInstance().token>", "platform": 1 }
```

| Field | Rules |
|---|---|
| `fcmToken` | Required, max 255 chars |
| `platform` | Optional: `0` Unknown, `1` Android, `2` iOS, `3` Web |

Returns **204**. Calling it again with the same token is fine (it just refreshes it). If another account had registered the same device, the token moves to the current user.

**Unregister:** call on logout, **before** clearing the access token (the call needs auth):

`POST /api/Notifications/unregister-token`

```json
{ "fcmToken": "<this device's token>" }
```

Returns **204**, also if the token wasn't registered. After this the device stops getting the previous user's pushes.

Tokens that Firebase reports as dead are removed by the backend automatically.

### 4.3 Push payload

Pushes contain both a `notification` block (title and body, shown by the system when the app is in the background) and a `data` block:

| Event | Title | Body | `data` |
|---|---|---|---|
| Staff reply | `New reply on Mail #4` | `The support team replied to your inquiry "Music Enquiry".` | `{ "type": "support_mail", "mailId": "<id>", "event": "staff_reply" }` |
| Mail closed | `Mail #4 was closed` | `Your inquiry "Music Enquiry" was closed. You can still reply to reopen it.` | `{ "type": "support_mail", "mailId": "<id>", "event": "closed" }` |

Moderators who use the app may also receive `event` values `assigned` and `requester_reply` for mails assigned to them. Treat any unknown `event` like `staff_reply`.

**App in the foreground:** Android does **not** display the notification automatically. In `onMessageReceived`:
- if Mail #N for that `mailId` is open, reload it (`GET /{id}`) instead of showing a notification;
- otherwise show a local notification on the `support_mails` channel with the title/body, putting `type` and `mailId` in the tap intent;
- in both cases refresh the mail badge (`GET /api/SupportMails/unread-count`) and, if visible, the Mails History list.

### 4.4 Tapping a notification

When the app is in the background, the system shows the notification and a tap opens the **launcher activity** with the `data` values as intent extras (`intent.extras?.getString("type")`, `getString("mailId")`). The same applies to the local notifications you build in 4.3.

On launch / `onNewIntent`:

```
if extras.type == "support_mail" and the user is signed in:
    navigate to Mail #N (mailId)  →  GET /api/SupportMails/{mailId}  →  PUT /api/SupportMails/{mailId}/read
if not signed in:
    keep mailId, log in, then navigate
```

If the mail returns `400 Mail not found` (e.g. the device was handed over to another account), just open Mails History.

### 4.5 In-app notifications

Every push is also stored as an in-app notification, so the app can show a notification list or bell even when pushes are disabled. These endpoints work for every signed-in user and only return the current user's notifications.

| Call | Purpose |
|---|---|
| `GET /api/Notifications/me?pageNumber=1&pageSize=20&unreadOnly=false&notificationType=SupportMail` | Paginated list, newest first (`notificationType` optional) |
| `GET /api/Notifications/me/unread-count` | `data` is the number of unread notifications |
| `PUT /api/Notifications/me/{notificationId}/read` | Mark one as read (204) |
| `PUT /api/Notifications/me/read-all` | Mark all as read (204) |

Item shape (`data.items[]`):

```json
{
  "id": "9a1c...",
  "userId": "5d2e...",
  "title": "New reply on Mail #4",
  "message": "The support team replied to your inquiry \"Music Enquiry\".",
  "isRead": false,
  "actionUrl": "support-mails/3f2b8f0e-6b1c-4c1e-9a55-0c1f2d3e4a5b?event=staff_reply&id=7c0d...",
  "notificationType": "SupportMail",
  "createdAt": "2026-10-12T14:15:05+00:00"
}
```

For `notificationType == "SupportMail"`, the mail id is the second path segment of `actionUrl` (`support-mails/{mailId}?...`). On tap: mark the notification as read and open Mail #N as in 4.4.

> Don't use `GET /api/Notifications/{userId}`; it's an admin endpoint and returns 403 for normal users.

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
Login / app start (signed in)
  ├─ POST /api/Notifications/register-token   (also from onNewToken)
  └─ GET  /unread-count                        → mail icon badge
Contact Us opens
  ├─ GET /topics
  └─ GET /eligibility ── canCreate=false → disable Send, show "after one hour" hint
Send
  └─ POST /  → 201 → success screen → GET /eligibility (for the hint)
              → (ask for POST_NOTIFICATIONS on Android 13+ if not granted yet)
Mail icon (top-right)
  └─ GET /?pageNumber=1  (load more while hasNextPage)
View Details
  ├─ GET /{id}
  └─ PUT /{id}/read → GET /unread-count
Reply
  └─ POST /{id}/replies → GET /{id}
Push "support_mail" received (foreground)
  └─ Mail #N open? reload it : show local notification → GET /unread-count
Push "support_mail" tapped
  └─ open Mail detail(mailId) → GET /{id} → PUT /{id}/read
Logout
  └─ POST /api/Notifications/unregister-token → clear session
```
