import { createContext } from 'react'
import type { ApiClient } from '../api/client'
import type { CurrentUser, DemoUserOption } from '../api/types'

export interface AuthContextValue {
  /** The list of selectable local demo identities, fetched from the API. */
  demoUsers: DemoUserOption[]
  /** The currently selected demo identity's external id (e.g. "demo-priya"), or null if none selected. */
  selectedDemoUserId: string | null
  /** The resolved current user profile from the server, or null while loading/unauthenticated. */
  currentUser: CurrentUser | null
  isLoading: boolean
  error: string | null
  selectDemoUser: (externalId: string) => void
  signOut: () => void
  refreshCurrentUser: () => Promise<void>
  apiClient: ApiClient
}

export const AuthContext = createContext<AuthContextValue | undefined>(undefined)
