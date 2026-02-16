'use client'

import { useState } from 'react'
import { Plus, FolderOpen } from 'lucide-react'
import { Button } from '@/components/button'
import { ProjectDto } from '@/types/project'
import { ProjectList } from './ProjectList'
import { CreateProjectModal } from './CreateProjectModal'

interface ProjectsPageContentProps {
  projects: ProjectDto[]
}

export function ProjectsPageContent({ projects }: ProjectsPageContentProps) {
  const [isCreateOpen, setIsCreateOpen] = useState(false)

  if (projects.length === 0) {
    return (
      <>
        <div className="text-center py-16">
          <FolderOpen className="h-12 w-12 mx-auto text-stone-400 dark:text-stone-500 mb-4" />
          <h2 className="text-2xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
            Нет проектов
          </h2>
          <p className="mt-2 text-stone-600 dark:text-stone-400">
            Создайте первый проект, чтобы начать работу
          </p>
          <div className="mt-6">
            <Button onClick={() => setIsCreateOpen(true)}>
              <Plus className="h-4 w-4 mr-2" />
              Создать проект
            </Button>
          </div>
        </div>
        <CreateProjectModal
          isOpen={isCreateOpen}
          onClose={() => setIsCreateOpen(false)}
        />
      </>
    )
  }

  return (
    <>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-4xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
          Проекты
        </h1>
        <Button onClick={() => setIsCreateOpen(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Создать проект
        </Button>
      </div>
      <ProjectList projects={projects} />
      <CreateProjectModal
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
      />
    </>
  )
}
