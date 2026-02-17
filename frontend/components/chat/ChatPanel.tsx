'use client'

import { useCallback } from 'react'
import { useChatSignalR } from '@/hooks/use-chat-signalr'
import { ChatMessageList } from './ChatMessageList'
import { ChatInput } from './ChatInput'
import { AlertCircle } from 'lucide-react'

interface ChatPanelProps {
  taskId: string
}

export function ChatPanel({ taskId }: ChatPanelProps) {
  const {
    messages,
    streamingContent,
    agentStatus,
    isStreaming,
    sendMessage,
    error,
  } = useChatSignalR(taskId)

  const handleSend = useCallback(
    (content: string) => {
      sendMessage(content)
    },
    [sendMessage]
  )

  return (
    <div className="flex flex-col h-full bg-white dark:bg-gray-950">
      {error && (
        <div className="mx-4 mt-4 flex items-center gap-2 px-3 py-2 rounded bg-red-50 dark:bg-red-950/30 border border-red-200 dark:border-red-800 text-red-700 dark:text-red-400 text-sm">
          <AlertCircle className="h-4 w-4 flex-shrink-0" />
          <span className="flex-1">{error}</span>
        </div>
      )}
      {agentStatus === 'unavailable' && (
        <div className="mx-4 mt-4 flex items-center gap-2 px-3 py-2 rounded bg-stone-50 dark:bg-gray-900 border border-stone-200 dark:border-gray-800 text-stone-600 dark:text-stone-400 text-sm">
          <AlertCircle className="h-4 w-4 flex-shrink-0" />
          <span>Агент временно недоступен</span>
        </div>
      )}
      <ChatMessageList
        messages={messages}
        streamingContent={streamingContent}
        isStreaming={isStreaming}
      />
      <ChatInput
        onSend={handleSend}
        disabled={isStreaming || agentStatus === 'unavailable'}
      />
    </div>
  )
}
