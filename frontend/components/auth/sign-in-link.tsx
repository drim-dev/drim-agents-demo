'use client'

import Link from 'next/link'
import { usePathname } from 'next/navigation'
import { ReactNode } from 'react'

interface SignInLinkProps {
  children: ReactNode
  className?: string
}

export function SignInLink({ children, className }: SignInLinkProps) {
  const pathname = usePathname()
  const returnUrl = encodeURIComponent(pathname)

  return (
    <Link href={`/auth/signin?callbackUrl=${returnUrl}`} className={className}>
      {children}
    </Link>
  )
}
