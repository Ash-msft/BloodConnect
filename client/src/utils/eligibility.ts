/** Default whole-blood donation interval in days, matching the backend default (see README). */
export const DEFAULT_MINIMUM_DONATION_INTERVAL_DAYS = 56

/**
 * Client-side mirror of the backend eligibility rule, used only for immediate UI feedback while a
 * donor edits their last-donation date. The server always recomputes and enforces eligibility
 * independently — this must never be trusted as the source of truth.
 */
export function isEligibleToDonate(
  lastDonationUtc: string | null,
  asOf: Date = new Date(),
  minimumIntervalDays: number = DEFAULT_MINIMUM_DONATION_INTERVAL_DAYS,
): boolean {
  if (!lastDonationUtc) {
    return true
  }
  const nextEligible = getNextEligibleDate(lastDonationUtc, minimumIntervalDays)
  return asOf >= nextEligible
}

export function getNextEligibleDate(
  lastDonationUtc: string,
  minimumIntervalDays: number = DEFAULT_MINIMUM_DONATION_INTERVAL_DAYS,
): Date {
  const lastDonation = new Date(lastDonationUtc)
  const next = new Date(lastDonation)
  next.setUTCDate(next.getUTCDate() + minimumIntervalDays)
  return next
}

export function formatDate(value: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })
}

export function formatDateTime(value: string | null): string {
  if (!value) return '—'
  return new Date(value).toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: 'numeric',
    minute: '2-digit',
  })
}
