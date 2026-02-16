import { NextRequest } from 'next/server'
import { proxyGet, proxyPut } from '@/lib/api'

export async function GET(
  _request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params
  return proxyGet(`/api/projects/${id}`)
}

export async function PUT(
  request: NextRequest,
  { params }: { params: Promise<{ id: string }> }
) {
  const { id } = await params
  return proxyPut(`/api/projects/${id}`, request)
}
