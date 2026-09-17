import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/useAuth'
import { ApiError } from '../api/client'
import type { BloodRequest, CreateBloodRequest } from '../api/types'
import { BLOOD_GROUPS, formatBloodGroup } from '../api/types'

const EMPTY_FORM: CreateBloodRequest = {
  bloodGroup: 'OPositive',
  hospitalName: '',
  city: '',
  pincode: null,
  unitsNeeded: 1,
  urgency: 'Routine',
  notes: null,
}

export function CreateRequestPage() {
  const { apiClient } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState<CreateBloodRequest>(EMPTY_FORM)
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: React.FormEvent) {
    event.preventDefault()
    setError(null)

    if (!form.hospitalName.trim() || !form.city.trim()) {
      setError('Hospital name and city are required.')
      return
    }
    if (form.unitsNeeded < 1 || form.unitsNeeded > 50) {
      setError('Units needed must be between 1 and 50.')
      return
    }

    setIsSubmitting(true)
    try {
      const created = await apiClient.post<BloodRequest>('/api/requests', form)
      navigate(`/requests/${created.id}`)
    } catch (err) {
      setError(err instanceof ApiError ? err.message : 'Failed to create the blood request.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <div className="page">
      <h1>Create a Blood Request</h1>
      <p className="page__subtitle">
        Compatible, available, and currently eligible donors will be notified automatically. No
        organization-wide broadcast is sent.
      </p>

      <form className="card form" onSubmit={handleSubmit} aria-label="Create blood request form">
        <div className="form-row">
          <label htmlFor="bloodGroup">Blood group needed</label>
          <select
            id="bloodGroup"
            value={form.bloodGroup}
            onChange={(e) => setForm({ ...form, bloodGroup: e.target.value as CreateBloodRequest['bloodGroup'] })}
          >
            {BLOOD_GROUPS.map((group) => (
              <option key={group} value={group}>
                {formatBloodGroup(group)}
              </option>
            ))}
          </select>
        </div>

        <div className="form-row">
          <label htmlFor="hospitalName">Hospital name</label>
          <input
            id="hospitalName"
            type="text"
            value={form.hospitalName}
            maxLength={200}
            onChange={(e) => setForm({ ...form, hospitalName: e.target.value })}
            required
          />
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
          <label htmlFor="pincode">Hospital pincode (optional)</label>
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
            Supplying the hospital pincode narrows notifications to the donors who can reach it
            fastest. Currently supported for Delhi.
          </p>
        </div>

        <div className="form-row">
          <label htmlFor="unitsNeeded">Units needed</label>
          <input
            id="unitsNeeded"
            type="number"
            min={1}
            max={50}
            value={form.unitsNeeded}
            onChange={(e) => setForm({ ...form, unitsNeeded: Number(e.target.value) })}
            required
          />
        </div>

        <div className="form-row">
          <label htmlFor="urgency">Urgency</label>
          <select
            id="urgency"
            value={form.urgency}
            onChange={(e) => setForm({ ...form, urgency: e.target.value as CreateBloodRequest['urgency'] })}
          >
            <option value="Routine">Routine</option>
            <option value="Urgent">Urgent</option>
            <option value="Critical">Critical</option>
          </select>
        </div>

        <div className="form-row">
          <label htmlFor="notes">Additional notes (optional)</label>
          <textarea
            id="notes"
            value={form.notes ?? ''}
            maxLength={1000}
            rows={4}
            onChange={(e) => setForm({ ...form, notes: e.target.value || null })}
          />
        </div>

        {error && (
          <p className="form-error" role="alert">
            {error}
          </p>
        )}

        <button type="submit" className="btn btn--primary" disabled={isSubmitting}>
          {isSubmitting ? 'Creating…' : 'Create request'}
        </button>
      </form>
    </div>
  )
}
