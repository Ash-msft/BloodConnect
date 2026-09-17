import { useEffect, useState } from 'react'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type {
  DonationHistoryEntry,
  DonorProfile,
  UpsertDonorProfileRequest,
} from '../api/types'
import { BLOOD_GROUPS, formatBloodGroup } from '../api/types'
import { ErrorState, LoadingState } from '../components/StatusStates'
import { formatDate } from '../utils/eligibility'

const EMPTY_FORM: UpsertDonorProfileRequest = {
  bloodGroup: 'OPositive',
  city: '',
  pincode: null,
  availability: 'Available',
  lastDonationUtc: null,
  contactPreference: 'TeamsChat',
  contactPhone: null,
  hasOptedIn: false,
}

export function ProfilePage() {
  const { apiClient, refreshCurrentUser } = useAuth()
  const [form, setForm] = useState<UpsertDonorProfileRequest>(EMPTY_FORM)
  const [profile, setProfile] = useState<DonorProfile | null>(null)
  const [history, setHistory] = useState<DonationHistoryEntry[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [saveError, setSaveError] = useState<string | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [savedMessage, setSavedMessage] = useState<string | null>(null)
  const [donationDate, setDonationDate] = useState('')
  const [donationNotes, setDonationNotes] = useState('')
  const [donationError, setDonationError] = useState<string | null>(null)

  useEffect(() => {
    const controller = new AbortController()

    Promise.resolve().then(() => {
      setIsLoading(true)
      setLoadError(null)

      Promise.all([
        apiClient.get<DonorProfile>('/api/profile', controller.signal).catch((err: unknown) => {
          if (err instanceof ApiError && err.status === 404) {
            return null
          }
          throw err
        }),
        apiClient.get<DonationHistoryEntry[]>('/api/donations', controller.signal),
      ])
        .then(([loadedProfile, loadedHistory]) => {
          if (loadedProfile) {
            setProfile(loadedProfile)
            setForm({
              bloodGroup: loadedProfile.bloodGroup,
              city: loadedProfile.city,
              pincode: loadedProfile.pincode,
              availability: loadedProfile.availability,
              lastDonationUtc: loadedProfile.lastDonationUtc,
              contactPreference: loadedProfile.contactPreference,
              contactPhone: loadedProfile.contactPhone,
              hasOptedIn: loadedProfile.hasOptedIn,
            })
          }
          setHistory(loadedHistory)
        })
        .catch((err: unknown) => {
          if (!controller.signal.aborted) {
            setLoadError(err instanceof Error ? err.message : 'Failed to load your profile.')
          }
        })
        .finally(() => setIsLoading(false))
    })

    return () => controller.abort()
  }, [apiClient])

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setSaveError(null)
    setSavedMessage(null)

    if (!form.city.trim()) {
      setSaveError('City is required.')
      return
    }
    if (form.contactPreference === 'Phone' && !form.contactPhone?.trim()) {
      setSaveError('A contact phone number is required when phone is your preferred contact method.')
      return
    }

    setIsSaving(true)
    try {
      const updated = await apiClient.put<DonorProfile>('/api/profile', form)
      setProfile(updated)
      setSavedMessage('Your donor profile has been saved.')
      await refreshCurrentUser()
    } catch (err) {
      setSaveError(err instanceof ApiError ? err.message : 'Failed to save your profile.')
    } finally {
      setIsSaving(false)
    }
  }

  async function handleRecordDonation(event: React.FormEvent) {
    event.preventDefault()
    setDonationError(null)

    if (!donationDate) {
      setDonationError('Donation date is required.')
      return
    }

    try {
      await apiClient.post('/api/donations', {
        donationDateUtc: new Date(donationDate).toISOString(),
        notes: donationNotes.trim() || null,
        fulfilledRequestId: null,
      })
      const [updatedProfile, updatedHistory] = await Promise.all([
        apiClient.get<DonorProfile>('/api/profile'),
        apiClient.get<DonationHistoryEntry[]>('/api/donations'),
      ])
      setProfile(updatedProfile)
      setForm((prev) => ({ ...prev, lastDonationUtc: updatedProfile.lastDonationUtc }))
      setHistory(updatedHistory)
      setDonationDate('')
      setDonationNotes('')
    } catch (err) {
      setDonationError(err instanceof ApiError ? err.message : 'Failed to record your donation.')
    }
  }

  if (isLoading) {
    return <LoadingState label="Loading your profile…" />
  }

  if (loadError) {
    return <ErrorState message={loadError} />
  }

  return (
    <div className="page">
      <h1>My Donor Profile</h1>
      <p className="page__subtitle">
        This profile is private. It is only ever shared with a requester if you affirmatively respond
        "Available" to one of their specific requests.
      </p>

      <form className="card form" onSubmit={handleSubmit} aria-label="Donor profile form">
        <div className="form-row">
          <label htmlFor="bloodGroup">Blood group</label>
          <select
            id="bloodGroup"
            value={form.bloodGroup}
            onChange={(e) => setForm({ ...form, bloodGroup: e.target.value as UpsertDonorProfileRequest['bloodGroup'] })}
          >
            {BLOOD_GROUPS.map((group) => (
              <option key={group} value={group}>
                {formatBloodGroup(group)}
              </option>
            ))}
          </select>
        </div>

        <div className="form-row">
          <label htmlFor="city">City / location</label>
          <input
            id="city"
            type="text"
            value={form.city}
            maxLength={120}
            onChange={(e) => setForm({ ...form, city: e.target.value })}
            required
          />
        </div>

        <div className="form-row">
          <label htmlFor="pincode">Pincode (optional)</label>
          <input
            id="pincode"
            type="text"
            inputMode="numeric"
            value={form.pincode ?? ''}
            maxLength={10}
            placeholder="e.g. 110016"
            onChange={(e) => setForm({ ...form, pincode: e.target.value.trim() === '' ? null : e.target.value })}
          />
          <p className="form-hint">
            Adding your pincode lets BloodConnect notify you only about requests near you. Currently
            supported for Delhi.
          </p>
        </div>

        <div className="form-row">
          <label htmlFor="availability">Availability</label>
          <select
            id="availability"
            value={form.availability}
            onChange={(e) =>
              setForm({ ...form, availability: e.target.value as UpsertDonorProfileRequest['availability'] })
            }
          >
            <option value="Available">Available</option>
            <option value="Unavailable">Unavailable</option>
          </select>
        </div>

        <div className="form-row">
          <label htmlFor="lastDonation">Last donation date</label>
          <input
            id="lastDonation"
            type="date"
            value={form.lastDonationUtc ? form.lastDonationUtc.slice(0, 10) : ''}
            max={new Date().toISOString().slice(0, 10)}
            onChange={(e) =>
              setForm({
                ...form,
                lastDonationUtc: e.target.value ? new Date(e.target.value).toISOString() : null,
              })
            }
          />
        </div>

        <div className="form-row">
          <label htmlFor="contactPreference">Preferred contact method</label>
          <select
            id="contactPreference"
            value={form.contactPreference}
            onChange={(e) =>
              setForm({
                ...form,
                contactPreference: e.target.value as UpsertDonorProfileRequest['contactPreference'],
              })
            }
          >
            <option value="TeamsChat">Teams chat</option>
            <option value="Email">Email</option>
            <option value="Phone">Phone</option>
          </select>
        </div>

        {form.contactPreference === 'Phone' && (
          <div className="form-row">
            <label htmlFor="contactPhone">Contact phone number</label>
            <input
              id="contactPhone"
              type="tel"
              value={form.contactPhone ?? ''}
              maxLength={30}
              onChange={(e) => setForm({ ...form, contactPhone: e.target.value })}
            />
          </div>
        )}

        <div className="form-row form-row--checkbox">
          <input
            id="hasOptedIn"
            type="checkbox"
            checked={form.hasOptedIn}
            onChange={(e) => setForm({ ...form, hasOptedIn: e.target.checked })}
          />
          <label htmlFor="hasOptedIn">
            I opt in to appear in donor matching for compatible blood requests
          </label>
        </div>

        {saveError && (
          <p className="form-error" role="alert">
            {saveError}
          </p>
        )}
        {savedMessage && <p className="form-success">{savedMessage}</p>}

        <button type="submit" className="btn btn--primary" disabled={isSaving}>
          {isSaving ? 'Saving…' : 'Save profile'}
        </button>
      </form>

      {profile && (
        <div className="card">
          <h2>Eligibility</h2>
          <p>
            {profile.isEligibleNow
              ? 'You are currently eligible to donate based on your last donation date.'
              : `You will next be eligible around ${formatDate(profile.nextEligibleUtc)}.`}
          </p>
          <p className="disclaimer-text">
            This is informational only — your donation center's clinical screening always applies.
          </p>
        </div>
      )}

      <div className="card">
        <h2>Donation history</h2>
        <form className="form form--inline" onSubmit={handleRecordDonation} aria-label="Record a donation">
          <div className="form-row">
            <label htmlFor="donationDate">Donation date</label>
            <input
              id="donationDate"
              type="date"
              value={donationDate}
              max={new Date().toISOString().slice(0, 10)}
              onChange={(e) => setDonationDate(e.target.value)}
            />
          </div>
          <div className="form-row">
            <label htmlFor="donationNotes">Notes (optional)</label>
            <input
              id="donationNotes"
              type="text"
              value={donationNotes}
              maxLength={1000}
              onChange={(e) => setDonationNotes(e.target.value)}
            />
          </div>
          {donationError && (
            <p className="form-error" role="alert">
              {donationError}
            </p>
          )}
          <button type="submit" className="btn btn--secondary">
            Record donation
          </button>
        </form>

        {history.length === 0 ? (
          <p className="status-state status-state--empty">No donations recorded yet.</p>
        ) : (
          <ul className="history-list">
            {history.map((entry) => (
              <li key={entry.id}>
                <strong>{formatDate(entry.donationDateUtc)}</strong>
                {entry.notes && <span> — {entry.notes}</span>}
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  )
}
