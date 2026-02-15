'use client'

import { Moon, Sun } from 'lucide-react'
import { useTheme } from 'next-themes'
import { useEffect, useState } from 'react'

export function ThemeToggle() {
  const [mounted, setMounted] = useState(false)
  const { theme, setTheme } = useTheme()

  useEffect(() => {
    setMounted(true)
  }, [])

  if (!mounted) {
    return (
      <div className="h-9 w-9 rounded bg-stone-100 dark:bg-gray-800 animate-pulse" />
    )
  }

  return (
    <button
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      className="relative h-9 w-9 rounded border border-stone-200 bg-white hover:bg-stone-50 dark:border-gray-700 dark:bg-gray-800 dark:hover:bg-gray-700 transition-colors flex items-center justify-center focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-950"
      aria-label="Toggle theme"
    >
      <Sun className="h-4 w-4 text-brand-500 dark:hidden" />
      <Moon className="hidden h-4 w-4 text-brand-500 dark:block" />
    </button>
  )
}
