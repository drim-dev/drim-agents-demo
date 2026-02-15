'use client'

import { useSession } from 'next-auth/react'
import Link from 'next/link'
import { Button } from '@/components/button'
import { ThemeToggle } from './theme-toggle'
import { MobileMenu } from './mobile-menu'
import { UserMenu } from './user-menu'
import { SignInLink } from '@/components/auth/sign-in-link'

export function Navbar() {
  const { data: session, status } = useSession()

  return (
    <nav className="bg-white dark:bg-gray-900 border-b border-stone-200 dark:border-gray-800">
      <div className="mx-auto max-w-6xl px-4 sm:px-6 lg:px-8">
        <div className="flex h-16 justify-between">
          <div className="flex items-center">
            <Link
              href="/"
              className="rounded text-2xl font-bold text-brand-500 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 dark:text-brand-500 dark:focus-visible:ring-brand-600 dark:focus-visible:ring-offset-gray-950"
            >
              Drim Agents
            </Link>
          </div>

          <div className="flex items-center space-x-4">
            <ThemeToggle />

            <div className="hidden items-center space-x-4 md:flex">
              {status === 'loading' ? (
                <div className="h-8 w-24 animate-pulse rounded bg-stone-200 dark:bg-gray-700"></div>
              ) : session ? (
                <UserMenu session={session} />
              ) : (
                <SignInLink>
                  <Button>Войти</Button>
                </SignInLink>
              )}
            </div>

            {status !== 'loading' && (
              <MobileMenu
                isAuthenticated={!!session}
                isAdmin={session?.user.role === 'Admin'}
              />
            )}
          </div>
        </div>
      </div>
    </nav>
  )
}
