import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { ApiClient } from '../api/client'
import type { CurrentUser, DemoUserOption } from '../api/types'
import { AuthContext, type AuthContextValue } from './AuthContextDefinition'

const DEMO_USER_STORAGE_KEY = 'bloodconnect.demoUserId'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [selectedDemoUserId, setSelectedDemoUserId] = useState<string | null>(() =>
    window.localStorage.getItem(DEMO_USER_STORAGE_KEY),
  )
  const [demoUsers, setDemoUsers] = useState<DemoUserOption[]>([])
  const [currentUser, setCurrentUser] = useState<CurrentUser | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  const apiClient = useMemo(
    () => new ApiClient({ getDemoUserId: () => selectedDemoUserId }),
    [selectedDemoUserId],
  )

  useEffect(() => {
    const controller = new AbortController()
    apiClient
      .get<DemoUserOption[]>('/api/demo-users', controller.signal)
      .then(setDemoUsers)
      .catch((err: unknown) => {
        if (!controller.signal.aborted) {
          setError(err instanceof Error ? err.message : 'Failed to load demo identities.')
        }
      })
    return () => controller.abort()
  }, [apiClient])

  const refreshCurrentUser = useMemo(
    () => async () => {
      if (!selectedDemoUserId) {
        setCurrentUser(null)
        setIsLoading(false)
        return
      }
      setIsLoading(true)
      setError(null)
      try {
        const user = await apiClient.get<CurrentUser>('/api/me')
        setCurrentUser(user)
      } catch (err) {
        setCurrentUser(null)
        setError(err instanceof Error ? err.message : 'Failed to load your profile.')
      } finally {
        setIsLoading(false)
      }
    },
    [apiClient, selectedDemoUserId],
  )

  useEffect(() => {
    // Deferred via microtask so the state updates inside refreshCurrentUser happen in a callback
    // rather than synchronously within the effect body itself.
    Promise.resolve().then(() => {
      void refreshCurrentUser()
    })
  }, [refreshCurrentUser])

  const selectDemoUser = (externalId: string) => {
    window.localStorage.setItem(DEMO_USER_STORAGE_KEY, externalId)
    setSelectedDemoUserId(externalId)
  }

  const signOut = () => {
    window.localStorage.removeItem(DEMO_USER_STORAGE_KEY)
    setSelectedDemoUserId(null)
    setCurrentUser(null)
  }

  const value: AuthContextValue = {
    demoUsers,
    selectedDemoUserId,
    currentUser,
    isLoading,
    error,
    selectDemoUser,
    signOut,
    refreshCurrentUser,
    apiClient,
  }

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
