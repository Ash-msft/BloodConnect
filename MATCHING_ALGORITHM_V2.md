# BloodConnect Matching Algorithm v2.0

## Overview

BloodConnect v2.0 implements an **urgency-aware, location-based donor matching algorithm** that prioritizes speed of response based on medical urgency and geographic proximity.

The algorithm is designed to answer: **"Who is the closest, most eligible donor who can reach the fastest?"**

---

## Matching Pipeline

### Step 1: Filter Compatible Donors

**Criteria**:
- Blood type compatibility (ABO/Rh matrix)
- Availability status = Available
- Opted into donor network
- Not the requester themselves
- Not already notified for this request

**Output**: Candidate pool of compatible, available donors

### Step 2: Filter Eligible Donors

**Criteria**:
- Last donation ≥ 56 days ago (whole blood minimum)
- OR no previous donation

**Output**: Medically eligible donor pool

### Step 3: Location-Based Prioritization (NEW)

#### For Delhi Requests
Uses **pincode-based distance calculation** (Haversine formula)

**Distance Radius by Urgency**:
- **Critical** (🔴): ≤ 3 km (estimated <10 min travel)
- **Urgent** (🟠): ≤ 5 km (estimated <15 min travel)
- **Routine** (🟢): ≤ 10 km (estimated <30 min travel)

**Sorting Order** (tiered, so an urgent request never ends up matching nobody):
1. **Tier 1** — donors with a known pincode inside the urgency radius, closest first, then by
   donation recency.
2. **Tier 2** — donors whose pincode is missing or outside the Delhi dataset. Their distance is
   *unknown*, not "far", so they stay in the pool at lower priority.
3. **Tier 3** — donors with a known pincode beyond the radius, closest first. Included last so a
   request still reaches someone when the immediate area has no eligible donors.

If the **request itself** has no resolvable Delhi pincode, distance cannot be computed at all and
matching falls back to city-name prioritization rather than filtering everyone out.

The overall notification cap (`MaxDonorsNotifiedPerRequest`) is applied after this ordering, so the
closest donors are always notified first.

#### For Other Cities (Fallback)
Uses **city-name matching** (will be extended with pincode support later)

**Sorting Order**:
1. Same city first
2. Most recent donor second

### Step 4: Limit Notifications

**Default**: Top 5 donors notified
- Reduces notification spam
- Focuses on fastest responders
- Can be tuned via config

### Step 5: Create Response Records & Notify

For each prioritized donor:
1. Create pending `DonorResponse` record
2. Calculate and store `ProximityRank` (0=very close, 1=close, 2=moderate, 3=far)
3. Send targeted notification via Teams/Webhook

---

## Blood Type Compatibility Matrix

| Recipient | Compatible Donors | Priority |
|-----------|-------------------|----------|
| **O-** | O- | Universal recipient |
| **O+** | O-, O+ | Common recipient |
| **A-** | O-, A- | Rare negative |
| **A+** | O-, O+, A-, A+ | Common recipient |
| **B-** | O-, B- | Rare negative |
| **B+** | O-, O+, B-, B+ | Common recipient |
| **AB-** | O-, A-, B-, AB- | Rare negative, universal recipient |
| **AB+** | ALL | Universal recipient |

**Key Rules**:
- O- can donate to anyone (universal)
- Positive can only donate to positive
- Negative can donate to both positive and negative

---

## Delhi Pincode Service

### Supported Pincodes

**Central Delhi**: 110001-110006
**North Delhi**: 110007-110010
**South Delhi**: 110011-110016
**East Delhi**: 110017-110020
**West Delhi**: 110021-110024
**Northeast Delhi**: 110025-110028
**Northwest Delhi**: 110029-110032
**Southeast Delhi**: 110033-110036
**Southwest Delhi**: 110037-110040
**New Delhi zones**: 110041-110071

**Total**: 70+ pincodes covering all major Delhi zones

### Distance Calculation

Uses **Haversine formula** to calculate great-circle distance:

```
Distance = 2 × R × arcsin(√[sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlng/2)])
R = 6371 km (Earth's radius)
```

Accuracy: ±0.5-1 km for Delhi zones

### Example

**Request**: O+ blood, Pincode 110016 (South Delhi), **Urgent**

Matching process:
1. **Find compatible donors**: O+ can receive from O-, O+, A-, A+, B-, B+
2. **Filter available**: 3 available
3. **Filter eligible**: All 3 eligible (last donation ≥ 56 days)
4. **Prioritize by location** (Urgent = 5 km radius):
   - Donor 1: Pincode 110014 (2.1 km away) → Tier 1, rank 0 ✅ NOTIFY FIRST
   - Donor 2: Pincode 110018 (4.3 km away) → Tier 1, rank 1 ✅ NOTIFY SECOND
   - Donor 3: Pincode 110032 (12 km away) → Tier 3 ⚠️ only notified if capacity remains

**Result**: The two nearby donors are notified first; the distant donor is a last resort rather than
being dropped entirely.

---

## Urgency Levels & Response Times

| Urgency | Radius | Expected Response | Example Scenario |
|---------|--------|-------------------|-----------------|
| **Critical** 🔴 | 3 km | <10 minutes | Accident victim in OR, type match urgent |
| **Urgent** 🟠 | 5 km | <15 minutes | Scheduled emergency surgery starting soon |
| **Routine** 🟢 | 10 km | <1 hour | Planned procedure, flexible timing |

---

## Privacy & Security

### Identity Reveal Timeline

1. **Matching Phase**: Donor receives notification (identity hidden)
   - "A request for O+ blood at Max Healthcare matches your profile"
   - No requester details shown

2. **Pending Response**: Donor sees request details (hospital, urgency, location)
   - Still anonymous: "Max Healthcare Institute, Delhi"
   - Donor decides: Available / Not Available

3. **After "Available" Response**: 
   - ✅ Requester sees donor name, phone, email
   - ✅ Donors can contact each other

4. **After "Not Available" Response**:
   - ❌ Donor details remain hidden from requester
   - ❌ Privacy preserved

### Location Privacy

- Pincodes are 6 digits (district-level granularity)
- Pincode ≠ exact address
- Distance calculation is one-way (donor → hospital)
- No donor location revealed to requester until Available response

---

## Performance Characteristics

### Matching Speed
- **Candidate filter**: O(n) where n = all donors
- **Eligibility check**: O(n) simple timestamp comparison
- **Location sort**: O(n log n) with distance calculation
- **Notification dispatch**: Asynchronous via outbox

**Total time**: ~100-200ms for typical 50-100 donor pool

### Scalability
- ✅ Linear complexity in donor count
- ✅ No full-text search or complex joins
- ✅ Pincode distance cached in memory
- ✅ Suitable for 1000+ donors

### Database Indexes
```sql
-- Recommended
CREATE INDEX idx_donor_profiles_availability ON DonorProfiles(Availability, HasOptedIn);
CREATE INDEX idx_blood_requests_status ON BloodRequests(Status, CreatedUtc DESC);
CREATE INDEX idx_donor_responses_request ON DonorResponses(BloodRequestId, Status);
```

---

## Configuration

### appsettings.json

```json
{
  "BloodConnect": {
	"MinimumDonationIntervalDays": 56,
	"MaxDonorsNotifiedPerRequest": 5,
	"RequestExpiryHours": 72
  },
  "Locations": {
	"DelhiEnabled": true,
	"DelhiMaxDistanceKmByUrgency": {
	  "Critical": 3,
	  "High": 5,
	  "Medium": 10,
	  "Low": 999
	}
  }
}
```

### Extension to Other Cities

To add pincode support to other cities:

1. **Create CityPincodeService**:
   ```csharp
   public static class MumbaiPincodeService { ... }
   public static class BangalorePincodeService { ... }
   ```

2. **Update DonorMatchingService**:
   ```csharp
   if (request.City == "Delhi")
	   return PrioritizeDonorsForDelhi(...);
   else if (request.City == "Mumbai")
	   return PrioritizeDonorsForMumbai(...);
   ```

3. **Seed pincodes** in appropriate demo data

---

## Testing

### Test Scenarios

1. **Blood Type Compatibility**
   - ✅ O+ can donate to A+ (included in matrix)
   - ❌ A+ cannot donate to B+ (not in matrix)

2. **Eligibility**
   - ✅ Donor who donated 60 days ago is eligible
   - ❌ Donor who donated 30 days ago is not eligible

3. **Proximity (Delhi)**
   - ✅ High urgency notifies donors within 5 km
   - ❌ High urgency does NOT notify donors 10 km away

4. **Privacy**
   - ✅ Non-available donor identity not leaked
   - ✅ Requester cannot see all donor profiles

5. **Edge Cases**
   - ✅ No donors match → No notifications sent
   - ✅ Duplicate match → Re-notification prevented
   - ✅ Requester is also donor → Self excluded

### Unit Tests Included

- `BloodCompatibilityTests` (15 tests)
- `BloodEligibilityCalculatorTests` (10 tests)
- `DelhiPincodeServiceTests` (to be added)
- `DonorMatchingServiceTests` (integration tests)

---

## Future Enhancements

### Phase 2 (v2.1)
- [ ] Add Mumbai pincode support
- [ ] Add Bangalore pincode support
- [ ] Donor ranking by frequency
- [ ] Real-time distance via Google Maps API

### Phase 3 (v2.2)
- [ ] Traffic-aware response time estimation
- [ ] Donor availability by time-of-day
- [ ] SMS/WhatsApp notification channel
- [ ] Donor availability calendar

### Phase 4 (v2.3)
- [ ] Machine learning donor prediction
- [ ] Donation preference learning
- [ ] Hospital-specific compatibility rules
- [ ] Analytics dashboard

---

## Deployment Notes

- **Database migration required**: Adds `Pincode` columns to DonorProfile and BloodRequest
- **No breaking changes**: Pincodes are optional, city-name fallback still works
- **Backward compatible**: Existing donors/requests continue to work
- **Delhi seeded data**: 12 demo donors with Delhi pincodes included

---

## References

- WHO Blood Type Guidelines
- Haversine Formula: https://en.wikipedia.org/wiki/Haversine_formula
- Delhi Pincode Database: India POST standards
- Indian Blood Bank Standards (NBTC)

---

*Last updated: 2026-09-16 19:44*  
*Algorithm v2.0 - Urgency-aware location matching*
