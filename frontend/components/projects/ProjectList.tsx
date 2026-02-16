import { ProjectDto } from '@/types/project'
import { ProjectCard } from './ProjectCard'

interface ProjectListProps {
  projects: ProjectDto[]
}

export function ProjectList({ projects }: ProjectListProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {projects.map((project) => (
        <ProjectCard key={project.id} project={project} />
      ))}
    </div>
  )
}
