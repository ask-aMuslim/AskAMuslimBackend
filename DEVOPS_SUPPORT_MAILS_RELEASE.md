# Support Mails release: DevOps guide

This release adds **Support Mails** (Contact Us / Mails History in the mobile app) and **push notifications**. One thing needs to be done on the server: give the API the **Firebase key**. Everything else deploys as usual.

---

## 1. What you'll receive

A Firebase service-account key: a JSON file named like `ask-a-muslim-mobile-app-firebase-adminsdk-xxxxx-xxxxxxxxxx.json`. It comes from the Firebase project **`ask-a-muslim-mobile-app`**.

It's a secret: anyone with it can send push notifications to our users.
- Keep it only on the server.
- Don't commit it to any repository.
- Don't paste it into tickets or chat groups.
- Delete your local copies once it's on the server.

---

## 2. Give the key to the API (one time)

The API reads the key from the environment variable **`Firebase__CredentialJson`** (two underscores). The value can be the raw JSON or the JSON **base64-encoded**. Base64 is easier because it's a single line with no quotes to escape.

The API runs with `docker compose` in the app folder on the server (the `api` service), so set the variable there.

**Step 1: encode the key** (on the server, in a private folder):

```bash
base64 -w0 ask-a-muslim-mobile-app-firebase-adminsdk-*.json > firebase.b64
```

**Step 2: add the variable to the `api` service.** Pick one option.

*Option A: an env file next to `docker-compose.yml` (recommended)*

```bash
cd <APP_PATH>
printf 'Firebase__CredentialJson=%s\n' "$(cat /path/to/firebase.b64)" > .env.firebase
chmod 600 .env.firebase
```

```yaml
# docker-compose.yml
services:
  api:
    env_file:
      - .env.firebase
```

*Option B: directly in `docker-compose.yml`*

```yaml
services:
  api:
    environment:
      Firebase__CredentialJson: "<paste the single base64 line here>"
```

> The deploy job runs `git reset --hard` in `<APP_PATH>`. That leaves **untracked** files alone, so `.env.firebase` and the server's own `docker-compose.yml` survive deploys. Keep them out of git.

**Step 3: recreate the container** so it picks up the variable:

```bash
docker compose up -d --force-recreate api
```

**Step 4: delete the leftovers**: `firebase.b64` and the downloaded JSON (once the env file holds the value).

---

## 3. Check it worked

```bash
docker compose logs api | grep -i firebase
```

| You see | Meaning |
|---|---|
| `Firebase initialized from Firebase:CredentialJson.` | ✅ Done |
| `Firebase is not initialized ...` (warning) | The variable is missing or the value is wrong. The API still runs, but no push notifications are sent. Check the name (`Firebase__CredentialJson`, two underscores) and that the base64 is a single line. |

---

## 4. What else this release does (no action needed)

- **Two database migrations run automatically on startup:**
  - `AddSupportMails` creates the tables `SupportMails` and `SupportMailMessages`.
  - `AddUserDeviceTokens` creates the table `UserDeviceTokens` and copies each user's existing `Users.FcmToken` into it.

  Both only **add** tables; no existing data is changed or removed. Take a normal database backup before deploying, as for any release with migrations.
- **Background jobs:** notification emails and pushes run as Hangfire jobs, in the same Postgres database and API process as the existing ones. Nothing new to run.
- **Email:** uses the existing SMTP configuration (`emailService.json`). New-mail and reply alerts go to `info@ask-a-muslim.com` by default. To change it, set `SupportMails__InboxEmail=<address>` on the `api` service.
- **The old `src/Web/firebase-adminsdk.json` is gone from the repository.** The next deploy's `git reset --hard` removes it from the server checkout. That's expected; the API now uses `Firebase__CredentialJson`.

---

## 5. Order

Setting the key and deploying the release can happen in either order. Until the key is set, the API works normally but push notifications fail (emails and in-app notifications still work).

---

## Checklist

- [ ] Received the key JSON privately
- [ ] `Firebase__CredentialJson` set on the `api` service (base64, single line)
- [ ] `docker compose up -d --force-recreate api`
- [ ] Logs show `Firebase initialized from Firebase:CredentialJson.`
- [ ] Local copies of the key deleted
- [ ] Database backed up before the release deploy
- [ ] After the deploy, logs show both migrations applied (`AddSupportMails`, `AddUserDeviceTokens`)
