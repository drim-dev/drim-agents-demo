'use client'

import { KeyboardEvent, useCallback, useRef, useState } from 'react'
import { Send } from 'lucide-react'

interface ChatInputProps {
  onSend: (content: string) => void
  disabled: boolean
}

export function ChatInput({ onSend, disabled }: ChatInputProps) {
  const [value, setValue] = useState('')
  const textareaRef = useRef<HTMLTextAreaElement>(null)

  const adjustHeight = useCallback(() => {
    const textarea = textareaRef.current
    if (textarea) {
      textarea.style.height = 'auto'
      textarea.style.height = `${Math.min(textarea.scrollHeight, 160)}px`
    }
  }, [])

  const handleSend = useCallback(() => {
    const trimmed = value.trim()
    if (!trimmed || disabled) return

    onSend(trimmed)
    setValue('')

    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto'
    }
  }, [value, disabled, onSend])

  const handleKeyDown = useCallback(
    (e: KeyboardEvent<HTMLTextAreaElement>) => {
      if (e.key === 'Enter' && !e.shiftKey) {
        e.preventDefault()
        handleSend()
      }
    },
    [handleSend]
  )

  return (
    <div className="border-t border-stone-200 dark:border-gray-800 p-4">
      <div className="flex items-end gap-2">
        <textarea
          ref={textareaRef}
          value={value}
          onChange={e => {
            setValue(e.target.value)
            adjustHeight()
          }}
          onKeyDown={handleKeyDown}
          disabled={disabled}
          placeholder="Введите сообщение..."
          rows={1}
          className="flex-1 resize-none px-3 py-2 bg-white dark:bg-gray-800 border border-stone-300 dark:border-gray-700 rounded text-stone-900 dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow text-sm disabled:opacity-50"
        />
        <button
          onClick={handleSend}
          disabled={disabled || !value.trim()}
          className="p-2 rounded bg-gradient-to-r from-brand-500 to-brand-550 hover:from-brand-600 hover:to-brand-650 dark:from-brand-600 dark:to-brand-650 dark:hover:from-brand-700 dark:hover:to-brand-700 text-white transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500 dark:focus-visible:ring-brand-600 focus-visible:ring-offset-2 dark:focus-visible:ring-offset-gray-950"
        >
          <Send className="h-4 w-4" />
        </button>
      </div>
    </div>
  )
}
