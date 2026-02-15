import { SignInForm } from '@/components/auth/sign-in-form'
import { AuthContainer } from '@/components/layout/containers'

interface SignInPageProps {
  searchParams: Promise<{ callbackUrl?: string }>
}

export default async function SignInPage({ searchParams }: SignInPageProps) {
  const { callbackUrl } = await searchParams

  return (
    <div className="min-h-screen flex items-center justify-center bg-stone-50 dark:bg-gray-950">
      <AuthContainer className="space-y-6">
        <div className="text-center">
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-100">
            Войти в Drim Agents
          </h2>
          <p className="mt-2 text-sm text-stone-600 dark:text-stone-400">
            Платформа для оркестрации AI-агентов
          </p>
        </div>
        <div className="bg-white dark:bg-gray-900 border border-stone-200 dark:border-gray-800 rounded shadow-sm dark:shadow-gray-900/50 p-6">
          <SignInForm callbackUrl={callbackUrl} />
        </div>
      </AuthContainer>
    </div>
  )
}
