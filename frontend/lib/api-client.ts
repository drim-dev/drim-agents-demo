import { ApiClientError, ProblemDetails } from '@/types/api'

async function apiFetch(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const response = await fetch(path, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options.headers,
    },
  })

  if (!response.ok) {
    let problemDetails: ProblemDetails | undefined
    try {
      problemDetails = await response.json()
    } catch {
      // Ignore JSON parse errors
    }

    throw new ApiClientError(
      response.status,
      problemDetails?.title || 'API request failed',
      problemDetails
    )
  }

  return response
}

export async function apiGet<T>(path: string): Promise<T> {
  const response = await apiFetch(path, {
    method: 'GET',
  })

  return response.json()
}

export async function apiPost<TResponse = unknown, TBody = unknown>(
  path: string,
  body?: TBody
): Promise<TResponse> {
  const response = await apiFetch(path, {
    method: 'POST',
    body: body ? JSON.stringify(body) : undefined,
  })

  if (response.status === 204) {
    return undefined as TResponse
  }

  return response.json()
}

export async function apiPut<TResponse = unknown, TBody = unknown>(
  path: string,
  body?: TBody
): Promise<TResponse> {
  const response = await apiFetch(path, {
    method: 'PUT',
    body: body ? JSON.stringify(body) : undefined,
  })

  return response.json()
}

export async function apiPatch<TResponse = unknown, TBody = unknown>(
  path: string,
  body?: TBody
): Promise<TResponse> {
  const response = await apiFetch(path, {
    method: 'PATCH',
    body: body ? JSON.stringify(body) : undefined,
  })

  return response.json()
}

export async function apiDelete<T = void>(path: string): Promise<T> {
  const response = await apiFetch(path, {
    method: 'DELETE',
  })

  if (response.status === 204) {
    return undefined as T
  }

  return response.json()
}
