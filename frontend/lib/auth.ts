import { auth } from '@/auth'

export async function getServerSession() {
  return await auth()
}

export async function getCurrentUser() {
  const session = await auth()
  return session?.user ?? null
}

export async function requireAuth() {
  const session = await auth()
  if (!session?.user) {
    throw new Error('Unauthorized')
  }
  return session.user
}

export async function hasRole(role: string) {
  const session = await auth()
  return session?.user?.role === role
}

export async function requireRole(role: string) {
  const user = await requireAuth()
  if (user.role !== role) {
    throw new Error('Forbidden')
  }
  return user
}

export async function isAdmin() {
  return await hasRole('Admin')
}

export async function requireAdmin() {
  return await requireRole('Admin')
}

