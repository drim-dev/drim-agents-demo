export interface ProjectDto {
  id: string
  name: string
  description: string | null
  gitHubRepoUrl: string
  maskedGitHubPat: string
  createdAt: string
  updatedAt: string
}

export interface CreateProjectRequest {
  name: string
  description?: string
  gitHubRepoUrl: string
  gitHubPat: string
}

export interface UpdateProjectRequest {
  name: string
  description?: string
  gitHubRepoUrl: string
  gitHubPat?: string | null
}
