import { auth } from '@/auth'
import { NextRequest, NextResponse } from 'next/server'

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
    public details?: unknown
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

function getBackendUrl() {
  return process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000'
}

export async function forwardToBackend(
  path: string,
  options: RequestInit = {}
): Promise<Response> {
  const session = await auth()
  const backendUrl = getBackendUrl()

  const headers: Record<string, string> = {
    'Content-Type': 'application/json',
  }

  if (options.headers) {
    const existingHeaders = new Headers(options.headers)
    existingHeaders.forEach((value, key) => {
      headers[key] = value
    })
  }

  if (session?.user) {
    headers['X-User-Id'] = session.user.id
    headers['X-User-Email'] = session.user.email || ''
    headers['X-User-Role'] = session.user.role
  }

  const url = `${backendUrl}${path}`

  const response = await fetch(url, {
    ...options,
    headers,
  })

  return response
}

export async function handleApiRoute(
  handler: () => Promise<NextResponse>
): Promise<NextResponse> {
  try {
    return await handler()
  } catch (error) {
    console.error('API route error:', error)

    if (error instanceof ApiError) {
      return NextResponse.json(
        {
          error: error.message,
          details: error.details,
        },
        { status: error.status }
      )
    }

    return NextResponse.json(
      { error: 'Internal server error' },
      { status: 500 }
    )
  }
}

export async function getRequestBody<T = unknown>(
  request: NextRequest
): Promise<T> {
  try {
    return await request.json()
  } catch {
    throw new ApiError(400, 'Invalid JSON in request body')
  }
}

export async function requireAuthForApi() {
  const session = await auth()
  if (!session?.user) {
    throw new ApiError(401, 'Unauthorized')
  }
  return session.user
}

export async function requireRoleForApi(role: string) {
  const user = await requireAuthForApi()
  if (user.role !== role) {
    throw new ApiError(403, 'Forbidden')
  }
  return user
}

export async function handleBackendError(
  response: Response
): Promise<NextResponse> {
  let errorData
  try {
    errorData = await response.json()
  } catch {
    errorData = { error: 'Unknown error' }
  }

  return NextResponse.json(errorData, { status: response.status })
}

export async function proxyGet(path: string) {
  return handleApiRoute(async () => {
    const response = await forwardToBackend(path, {
      method: 'GET',
    })

    if (!response.ok) {
      return handleBackendError(response)
    }

    const data = await response.json()
    return NextResponse.json(data)
  })
}

export async function proxyPost(path: string, request: NextRequest) {
  return handleApiRoute(async () => {
    const body = await getRequestBody(request)

    const response = await forwardToBackend(path, {
      method: 'POST',
      body: JSON.stringify(body),
    })

    if (!response.ok) {
      return handleBackendError(response)
    }

    const data = await response.json()
    return NextResponse.json(data, { status: response.status })
  })
}

export async function proxyPut(path: string, request: NextRequest) {
  return handleApiRoute(async () => {
    const body = await getRequestBody(request)

    const response = await forwardToBackend(path, {
      method: 'PUT',
      body: JSON.stringify(body),
    })

    if (!response.ok) {
      return handleBackendError(response)
    }

    const data = await response.json()
    return NextResponse.json(data)
  })
}

export async function proxyDelete(path: string) {
  return handleApiRoute(async () => {
    const response = await forwardToBackend(path, {
      method: 'DELETE',
    })

    if (!response.ok) {
      return handleBackendError(response)
    }

    if (response.status === 204) {
      return new NextResponse(null, { status: 204 })
    }

    const data = await response.json()
    return NextResponse.json(data)
  })
}
