import type { ReactNode } from 'react'

type EmptyStateProps = {
  title: string
  description?: string
  action?: ReactNode
  className?: string
}

export function EmptyState({ title, description, action, className = '' }: EmptyStateProps) {
  return (
    <div className={`empty-state${className ? ` ${className}` : ''}`} role="status">
      <strong>{title}</strong>
      {description && <p>{description}</p>}
      {action}
    </div>
  )
}
