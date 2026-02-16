'use client'

import { useState } from 'react'
import { ExternalLink, Pencil } from 'lucide-react'
import { Button } from '@/components/button'
import { ProjectDto } from '@/types/project'
import { EditProjectModal } from './EditProjectModal'

interface ProjectDetailContentProps {
  project: ProjectDto
}

export function ProjectDetailContent({ project }: ProjectDetailContentProps) {
  const [isEditOpen, setIsEditOpen] = useState(false)

  return (
    <>
      <div className="flex items-start justify-between gap-4 mb-8">
        <div>
          <h1 className="text-4xl font-bold text-stone-900 dark:text-stone-100 leading-tight">
            {project.name}
          </h1>
          {project.description && (
            <p className="mt-2 text-stone-600 dark:text-stone-400 leading-relaxed">
              {project.description}
            </p>
          )}
          <div className="mt-4">
            <a
              href={project.gitHubRepoUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-1.5 text-sm text-brand-600 dark:text-brand-400 hover:underline"
            >
              <ExternalLink className="h-4 w-4" />
              {project.gitHubRepoUrl.replace('https://github.com/', '')}
            </a>
          </div>
        </div>
        <Button variant="secondary" onClick={() => setIsEditOpen(true)}>
          <Pencil className="h-4 w-4 mr-2" />
          Редактировать
        </Button>
      </div>

      <div className="border border-dashed border-stone-300 dark:border-gray-700 rounded p-12 text-center">
        <p className="text-stone-500 dark:text-stone-400">
          Канбан-доска задач появится здесь
        </p>
      </div>

      <EditProjectModal
        project={project}
        isOpen={isEditOpen}
        onClose={() => setIsEditOpen(false)}
      />
    </>
  )
}
