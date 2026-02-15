'use client'

import { useState, useRef, useEffect } from 'react'
import { signOut } from 'next-auth/react'
import { usePathname } from 'next/navigation'
import Link from 'next/link'
import type { Session } from 'next-auth'

interface UserMenuProps {
  session: Session
  displayName?: string
}

export function UserMenu({ session, displayName }: UserMenuProps) {
  const [isOpen, setIsOpen] = useState(false)
  const menuRef = useRef<HTMLDivElement>(null)
  const pathname = usePathname()

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setIsOpen(false)
      }
    }

    if (isOpen) {
      document.addEventListener('mousedown', handleClickOutside)
      return () => document.removeEventListener('mousedown', handleClickOutside)
    }
  }, [isOpen])

  const userName = displayName || session.user.name || 'User'

  return (
    <div className="relative" ref={menuRef}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex h-9 w-9 items-center justify-center rounded-full bg-gradient-to-r from-brand-500 to-brand-550 text-sm font-medium text-white transition-all duration-200 hover:scale-[1.05] hover:from-brand-600 hover:to-brand-650 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 focus-visible:ring-offset-2 dark:from-brand-600 dark:to-brand-650 dark:hover:from-brand-700 dark:hover:to-brand-700 dark:focus-visible:ring-brand-600 dark:focus-visible:ring-offset-gray-950"
        aria-expanded={isOpen}
        aria-haspopup="true"
        aria-label="Меню пользователя"
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
      </button>

      {isOpen && (
        <div className="absolute right-0 z-50 mt-2 w-64 rounded border border-stone-200 bg-white shadow-lg dark:border-gray-800 dark:bg-gray-900 dark:shadow-gray-900/50">
          <div className="border-b border-stone-200 px-4 py-3 dark:border-gray-800">
            <p className="text-sm font-medium leading-normal text-stone-900 dark:text-stone-100">
              {userName}
            </p>
            <p className="text-xs leading-normal text-stone-600 dark:text-stone-400">
              {session.user.email}
            </p>
          </div>

          <nav className="py-2">
            <Link
              href="/profile"
              onClick={() => setIsOpen(false)}
              className="flex items-center gap-3 px-4 py-2 text-sm leading-normal text-stone-700 hover:bg-stone-100 focus:bg-stone-100 focus-visible:outline-none dark:text-stone-300 dark:hover:bg-gray-800 dark:focus:bg-gray-800"
            >
              <svg
                className="h-4 w-4"
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

            {session.user.role === 'Admin' && (
              <Link
                href="/admin"
                onClick={() => setIsOpen(false)}
                className="flex items-center gap-3 px-4 py-2 text-sm leading-normal text-stone-700 hover:bg-stone-100 focus:bg-stone-100 focus-visible:outline-none dark:text-stone-300 dark:hover:bg-gray-800 dark:focus:bg-gray-800"
              >
                <svg
                  className="h-4 w-4"
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
          </nav>

          <div className="border-t border-stone-200 py-2 dark:border-gray-800">
            <button
              onClick={() => {
                setIsOpen(false)
                signOut({ callbackUrl: pathname })
              }}
              className="flex w-full items-center gap-3 px-4 py-2 text-sm leading-normal text-stone-700 hover:bg-stone-100 focus:bg-stone-100 focus-visible:outline-none dark:text-stone-300 dark:hover:bg-gray-800 dark:focus:bg-gray-800"
            >
              <svg
                className="h-4 w-4"
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
            </button>
          </div>
        </div>
      )}
    </div>
  )
}
