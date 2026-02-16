import { requireAuth } from '@/lib/auth-helpers'
import { forwardToBackend } from '@/lib/api'
import { DataContainer } from '@/components/layout/containers'
import { ProjectsPageContent } from '@/components/projects/ProjectsPageContent'
import { ProjectDto } from '@/types/project'

export default async function ProjectsPage() {
  await requireAuth()

  const response = await forwardToBackend('/api/projects')
  const projects: ProjectDto[] = response.ok ? await response.json() : []

  return (
    <DataContainer className="py-8">
      <ProjectsPageContent projects={projects} />
    </DataContainer>
  )
}
