import Link from 'next/link'
import { ExternalLink, Calendar } from 'lucide-react'
import { ProjectDto } from '@/types/project'

interface ProjectCardProps {
  project: ProjectDto
}

export function ProjectCard({ project }: ProjectCardProps) {
  const formattedDate = new Date(project.createdAt).toLocaleDateString('ru-RU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  })

  return (
    <Link
      href={`/projects/${project.id}`}
      className="block bg-white dark:bg-gray-900 border border-stone-200 dark:border-gray-800 p-6 rounded shadow-sm dark:shadow-gray-900/50 hover:shadow-md dark:hover:shadow-gray-900/70 transition-shadow"
    >
      <h3 className="text-lg font-bold text-stone-900 dark:text-stone-100 leading-snug truncate">
        {project.name}
      </h3>

      {project.description && (
        <p className="mt-2 text-sm text-stone-600 dark:text-stone-400 leading-relaxed line-clamp-2">
          {project.description}
        </p>
      )}

      <div className="mt-4 flex items-center gap-4 text-sm text-stone-500 dark:text-stone-500">
        <span className="flex items-center gap-1.5 truncate">
          <ExternalLink className="h-4 w-4 shrink-0" />
          <span className="truncate">
            {project.gitHubRepoUrl.replace('https://github.com/', '')}
          </span>
        </span>

        <span className="flex items-center gap-1.5 shrink-0">
          <Calendar className="h-4 w-4" />
          {formattedDate}
        </span>
      </div>
    </Link>
  )
}
