'use client'

import { useState } from 'react'
import { useRouter } from 'next/navigation'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { X } from 'lucide-react'
import { toast } from 'sonner'
import { Button } from '@/components/button'
import { apiPost } from '@/lib/api-client'
import { ApiClientError } from '@/types/api'
import { ProjectDto } from '@/types/project'
import {
  createProjectSchema,
  CreateProjectFormData,
} from '@/lib/validations/project'

interface CreateProjectModalProps {
  isOpen: boolean
  onClose: () => void
}

export function CreateProjectModal({ isOpen, onClose }: CreateProjectModalProps) {
  const router = useRouter()
  const [isSubmitting, setIsSubmitting] = useState(false)

  const {
    register,
    handleSubmit,
    setError,
    reset,
    formState: { errors },
  } = useForm<CreateProjectFormData>({
    resolver: zodResolver(createProjectSchema),
  })

  if (!isOpen) return null

  const onSubmit = async (data: CreateProjectFormData) => {
    setIsSubmitting(true)
    try {
      await apiPost<ProjectDto>('/api/projects', data)
      toast.success('Проект создан')
      reset()
      onClose()
      router.refresh()
    } catch (error) {
      if (error instanceof ApiClientError && error.problemDetails?.errors) {
        const fieldErrors = error.problemDetails.errors
        for (const [field, messages] of Object.entries(fieldErrors)) {
          const fieldName = field.charAt(0).toLowerCase() + field.slice(1)
          setError(fieldName as keyof CreateProjectFormData, {
            message: messages[0],
          })
        }
      } else if (error instanceof ApiClientError) {
        toast.error(error.problemDetails?.detail || error.message)
      }
    } finally {
      setIsSubmitting(false)
    }
  }

  const handleClose = () => {
    reset()
    onClose()
  }

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm"
      onClick={handleClose}
    >
      <div
        className="w-full max-w-lg mx-4 bg-white dark:bg-gray-900 border border-stone-200 dark:border-gray-800 rounded shadow-lg p-6"
        onClick={(e) => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
            Новый проект
          </h2>
          <button
            onClick={handleClose}
            className="text-stone-500 dark:text-stone-400 hover:text-stone-700 dark:hover:text-stone-200 transition-colors"
          >
            <X className="h-5 w-5" />
          </button>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-stone-900 dark:text-stone-100 mb-1">
              Название
            </label>
            <input
              {...register('name')}
              className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-stone-300 dark:border-gray-700 rounded text-stone-900 dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow"
              placeholder="Мой проект"
            />
            {errors.name && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">
                {errors.name.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-stone-900 dark:text-stone-100 mb-1">
              Описание
            </label>
            <textarea
              {...register('description')}
              rows={3}
              className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-stone-300 dark:border-gray-700 rounded text-stone-900 dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow resize-none"
              placeholder="Описание проекта (необязательно)"
            />
            {errors.description && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">
                {errors.description.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-stone-900 dark:text-stone-100 mb-1">
              GitHub URL
            </label>
            <input
              {...register('gitHubRepoUrl')}
              className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-stone-300 dark:border-gray-700 rounded text-stone-900 dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow"
              placeholder="https://github.com/owner/repo"
            />
            {errors.gitHubRepoUrl && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">
                {errors.gitHubRepoUrl.message}
              </p>
            )}
          </div>

          <div>
            <label className="block text-sm font-medium text-stone-900 dark:text-stone-100 mb-1">
              GitHub Personal Access Token
            </label>
            <input
              {...register('gitHubPat')}
              type="password"
              className="w-full px-3 py-2 bg-white dark:bg-gray-800 border border-stone-300 dark:border-gray-700 rounded text-stone-900 dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow"
              placeholder="ghp_xxxxxxxxxxxx"
            />
            {errors.gitHubPat && (
              <p className="mt-1 text-sm text-red-600 dark:text-red-400">
                {errors.gitHubPat.message}
              </p>
            )}
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <Button type="button" variant="secondary" onClick={handleClose}>
              Отмена
            </Button>
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Создание...' : 'Создать'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  )
}
