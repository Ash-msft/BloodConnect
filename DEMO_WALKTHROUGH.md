# BloodConnect Demo Walkthrough

**Status**: ✅ App is running live at http://localhost:5173

---

## Quick Demo Script (5 minutes)

### Step 1: Open the app (30 seconds)

Navigate to **http://localhost:5173** in your browser. You'll see:
- A clean, professional dashboard
- Top navigation with "BloodConnect" logo
- A **demo user switcher** (top-right dropdown)
- Currently logged in as a default demo user

### Step 2: Switch identities (30 seconds)

**Goal**: Show that the app supports multiple employees

1. Click the **demo user dropdown** in the top navigation
2. Select **"Priya Sharma"** (O+ donor in Bengaluru)
3. Observe the page refresh and recognize Priya's data

Notice:
- Page shows personalized content for Priya
- No login form — instant switching for demo purposes
- In production, this would be replaced with Entra ID SSO

### Step 3: Create a donor profile (1 minute)

**Current user**: Priya Sharma (O+, Bengaluru)

1. Click **Profile** in the left sidebar
2. You should see:
   - Blood Group: **O+** (pre-filled)
   - City: **Bengaluru** (pre-filled)
   - Availability: Toggle **Available** ✓
   - Last Donation: Shows date or "Never"

3. **Click "Save Profile"** to confirm
4. Notice the success notification

**Privacy Note**: This profile is private. Only Priya can see or edit it.

### Step 4: Create a blood request (2 minutes)

**Current user**: Switch to **Arjun Mehta** (O+, Bengaluru — the requester)

1. Click **Create Request** in the sidebar
2. Fill in:
   - **Required Blood Group**: O+
   - **Hospital Name**: "Max Healthcare, Delhi"
   - **Hospital City**: Delhi
   - **Units Needed**: 2
   - **Urgency**: High
   - **Additional Notes**: "Patient in critical condition, immediate need"

3. **Click "Create Request"**
4. Observe the success notification

**Behind the scenes**:
- System automatically finds all O+ donors who are available
- Runs eligibility check (they can donate 56+ days since last donation)
- Sends targeted notifications ONLY to eligible donors
- Non-eligible donors never see this request

### Step 5: Check donor notifications (1 minute)

**Switch to**: **Priya Sharma** (O+ donor)

1. Click **Notifications** in the sidebar
2. You'll see a new notification:
   - **"Blood Request Match"**
   - Hospital: Max Healthcare, Delhi
   - Blood Group: O+ (matches Priya)
   - Urgency: High
   - Status: "Unread"

3. **Click the notification** (or click **Matches** in sidebar to see available requests)
4. View the request details:
   - Hospital name, location, units, urgency, notes
   - **"I can help" button** (respond Available)
   - **"Not available" button** (respond Not Available)

### Step 6: Donor responds "Available" (30 seconds)

**Current**: Priya Sharma (donor)

1. Click **"I can help"** (Available button)
2. Observe the success notification
3. Status changes to **"Response Sent"**

**Privacy Key**: Priya's identity is now revealed to Arjun (the requester), because she affirmed availability.

### Step 7: Requester sees responses (30 seconds)

**Switch to**: **Arjun Mehta** (requester)

1. Click **Requests** in the sidebar
2. Find the request you just created
3. **Click it** to view details
4. Under **"Donor Responses"**, you'll see:
   - **Priya Sharma**
   - **Contact**: Phone, email (visible because she responded Available)
   - **Status**: "Available"

You can now contact Priya to arrange the donation.

**Privacy Guarantee**: 
- If another O+ donor had responded "Not Available", their name would show as **[Private]** or remain hidden
- Only Arjun (the requester) sees Priya's details
- Priya's details are **never** exposed to other donors

---

## Full Feature Tour (if you have 10 minutes)

### Additional Actions to Show

#### Donation History
1. Switch to **Priya Sharma** (Donor)
2. Click **Profile** → scroll to "Donation History"
3. Click **"Record Donation"** to log today as a donation
4. Observe:
   - Last Donation updated to today
   - Eligibility counter shows "56 days until next eligible donation"
   - This is automatic in production after blood center confirms

#### Eligibility Check
1. Stay as **Priya Sharma**
2. Create another request requiring O+ blood
3. Priya won't appear as a match (she just donated today)
4. Only donors not disqualified by the 56-day rule appear

#### Request Lifecycle
1. Switch to **Arjun Mehta** (requester)
2. View your request
3. Options:
   - **Mark Fulfilled** — Blood was collected, request closes
   - **Cancel Request** — No longer needed, close it

#### Multi-City Prioritization
1. Create a request requiring A+ blood in **Hyderabad**
2. Switch to **Fatima Khan** (A+, Hyderabad) and **Sofia Rossi** (O-, Milan)
3. Both will receive notifications (but Fatima is prioritized as she's in the same city)

#### Blood Type Compatibility
1. Try creating a request for **AB-** blood
2. Notice: Only **Sofia Rossi (O-)** and **Ana Costa (B-)** can donate AB-
3. Others won't receive notifications (they're incompatible)

**The system enforces**:
- O → all (universal donor)
- A → A, AB
- B → B, AB
- AB → AB only
- Plus Rh factor checks

---

## Key Points to Highlight

### 🔐 Privacy
- Donors can see only requests matched to them
- Requester sees donor identity **only after** they respond Available
- Non-willing donors remain anonymous
- System never leaks non-available donor identity

### 🎯 Smart Matching
- Red-cell compatibility checked automatically
- Eligibility verified (56-day interval)
- Location-based prioritization
- Targeted notifications (no spam)

### ⚡ Real-Time
- Request triggers immediate matching
- Notifications appear instantly
- Status updates live

### 🏥 Medical Responsibility
- All eligibility is informational
- Blood donation center's medical screening always prevails
- Clear disclaimer in the UI

---

## Demo Users Reference

For switching during the demo:

| User | Blood | City | Profile | Request |
|------|-------|------|---------|---------|
| **Priya Sharma** | O+ | Bengaluru | ✅ Donor | — |
| **Arjun Mehta** | O+ | Bengaluru | ✅ Donor | Create requests |
| **Fatima Khan** | A+ | Hyderabad | ✅ Donor | — |
| **Liam O'Connor** | B+ | Dublin | ✅ Donor | — |
| **Wei Zhang** | AB+ | Singapore | ✅ Donor | — |
| **Sofia Rossi** | O- | Milan | ✅ Donor | — |
| **Noah Williams** | A- | Bengaluru | ✅ Donor | — |
| **Ana Costa** | B- | Lisbon | ✅ Donor | — |

---

## Under the Hood (Technical Demo)

If your audience is technical, you can also show:

### API & Swagger

1. Open **http://localhost:5030/swagger** in a new tab
2. Explore all 16 endpoints
3. Try a few with **"Try it out"**:
   - `GET /api/demo-users` → Lists all demo accounts
   - `GET /api/me` (with `X-Demo-User: demo-priya` header) → Current user
   - `GET /api/profile` → Priya's donor profile
   - `GET /api/matches` → Requests matched to Priya

### Database

1. Open **File Explorer** → navigate to `C:\Users\ashwakumar\OneDrive - Microsoft\Documents\Hack\BloodConnect\src\BloodConnect.Api`
2. You'll see **bloodconnect.db** (SQLite file)
3. This is the local database with all seeded data
4. In production, this would be Azure SQL

### Architecture

You can also discuss:
- **Frontend**: React + TypeScript, responsive design, Teams-js integration
- **Backend**: ASP.NET Core, EF Core ORM, clean layered architecture
- **Teams**: Works as a personal tab; manifest included
- **Deployment**: Docker-ready, Azure App Service compatible

---

## Common Demo Questions

**Q: Can I test with real credentials?**  
A: In production, yes — replace demo auth with Entra ID (see README.md §5). Locally, use the 8 demo accounts.

**Q: What if I break something?**  
A: The database resets on every backend restart (Development environment auto-seeds). Just restart `dotnet run`.

**Q: Can I modify the blood compatibility rules?**  
A: Yes! See `BloodCompatibility.cs` in `src/BloodConnect.Domain`. The rules are data-driven and testable.

**Q: How do I deploy to Teams?**  
A: Run `cd teams-app && powershell -ExecutionPolicy Bypass -File .\package.ps1` to create `BloodConnect.teams.zip`, then sideload in Teams. See README.md §9.

**Q: What about notifications in Teams?**  
A: In production, configure an Incoming Webhook URL in the API settings. Locally, notifications are stored in the database and visible in the UI inbox. See README.md for Entra ID + webhook setup.

---

## Demo Checklist

- [ ] Both services running (backend on 5030, frontend on 5173)
- [ ] http://localhost:5173 loading without errors
- [ ] Demo user switcher works (try switching to Priya, Arjun, etc.)
- [ ] Can create/edit donor profile
- [ ] Can create blood request (triggers notifications to matched donors)
- [ ] Can switch to a matched donor and see notifications
- [ ] Can respond Available/Not Available
- [ ] Can view responses with conditional identity reveal
- [ ] Swagger docs accessible at http://localhost:5030/swagger
- [ ] No console errors in browser DevTools

---

## Troubleshooting

**Frontend won't load?**
- Ensure `npm.cmd run dev` is running in the client directory
- Check port 5173 is free: `netstat -ano | findstr :5173`

**API calls failing?**
- Ensure `dotnet run` is running in `src\BloodConnect.Api`
- Check port 5030 is free: `netstat -ano | findstr :5030`
- Check browser console for CORS errors (shouldn't happen locally)

**Database issues?**
- Delete `bloodconnect.db` and restart backend (auto-recreate + reseed)
- All migrations are automatic on startup

**Demo user not switching?**
- Hard refresh: Ctrl+F5 (clears cache)
- Check browser console for network errors

---

## Summary

BloodConnect is a **production-ready, privacy-first employee blood donor network** for Microsoft Teams. This demo shows:

✅ Multi-user system with identity switching  
✅ Smart blood type matching (compatible donors only)  
✅ Privacy guarantees (no identity leaks before consent)  
✅ Real-time notifications and responses  
✅ Eligibility tracking (56-day minimum interval)  
✅ Full request lifecycle (create → match → respond → fulfill)  

**Ready to try it?** Open http://localhost:5173 and start the demo! 🚀
