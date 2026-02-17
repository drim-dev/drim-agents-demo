export interface ChatMessageDto {
  id: string
  role: 'user' | 'agent'
  content: string
  createdAt: string
}

export interface ChatMessagesResponse {
  chatSessionId: string | null
  stage: string
  messages: ChatMessageDto[]
}
