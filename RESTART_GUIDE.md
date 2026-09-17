# 🚀 BloodConnect Quick Restart Guide

The services have stopped. Here's how to restart them for continued development or demo.

---

## Option 1: Restart Both Services (Recommended)

### Terminal 1 — Backend API

```powershell
cd "C:\Users\ashwakumar\OneDrive - Microsoft\Documents\Hack\BloodConnect\src\BloodConnect.Api"
dotnet run --urls http://localhost:5030
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
	  Now listening on: http://localhost:5030
info: Microsoft.Hosting.Lifetime[0]
	  Application started. Press Ctrl+C to shut down.
```

### Terminal 2 — Frontend SPA

```powershell
cd "C:\Users\ashwakumar\OneDrive - Microsoft\Documents\Hack\BloodConnect\client"
npm.cmd run dev
```

**Expected output:**
```
VITE v8.2.2  ready in X ms ➜  Local:   http://localhost:5173/
```

### Terminal 3 (Optional) — Verify API is Responding

```powershell
Invoke-WebRequest -Uri "http://localhost:5030/api/demo-users" -UseBasicParsing | ForEach-Object { $_.StatusCode }
# Should return: 200
```

---

## Option 2: Run with Docker (Alternative)

If you prefer containerized services:

```powershell
cd "C:\Users\ashwakumar\OneDrive - Microsoft\Documents\Hack\BloodConnect"
docker compose up --build
```

This will start:
- API on http://localhost:8080
- Frontend on http://localhost:5173

---

## Once Services Are Running

### Access the App
- **Frontend**: http://localhost:5173
- **Backend Swagger**: http://localhost:5030/swagger (local dev mode)

### Demo Workflow
1. Use the demo user switcher to switch identities
2. Create a donor profile
3. Post a blood request (auto-matches compatible donors)
4. Switch to a donor and respond
5. See privacy in action (donor identity hidden until Available response)

---

## Troubleshooting

### Port Already in Use

If port 5030 or 5173 is already in use:

**Check what's using the port:**
```powershell
# For port 5030
netstat -ano | findstr :5030

# For port 5173
netstat -ano | findstr :5173
```

**Kill the process:**
```powershell
# Replace PID with the actual process ID from above
Stop-Process -Id <PID> -Force
```

Then restart the services.

### Database Issues

If you encounter database errors:

```powershell
# Delete the local SQLite database
cd "C:\Users\ashwakumar\OneDrive - Microsoft\Documents\Hack\BloodConnect\src\BloodConnect.Api"
Remove-Item bloodconnect.db -Force

# Restart backend — it will auto-create and seed the database
dotnet run --urls http://localhost:5030
```

### Frontend Won't Load

```powershell
# Clear npm cache
cd client
npm.cmd cache clean --force

# Reinstall dependencies
npm.cmd install

# Restart dev server
npm.cmd run dev
```

---

## Full Documentation

For more details, see:
- **README.md** — Architecture, setup, API reference, privacy, production migration
- **QUICK_START.md** — 5-minute guide
- **DEMO_WALKTHROUGH.md** — Detailed demo script
- **IMPLEMENTATION_SUMMARY.md** — Features and verification

---

## Next Steps

1. **Restart the services** using Terminal 1 & 2 commands above
2. **Open http://localhost:5173** in your browser
3. **Follow DEMO_WALKTHROUGH.md** for a guided tour
4. **Explore the features** (create profiles, requests, test matching, verify privacy)

---

**Ready?** Start with Terminal 1 (Backend) and Terminal 2 (Frontend), then open the browser! 🚀
