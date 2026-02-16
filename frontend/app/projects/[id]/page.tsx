import { notFound } from 'next/navigation'
import { requireAuth } from '@/lib/auth-helpers'
import { forwardToBackend } from '@/lib/api'
import { DataContainer } from '@/components/layout/containers'
import { ProjectDetailContent } from '@/components/projects/ProjectDetailContent'
import { ProjectDto } from '@/types/project'

interface ProjectPageProps {
  params: Promise<{ id: string }>
}

export default async function ProjectPage({ params }: ProjectPageProps) {
  await requireAuth()
  const { id } = await params

  const response = await forwardToBackend(`/api/projects/${id}`)
  if (!response.ok) {
    notFound()
  }

  const project: ProjectDto = await response.json()

  return (
    <DataContainer className="py-8">
      <ProjectDetailContent project={project} />
    </DataContainer>
  )
}
