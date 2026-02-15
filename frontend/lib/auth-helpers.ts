import { auth } from '@/auth'
import { redirect } from 'next/navigation'
import { headers } from 'next/headers'

export async function requireAuth() {
  const session = await auth()
  if (!session) {
    const headersList = await headers()
    const referer = headersList.get('referer')

    if (referer) {
      try {
        const refererUrl = new URL(referer)
        const returnUrl = encodeURIComponent(refererUrl.pathname)
        redirect(`/auth/signin?callbackUrl=${returnUrl}`)
      } catch {
        redirect('/auth/signin?callbackUrl=/')
      }
    } else {
      redirect('/auth/signin?callbackUrl=/')
    }
  }
  return session.user
}

export async function requireRole(role: 'User' | 'Instructor' | 'Admin') {
  const user = await requireAuth()
  if (user.role !== role) {
    redirect('/')
  }
  return user
}

export async function getCurrentSession() {
  return await auth()
}

export async function getCurrentUser() {
  const session = await getCurrentSession()
  return session?.user || null
}
