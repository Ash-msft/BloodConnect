import type { ApiErrorResponse } from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5030'

/**
 * Thrown for any non-2xx API response. Carries the server's error message when available so the UI
 * can show a specific, honest error instead of a generic failure.
 */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
    this.name = 'ApiError'
  }
}

export interface ApiClientOptions {
  getDemoUserId: () => string | null
}

/**
 * Thin fetch wrapper that attaches the local demo identity header (see README "Local demo auth
 * limitations") and normalizes error handling. When Entra ID replaces demo auth, this is the only
 * place that needs to change (attach an Authorization: Bearer token instead of X-Demo-User).
 */
export class ApiClient {
  private readonly options: ApiClientOptions

  constructor(options: ApiClientOptions) {
    this.options = options
  }

  async get<T>(path: string, signal?: AbortSignal): Promise<T> {
    return this.request<T>('GET', path, undefined, signal)
  }

  async post<T>(path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
    return this.request<T>('POST', path, body, signal)
  }

  async put<T>(path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
    return this.request<T>('PUT', path, body, signal)
  }

  private async request<T>(
    method: string,
    path: string,
    body: unknown,
    signal?: AbortSignal,
  ): Promise<T> {
    const headers: Record<string, string> = { Accept: 'application/json' }
    const demoUserId = this.options.getDemoUserId()
    if (demoUserId) {
      headers['X-Demo-User'] = demoUserId
    }
    if (body !== undefined) {
      headers['Content-Type'] = 'application/json'
    }

    const response = await fetch(`${API_BASE_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
      signal,
    })

    if (response.status === 204) {
      return undefined as T
    }

    const text = await response.text()
    const data = text.length > 0 ? JSON.parse(text) : undefined

    if (!response.ok) {
      const errorBody = data as ApiErrorResponse | undefined
      throw new ApiError(response.status, errorBody?.message ?? `Request failed with status ${response.status}.`)
    }

    return data as T
  }
}
