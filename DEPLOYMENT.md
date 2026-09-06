# Free hosting deployment

## Recommended layout

Use one Render Free Web Service for `KuizServer` and a Neon Free PostgreSQL
database for questions. This keeps SignalR as the realtime transport, so the
desktop clients do not need to expose ports or communicate directly with the
database.

The free Koyeb service is deliberately a single instance. Lobby and game state
are held in server memory, so do not enable multiple replicas unless a shared
backplane and persistent lobby store are added.

## 1. Create the database

1. Create a Neon PostgreSQL project.
2. Copy its pooled or direct `postgresql://...` connection URL.
3. Keep the URL secret. It is supplied to the server only as `DATABASE_URL`.

The server creates the `questions` table at startup. To migrate existing data,
export the Railway `questions` table and import it into the new database before
switching clients.

## 2. Deploy the server

1. Push this repository to GitHub.
2. In Render, create a Web Service from the repository using the Docker runtime.
3. Set the Dockerfile path to `KuizServer/Dockerfile`. Render supplies `PORT`,
   which `KuizServer` already reads.
4. Add `DATABASE_URL` as a Render environment variable containing the Neon
   connection URL.
5. Deploy and open `/health`. It must return HTTP 200.

## 3. Point the desktop app at the new server

On each existing client, edit `%APPDATA%\Kuiz\config.json` and set both URLs to
the public Render URL:

```json
{
  "ApiUrl": "https://kuiz-server.onrender.com/api/question",
  "ServerUrl": "https://kuiz-server.onrender.com",
  "IsDebugMode": false
}
```

New builds read these settings for both question management and SignalR. Do not
put the Neon database URL in this file or distribute it with the desktop app.

## Operational limits

- Render's free service spins down after 15 minutes without traffic. The first
  request afterwards can take roughly 50 seconds or more to wake it; active
  SignalR games keep it busy.
- A server restart removes active lobbies and games, though questions persist
  in PostgreSQL.
- Treat the free tier as hobby/early-test hosting. Add authentication and a
  durable shared state store before running multiple server instances.
