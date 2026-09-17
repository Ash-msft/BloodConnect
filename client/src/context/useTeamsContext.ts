import { useEffect, useState } from 'react'

export interface TeamsHostInfo {
  isHostedInTeams: boolean
  theme: 'default' | 'dark' | 'contrast'
  userDisplayName: string | null
}

const DEFAULT_HOST_INFO: TeamsHostInfo = {
  isHostedInTeams: false,
  theme: 'default',
  userDisplayName: null,
}

/**
 * Initializes @microsoft/teams-js when the app is actually hosted inside a Microsoft Teams client
 * (personal tab), and falls back to plain-browser behavior otherwise. Teams SDK initialization is
 * given a short timeout — if it never resolves (i.e. we're not inside Teams) we proceed as a normal
 * web app rather than blocking the UI.
 */
export function useTeamsContext(): TeamsHostInfo {
  const [hostInfo, setHostInfo] = useState<TeamsHostInfo>(DEFAULT_HOST_INFO)

  useEffect(() => {
    let cancelled = false

    async function init() {
      try {
        const teamsJs = await import('@microsoft/teams-js')
        const initPromise = teamsJs.app.initialize()
        const timeout = new Promise<never>((_, reject) =>
          setTimeout(() => reject(new Error('Not running inside Microsoft Teams.')), 1500),
        )

        await Promise.race([initPromise, timeout])
        if (cancelled) return

        const context = await teamsJs.app.getContext()
        if (cancelled) return

        setHostInfo({
          isHostedInTeams: true,
          theme: (context.app.theme as TeamsHostInfo['theme']) ?? 'default',
          userDisplayName: context.user?.displayName ?? null,
        })

        teamsJs.app.registerOnThemeChangeHandler((theme) => {
          setHostInfo((prev) => ({ ...prev, theme: theme as TeamsHostInfo['theme'] }))
        })
      } catch {
        // Expected when running as a plain browser app (e.g. local development outside Teams).
        if (!cancelled) {
          setHostInfo(DEFAULT_HOST_INFO)
        }
      }
    }

    void init()
    return () => {
      cancelled = true
    }
  }, [])

  return hostInfo
}
