export function formatDate(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleDateString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })
}

export function formatDateTime(dateString: string): string {
  const date = new Date(dateString)
  return date.toLocaleString('ru-RU', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  })
}

export function formatRelativeTime(dateString: string): string {
  const date = new Date(dateString)
  const now = new Date()
  const diffMs = now.getTime() - date.getTime()
  const diffSeconds = Math.floor(diffMs / 1000)
  const diffMinutes = Math.floor(diffSeconds / 60)
  const diffHours = Math.floor(diffMinutes / 60)
  const diffDays = Math.floor(diffHours / 24)

  if (diffSeconds < 60) {
    return 'только что'
  } else if (diffMinutes < 60) {
    return `${diffMinutes} мин. назад`
  } else if (diffHours < 24) {
    return `${diffHours} ч. назад`
  } else if (diffDays < 7) {
    return `${diffDays} дн. назад`
  } else {
    return formatDate(dateString)
  }
}

export function cn(...classes: (string | undefined | null | false)[]): string {
  return classes.filter(Boolean).join(' ')
}

export function pluralize(count: number, one: string, few: string, many: string): string {
  const mod10 = count % 10
  const mod100 = count % 100

  if (mod100 >= 11 && mod100 <= 19) {
    return many
  }

  if (mod10 === 1) {
    return one
  }

  if (mod10 >= 2 && mod10 <= 4) {
    return few
  }

  return many
}

export function formatDuration(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} ${pluralize(minutes, 'минута', 'минуты', 'минут')}`
  }

  const hours = Math.floor(minutes / 60)
  const remainingMinutes = minutes % 60

  const hourStr = `${hours} ${pluralize(hours, 'час', 'часа', 'часов')}`

  if (remainingMinutes === 0) {
    return hourStr
  }

  const minStr = `${remainingMinutes} ${pluralize(remainingMinutes, 'минута', 'минуты', 'минут')}`
  return `${hourStr} ${minStr}`
}
