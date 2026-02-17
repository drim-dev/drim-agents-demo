'use client'

interface ChatStreamingIndicatorProps {
  content: string | null
}

export function ChatStreamingIndicator({ content }: ChatStreamingIndicatorProps) {
  return (
    <div className="flex justify-start">
      <div className="max-w-[75%] px-4 py-2.5 rounded bg-stone-100 dark:bg-gray-800 text-stone-900 dark:text-stone-100">
        {content ? (
          <div className="whitespace-pre-wrap text-sm leading-relaxed break-words">
            {content}
            <span className="inline-block w-1.5 h-4 ml-0.5 bg-brand-500 dark:bg-brand-400 animate-pulse align-middle" />
          </div>
        ) : (
          <div className="flex items-center gap-1.5 text-sm text-stone-500 dark:text-stone-400">
            <span>Агент думает</span>
            <span className="flex gap-0.5">
              <span className="w-1 h-1 rounded-full bg-stone-400 dark:bg-stone-500 animate-bounce [animation-delay:0ms]" />
              <span className="w-1 h-1 rounded-full bg-stone-400 dark:bg-stone-500 animate-bounce [animation-delay:150ms]" />
              <span className="w-1 h-1 rounded-full bg-stone-400 dark:bg-stone-500 animate-bounce [animation-delay:300ms]" />
            </span>
          </div>
        )}
      </div>
    </div>
  )
}
