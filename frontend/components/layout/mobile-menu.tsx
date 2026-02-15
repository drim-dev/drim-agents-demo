'use client'

import { useState } from 'react'
import { signOut } from 'next-auth/react'
import { usePathname } from 'next/navigation'
import Link from 'next/link'
import { Button } from '@/components/button'
import { SignInLink } from '@/components/auth/sign-in-link'

interface MobileMenuProps {
  isAuthenticated: boolean
  isAdmin: boolean
}

export function MobileMenu({ isAuthenticated, isAdmin }: MobileMenuProps) {
  const [isOpen, setIsOpen] = useState(false)
  const pathname = usePathname()

  const closeMenu = () => setIsOpen(false)

  return (
    <>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="inline-flex items-center justify-center rounded p-2 text-stone-700 hover:bg-stone-100 hover:text-stone-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 dark:text-stone-300 dark:hover:bg-gray-800 dark:hover:text-stone-100 dark:focus-visible:ring-brand-600 dark:focus-visible:ring-offset-gray-950 md:hidden"
        aria-expanded={isOpen}
        aria-label="Открыть меню"
      >
        <svg
          className="h-6 w-6"
          fill="none"
          viewBox="0 0 24 24"
          strokeWidth="2"
          stroke="currentColor"
          aria-hidden="true"
        >
          {isOpen ? (
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M6 18L18 6M6 6l12 12"
            />
          ) : (
            <path
              strokeLinecap="round"
              strokeLinejoin="round"
              d="M4 6h16M4 12h16M4 18h16"
            />
          )}
        </svg>
      </button>

      {isOpen && (
        <>
          <div
            className="fixed z-40 bg-black/40 backdrop-blur-sm md:hidden"
            onClick={closeMenu}
            aria-hidden="true"
            style={{
              left: 0,
              right: 0,
              top: 0,
              bottom: 0,
              width: '100vw',
              height: '100vh',
              margin: 0,
              padding: 0,
            }}
          />

          <div
            className="fixed inset-y-0 right-0 z-50 w-full max-w-xs bg-white dark:bg-gray-900 md:hidden"
            style={{
              boxShadow: '-12px 0 40px -8px rgba(0, 0, 0, 0.5), -6px 0 16px -4px rgba(0, 0, 0, 0.3)',
            }}
          >
            <div className="relative flex h-full flex-col">
              <div className="flex items-center justify-between border-b border-stone-200 px-4 py-4 dark:border-gray-800">
                <span className="text-lg font-bold leading-snug text-stone-900 dark:text-stone-100">
                  Меню
                </span>
                <button
                  onClick={closeMenu}
                  className="rounded p-2 text-stone-700 hover:bg-stone-100 hover:text-stone-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 dark:text-stone-300 dark:hover:bg-gray-800 dark:hover:text-stone-100 dark:focus-visible:ring-brand-600 dark:focus-visible:ring-offset-gray-950"
                  aria-label="Закрыть меню"
                >
                  <svg
                    className="h-6 w-6"
                    fill="none"
                    viewBox="0 0 24 24"
                    strokeWidth="2"
                    stroke="currentColor"
                  >
                    <path
                      strokeLinecap="round"
                      strokeLinejoin="round"
                      d="M6 18L18 6M6 6l12 12"
                    />
                  </svg>
                </button>
              </div>

              <nav className="flex-1 space-y-1 overflow-y-auto px-2 py-4 pb-24">
                {isAuthenticated ? (
                  <>
                    <Link
                      href="/profile"
                      onClick={closeMenu}
                      className="flex items-center gap-3 rounded px-3 py-2 text-base font-medium leading-normal text-stone-700 hover:bg-stone-100 hover:text-stone-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-stone-300 dark:hover:bg-gray-800 dark:hover:text-stone-100 dark:focus-visible:ring-brand-600"
                    >
                      <svg
                        className="h-5 w-5"
                        fill="none"
                        viewBox="0 0 24 24"
                        strokeWidth="2"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          d="M15.75 6a3.75 3.75 0 11-7.5 0 3.75 3.75 0 017.5 0zM4.501 20.118a7.5 7.5 0 0114.998 0A17.933 17.933 0 0112 21.75c-2.676 0-5.216-.584-7.499-1.632z"
                        />
                      </svg>
                      Профиль
                    </Link>
                    {isAdmin && (
                      <Link
                        href="/admin"
                        onClick={closeMenu}
                        className="flex items-center gap-3 rounded px-3 py-2 text-base font-medium leading-normal text-stone-700 hover:bg-stone-100 hover:text-stone-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-stone-300 dark:hover:bg-gray-800 dark:hover:text-stone-100 dark:focus-visible:ring-brand-600"
                      >
                        <svg
                          className="h-5 w-5"
                          fill="none"
                          viewBox="0 0 24 24"
                          strokeWidth="2"
                          stroke="currentColor"
                        >
                          <path
                            strokeLinecap="round"
                            strokeLinejoin="round"
                            d="M9 12.75L11.25 15 15 9.75m-3-7.036A11.959 11.959 0 013.598 6 11.99 11.99 0 003 9.749c0 5.592 3.824 10.29 9 11.623 5.176-1.332 9-6.03 9-11.622 0-1.31-.21-2.571-.598-3.751h-.152c-3.196 0-6.1-1.248-8.25-3.285z"
                          />
                        </svg>
                        Админка
                      </Link>
                    )}
                  </>
                ) : (
                  <SignInLink
                    className="flex items-center gap-3 rounded px-3 py-2 text-base font-medium leading-normal text-stone-700 hover:bg-stone-100 hover:text-stone-900 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:text-stone-300 dark:hover:bg-gray-800 dark:hover:text-stone-100 dark:focus-visible:ring-brand-600"
                  >
                    <div onClick={closeMenu} className="flex items-center gap-3">
                      <svg
                        className="h-5 w-5"
                        fill="none"
                        viewBox="0 0 24 24"
                        strokeWidth="2"
                        stroke="currentColor"
                      >
                        <path
                          strokeLinecap="round"
                          strokeLinejoin="round"
                          d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15M12 9l-3 3m0 0l3 3m-3-3h12.75"
                        />
                      </svg>
                      Войти
                    </div>
                  </SignInLink>
                )}
              </nav>

              {isAuthenticated && (
                <div className="absolute bottom-0 left-0 right-0 border-t border-stone-200 bg-white p-4 dark:border-gray-800 dark:bg-gray-900">
                  <Button
                    variant="secondary"
                    onClick={() => {
                      closeMenu()
                      signOut({ callbackUrl: pathname })
                    }}
                    className="w-full justify-center gap-2"
                  >
                    <svg
                      className="h-5 w-5"
                      fill="none"
                      viewBox="0 0 24 24"
                      strokeWidth="2"
                      stroke="currentColor"
                    >
                      <path
                        strokeLinecap="round"
                        strokeLinejoin="round"
                        d="M15.75 9V5.25A2.25 2.25 0 0013.5 3h-6a2.25 2.25 0 00-2.25 2.25v13.5A2.25 2.25 0 007.5 21h6a2.25 2.25 0 002.25-2.25V15m3 0l3-3m0 0l-3-3m3 3H9"
                      />
                    </svg>
                    Выйти
                  </Button>
                </div>
              )}
            </div>
          </div>
        </>
      )}
    </>
  )
}
