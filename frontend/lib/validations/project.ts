import { z } from 'zod'

const GITHUB_REPO_URL_REGEX =
  /^https:\/\/github\.com\/[a-zA-Z0-9\-\.]+\/[a-zA-Z0-9\-\.\_]+$/

export const createProjectSchema = z.object({
  name: z
    .string()
    .min(3, 'Название должно содержать минимум 3 символа')
    .max(200, 'Название не должно превышать 200 символов'),
  description: z.string().optional(),
  gitHubRepoUrl: z
    .url('Введите корректный URL')
    .regex(GITHUB_REPO_URL_REGEX, 'URL должен быть в формате https://github.com/owner/repo'),
  gitHubPat: z.string().min(1, 'GitHub Personal Access Token обязателен'),
})

export const updateProjectSchema = z.object({
  name: z
    .string()
    .min(3, 'Название должно содержать минимум 3 символа')
    .max(200, 'Название не должно превышать 200 символов'),
  description: z.string().optional(),
  gitHubRepoUrl: z
    .url('Введите корректный URL')
    .regex(GITHUB_REPO_URL_REGEX, 'URL должен быть в формате https://github.com/owner/repo'),
  gitHubPat: z.string().min(1, 'GitHub Personal Access Token не может быть пустым').optional(),
})

export type CreateProjectFormData = z.infer<typeof createProjectSchema>
export type UpdateProjectFormData = z.infer<typeof updateProjectSchema>
