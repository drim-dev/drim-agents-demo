import { auth } from '@/auth'
import { DataContainer } from '@/components/layout/containers'
import { SignInLink } from '@/components/auth/sign-in-link'
import { Button } from '@/components/button'

export default async function Home() {
  const session = await auth()

  return (
    <div>
      <DataContainer className="py-16">
        <h1 className="text-4xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
          Drim Agents
        </h1>
        <p className="mt-4 text-lg text-stone-600 dark:text-stone-400 leading-relaxed">
          Платформа для оркестрации AI-агентов
        </p>

        {session?.user ? (
          <p className="mt-6 text-stone-700 dark:text-stone-300">
            Добро пожаловать, {session.user.name || session.user.email}!
          </p>
        ) : (
          <div className="mt-6">
            <SignInLink>
              <Button>Войти</Button>
            </SignInLink>
          </div>
        )}
      </DataContainer>
    </div>
  )
}
