# BloodConnect — Quick Start

## 1. Install dependencies (once)

```powershell
# Backend (.NET)
cd src\BloodConnect.Api
dotnet restore

# Frontend (React/TypeScript)
cd ..\..\client
npm.cmd install
copy .env.example .env
```

## 2. Start the backend (Terminal 1)

```powershell
cd src\BloodConnect.Api
dotnet run --urls http://localhost:5030
```

✅ API is ready at `http://localhost:5030`  
✅ Swagger docs at `http://localhost:5030/swagger`  
✅ Demo database (`bloodconnect.db`) auto-created + seeded with 8 demo users

## 3. Start the frontend (Terminal 2)

```powershell
cd client
npm.cmd run dev
```

✅ SPA running at `http://localhost:5173`  
✅ Auto-reload on code changes

## 4. Try it out

1. Open `http://localhost:5173` in your browser.
2. Use the **user switcher** in the top navigation to become different employees.
3. Create a donor profile, view requests, respond to matches.

---

## Demo accounts (use the switcher UI)

| User          | Blood group | City       |
|---------------|-------------|-----------|
| Priya Sharma  | O+          | Bengaluru  |
| Arjun Mehta   | O+          | Bengaluru  |
| Fatima Khan   | A+          | Hyderabad  |
| Liam O'Connor | B+          | Dublin    |
| Wei Zhang     | AB+         | Singapore |
| Sofia Rossi   | O-          | Milan     |
| Noah Williams | A-          | Bengaluru |
| Ana Costa     | B-          | Lisbon    |

---

## Key features to explore

- **Donor profiles**: Set blood group, city, availability, see last donation date.
- **Blood requests**: Create a request → system matches compatible, available donors.
- **Smart matching**: Red-cell compatibility + eligibility check (56-day default interval) + location-based prioritization.
- **Privacy**: Matched donors receive notifications, but their identity is revealed to requester **only after they respond "Available"**.
- **Request lifecycle**: Track statuses (Open, Fulfilled, Cancelled) and responses from matched donors.

---

## Testing

```powershell
# Backend tests (50 xUnit tests: domain + integration)
dotnet test

# Frontend tests (13 vitest)
cd client
npm.cmd run test -- --run

# Frontend lint
npm.cmd run lint
```

---

## Next steps

- **Entra ID integration**: See README.md §5 for how to replace demo auth.
- **Teams deployment**: See README.md §9 for packaging + sideloading.
- **Azure deployment**: See README.md §8 for SQL Server + App Service config.
- **Docker**: `docker compose up --build` (requires Docker Desktop).

---

## Medical disclaimer

BloodConnect's eligibility guidance is **informational only**. It does not replace clinical screening at the donation center. The blood donation center's on-site medical assessment and local regulations always take precedence.
