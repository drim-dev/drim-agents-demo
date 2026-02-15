export interface PageResponse<T> {
  items: T[]
  nextPageToken?: string
}

export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  instance?: string
  errors?: Record<string, string[]>
}

export enum UserRole {
  User = 'User',
  Instructor = 'Instructor',
  Admin = 'Admin',
}

export interface UserDto {
  id: string
  email: string
  displayName?: string
  bio?: string
  avatarUrl?: string
  role: UserRole
  createdAt: string
}

export interface PublicUserDto {
  id: string
  displayName?: string
  bio?: string
  avatarUrl?: string
  createdAt: string
}

export interface UpdateProfileRequest {
  displayName?: string
  bio?: string
  avatarUrl?: string
}

export class ApiClientError extends Error {
  constructor(
    public status: number,
    message: string,
    public problemDetails?: ProblemDetails
  ) {
    super(message)
    this.name = 'ApiClientError'
  }
}
