import { NextRequest } from 'next/server'
import { proxyGet, proxyPost } from '@/lib/api'

export async function GET() {
  return proxyGet('/api/projects')
}

export async function POST(request: NextRequest) {
  return proxyPost('/api/projects', request)
}
