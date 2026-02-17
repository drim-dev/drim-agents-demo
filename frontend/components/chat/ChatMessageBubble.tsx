'use client'

import { ChatMessageDto } from '@/types/chat'

interface ChatMessageBubbleProps {
  message: ChatMessageDto
}

function formatTime(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
}

export function ChatMessageBubble({ message }: ChatMessageBubbleProps) {
  const isUser = message.role === 'user'

  return (
    <div className={`flex ${isUser ? 'justify-end' : 'justify-start'}`}>
      <div
        className={`max-w-[75%] px-4 py-2.5 rounded ${
          isUser
            ? 'bg-gradient-to-r from-brand-500 to-brand-550 dark:from-brand-600 dark:to-brand-650 text-white'
            : 'bg-stone-100 dark:bg-gray-800 text-stone-900 dark:text-stone-100'
        }`}
      >
        <div className="whitespace-pre-wrap text-sm leading-relaxed break-words">
          {message.content}
        </div>
        <div
          className={`text-xs mt-1 ${
            isUser
              ? 'text-white/70'
              : 'text-stone-500 dark:text-stone-400'
          }`}
        >
          {formatTime(message.createdAt)}
        </div>
      </div>
    </div>
  )
}
