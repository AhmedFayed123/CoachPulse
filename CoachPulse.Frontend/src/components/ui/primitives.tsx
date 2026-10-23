import { forwardRef, type ButtonHTMLAttributes, type InputHTMLAttributes, type SelectHTMLAttributes, type TableHTMLAttributes, type TextareaHTMLAttributes, type TdHTMLAttributes, type ThHTMLAttributes, type HTMLAttributes } from 'react'

function classes(...values: Array<string | undefined>) {
  return values.filter(Boolean).join(' ')
}

export const Button = forwardRef<HTMLButtonElement, ButtonHTMLAttributes<HTMLButtonElement>>(function Button({ className, ...props }, ref) {
  return <button ref={ref} className={classes('cp-button', className)} {...props} />
})
export const Input = forwardRef<HTMLInputElement, InputHTMLAttributes<HTMLInputElement>>(function Input({ className, ...props }, ref) {
  return <input ref={ref} className={classes('cp-control', className)} {...props} />
})
export const Select = forwardRef<HTMLSelectElement, SelectHTMLAttributes<HTMLSelectElement>>(function Select({ className, ...props }, ref) {
  return <select ref={ref} className={classes('cp-control', className)} {...props} />
})
export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(function Textarea({ className, ...props }, ref) {
  return <textarea ref={ref} className={classes('cp-control cp-textarea', className)} {...props} />
})
export function Table({ className, ...props }: TableHTMLAttributes<HTMLTableElement>) {
  return <table className={classes('cp-table', className)} {...props} />
}
export function TableHead(props: HTMLAttributes<HTMLTableSectionElement>) { return <thead {...props} /> }
export function TableBody(props: HTMLAttributes<HTMLTableSectionElement>) { return <tbody {...props} /> }
export function TableRow(props: HTMLAttributes<HTMLTableRowElement>) { return <tr {...props} /> }
export function TableHeaderCell(props: ThHTMLAttributes<HTMLTableCellElement>) { return <th {...props} /> }
export function TableCell(props: TdHTMLAttributes<HTMLTableCellElement>) { return <td {...props} /> }
