'use client'

import { useEffect, useRef } from 'react'
import { ChatMessageDto } from '@/types/chat'
import { ChatMessageBubble } from './ChatMessageBubble'
import { ChatStreamingIndicator } from './ChatStreamingIndicator'

interface ChatMessageListProps {
  messages: ChatMessageDto[]
  streamingContent: string | null
  isStreaming: boolean
}

export function ChatMessageList({
  messages,
  streamingContent,
  isStreaming,
}: ChatMessageListProps) {
  const bottomRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, streamingContent, isStreaming])

  return (
    <div className="flex-1 overflow-y-auto p-4 space-y-3">
      {messages.length === 0 && !isStreaming && (
        <div className="flex items-center justify-center h-full text-sm text-stone-500 dark:text-stone-400">
          Начните диалог с агентом
        </div>
      )}
      {messages.map(message => (
        <ChatMessageBubble key={message.id} message={message} />
      ))}
      {isStreaming && <ChatStreamingIndicator content={streamingContent} />}
      <div ref={bottomRef} />
    </div>
  )
}
