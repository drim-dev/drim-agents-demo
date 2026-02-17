import { NextRequest } from 'next/server'
import { proxyGet, proxyPost } from '@/lib/api'

export async function GET(
  request: NextRequest,
  { params }: { params: Promise<{ taskId: string }> }
) {
  const { taskId } = await params
  const searchParams = request.nextUrl.searchParams
  const query = searchParams.toString()
  const path = `/api/tasks/${taskId}/chat/messages${query ? `?${query}` : ''}`
  return proxyGet(path)
}

export async function POST(
  request: NextRequest,
  { params }: { params: Promise<{ taskId: string }> }
) {
  const { taskId } = await params
  return proxyPost(`/api/tasks/${taskId}/chat/messages`, request)
}
