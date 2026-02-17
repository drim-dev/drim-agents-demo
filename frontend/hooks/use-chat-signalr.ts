'use client'

import { useCallback, useEffect, useRef, useState } from 'react'
import { HubConnection, HubConnectionState } from '@microsoft/signalr'
import { createChatConnection } from '@/lib/signalr'
import { apiGet, apiPost } from '@/lib/api-client'
import { ChatMessageDto, ChatMessagesResponse } from '@/types/chat'
import { ApiClientError } from '@/types/api'

interface SendMessageRequest {
  content: string
}

interface SendMessageResponse {
  id: string
}

type AgentStatus = 'available' | 'unavailable' | null

interface UseChatSignalRReturn {
  messages: ChatMessageDto[]
  streamingContent: string | null
  agentStatus: AgentStatus
  isStreaming: boolean
  sendMessage: (content: string) => Promise<void>
  error: string | null
}

export function useChatSignalR(taskId: string): UseChatSignalRReturn {
  const [messages, setMessages] = useState<ChatMessageDto[]>([])
  const [streamingContent, setStreamingContent] = useState<string | null>(null)
  const [agentStatus, setAgentStatus] = useState<AgentStatus>(null)
  const [isStreaming, setIsStreaming] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const connectionRef = useRef<HubConnection | null>(null)
  const lastMessageIdRef = useRef<string | null>(null)

  const loadMessages = useCallback(async (afterId?: string): Promise<void> => {
    const params = new URLSearchParams()
    if (afterId) {
      params.set('afterId', afterId)
    }
    const query = params.toString()
    const path = `/api/tasks/${taskId}/chat/messages${query ? `?${query}` : ''}`

    const response = await apiGet<ChatMessagesResponse>(path)

    if (response.messages.length > 0) {
      const lastMsg = response.messages[response.messages.length - 1]
      lastMessageIdRef.current = lastMsg.id

      if (afterId) {
        setMessages(prev => [...prev, ...response.messages])
      } else {
        setMessages(response.messages)
      }
    }
  }, [taskId])

  const sendMessage = useCallback(async (content: string): Promise<void> => {
    setError(null)
    try {
      await apiPost<SendMessageResponse, SendMessageRequest>(
        `/api/tasks/${taskId}/chat/messages`,
        { content }
      )
    } catch (err) {
      if (err instanceof ApiClientError && err.status === 503) {
        setError('Агент временно недоступен. Попробуйте позже.')
      } else {
        setError('Не удалось отправить сообщение.')
      }
    }
  }, [taskId])

  useEffect(() => {
    const connection = createChatConnection()
    connectionRef.current = connection

    function registerHandlers(conn: HubConnection): void {
      conn.on('MessageReceived', (message: ChatMessageDto) => {
        setMessages(prev => [...prev, message])
        lastMessageIdRef.current = message.id
      })

      conn.on('StreamStarted', () => {
        setIsStreaming(true)
        setStreamingContent(null)
      })

      conn.on('StreamTokenReceived', (token: string) => {
        setStreamingContent(prev => (prev ?? '') + token)
      })

      conn.on('StreamCompleted', (message: ChatMessageDto) => {
        setIsStreaming(false)
        setStreamingContent(null)
        setMessages(prev => [...prev, message])
        lastMessageIdRef.current = message.id
      })

      conn.on('StreamError', (errorMessage: string) => {
        setIsStreaming(false)
        setStreamingContent(null)
        setError(errorMessage)
      })

      conn.on('AgentStatusChanged', (status: string) => {
        setAgentStatus(status as AgentStatus)
      })
    }

    registerHandlers(connection)

    connection.onreconnected(async () => {
      try {
        await connection.invoke('JoinTask', taskId)
        if (lastMessageIdRef.current) {
          await loadMessages(lastMessageIdRef.current)
        }
      } catch {
        setError('Не удалось восстановить соединение.')
      }
    })

    async function start(): Promise<void> {
      try {
        await connection.start()
        await connection.invoke('JoinTask', taskId)
        await loadMessages()
      } catch {
        setError('Не удалось подключиться к чату.')
      }
    }

    start()

    return () => {
      if (connection.state !== HubConnectionState.Disconnected) {
        connection.invoke('LeaveTask', taskId).catch(() => {})
        connection.stop()
      }
    }
  }, [taskId, loadMessages])

  return {
    messages,
    streamingContent,
    agentStatus,
    isStreaming,
    sendMessage,
    error,
  }
}
