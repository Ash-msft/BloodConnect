// Mirrors the backend enums/DTOs (see BloodConnect.Api.Dtos). Enum values are serialized as strings
// by the API's JsonStringEnumConverter, so string literal unions are used here directly.

export type BloodGroup =
  | 'OPositive'
  | 'ONegative'
  | 'APositive'
  | 'ANegative'
  | 'BPositive'
  | 'BNegative'
  | 'ABPositive'
  | 'ABNegative'

export const BLOOD_GROUPS: BloodGroup[] = [
  'OPositive',
  'ONegative',
  'APositive',
  'ANegative',
  'BPositive',
  'BNegative',
  'ABPositive',
  'ABNegative',
]

export function formatBloodGroup(group: BloodGroup): string {
  const map: Record<BloodGroup, string> = {
    OPositive: 'O+',
    ONegative: 'O-',
    APositive: 'A+',
    ANegative: 'A-',
    BPositive: 'B+',
    BNegative: 'B-',
    ABPositive: 'AB+',
    ABNegative: 'AB-',
  }
  return map[group]
}

export type AvailabilityStatus = 'Available' | 'Unavailable'

export type ContactPreference = 'TeamsChat' | 'Email' | 'Phone'

export type UrgencyLevel = 'Routine' | 'Urgent' | 'Critical'

export type RequestStatus = 'Open' | 'Fulfilled' | 'Cancelled' | 'Expired'

export type ResponseStatus = 'Pending' | 'Available' | 'NotAvailable'

export type NotificationStatus = 'Pending' | 'Sent' | 'Failed'

export type NotificationType =
  | 'NewMatchingRequest'
  | 'DonorAvailableResponse'
  | 'RequestFulfilled'
  | 'RequestCancelled'

export interface CurrentUser {
  userId: string
  displayName: string
  email: string
  hasDonorProfile: boolean
}

export interface DemoUserOption {
  externalId: string
  displayName: string
}

export interface DonorProfile {
  bloodGroup: BloodGroup
  city: string
  pincode: string | null
  availability: AvailabilityStatus
  lastDonationUtc: string | null
  contactPreference: ContactPreference
  contactPhone: string | null
  hasOptedIn: boolean
  isEligibleNow: boolean
  nextEligibleUtc: string | null
  locationZone: string | null
}

export interface UpsertDonorProfileRequest {
  bloodGroup: BloodGroup
  city: string
  pincode: string | null
  availability: AvailabilityStatus
  lastDonationUtc: string | null
  contactPreference: ContactPreference
  contactPhone: string | null
  hasOptedIn: boolean
}

export interface DonationHistoryEntry {
  id: string
  donationDateUtc: string
  notes: string | null
  fulfilledRequestId: string | null
}

export interface RecordDonationRequest {
  donationDateUtc: string
  notes: string | null
  fulfilledRequestId: string | null
}

export interface CreateBloodRequest {
  bloodGroup: BloodGroup
  hospitalName: string
  city: string
  pincode: string | null
  unitsNeeded: number
  urgency: UrgencyLevel
  notes: string | null
}

export interface BloodRequest {
  id: string
  bloodGroup: BloodGroup
  hospitalName: string
  city: string
  pincode: string | null
  unitsNeeded: number
  urgency: UrgencyLevel
  notes: string | null
  status: RequestStatus
  createdUtc: string
  expiresUtc: string
  matchedDonorCount: number
  availableDonorCount: number
  isOwnRequest: boolean
  locationZone: string | null
  searchRadiusKm: number
}

export interface DonorResponseForRequester {
  responseId: string
  status: ResponseStatus
  proximityRank: number
  notifiedUtc: string
  respondedUtc: string | null
  donorDisplayName: string | null
  donorEmail: string | null
  donorPhone: string | null
  donorContactPreference: ContactPreference | null
}

export interface MatchedRequest {
  requestId: string
  bloodGroup: BloodGroup
  hospitalName: string
  city: string
  unitsNeeded: number
  urgency: UrgencyLevel
  notes: string | null
  myResponseStatus: ResponseStatus
  responseId: string
  notifiedUtc: string
}

export interface NotificationItem {
  id: string
  type: NotificationType
  bloodRequestId: string | null
  title: string
  body: string
  adaptiveCardJson: string
  status: NotificationStatus
  channel: string
  deliveryDetail: string | null
  createdUtc: string
  readByRecipient: boolean
}

export interface ApiErrorResponse {
  message: string
}
