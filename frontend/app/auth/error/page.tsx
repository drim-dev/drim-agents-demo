import Link from 'next/link'
import { Button } from '@/components/button'
import { AuthContainer } from '@/components/layout/containers'

export default async function AuthErrorPage({
  searchParams,
}: {
  searchParams: Promise<{ error?: string }>
}) {
  const { error } = await searchParams

  const errorMessages: Record<string, string> = {
    Configuration: 'Ошибка конфигурации сервера',
    AccessDenied: 'Доступ запрещен',
    Verification: 'Ошибка проверки',
    Default: 'Произошла ошибка при входе',
  }

  const errorMessage = error ? errorMessages[error] || errorMessages.Default : errorMessages.Default

  return (
    <div className="min-h-screen flex items-center justify-center bg-stone-50 dark:bg-gray-950">
      <AuthContainer className="space-y-8 text-center">
        <div>
          <h2 className="mt-6 text-3xl font-bold text-stone-900 dark:text-stone-100">
            Ошибка входа
          </h2>
          <p className="mt-2 text-sm text-stone-600 dark:text-stone-400">{errorMessage}</p>
        </div>
        <div className="mt-8">
          <Link href="/auth/signin">
            <Button className="w-full">Попробовать снова</Button>
          </Link>
        </div>
      </AuthContainer>
    </div>
  )
}
