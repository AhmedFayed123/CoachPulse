import { readdir, readFile } from 'node:fs/promises'
import path from 'node:path'
import { fileURLToPath } from 'node:url'

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..', 'src')
const forbidden = /<(?:button|input|select|textarea|table)\b/g
const inlineStyle = /\bstyle\s*=\s*\{/g
const exceptions = new Set(['components/ui/primitives.tsx'])
const failures = []

async function scan(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name)
    if (entry.isDirectory()) await scan(full)
    else if (/\.(?:tsx?|jsx?)$/.test(entry.name)) {
      const relative = path.relative(root, full).replaceAll('\\', '/')
      if (exceptions.has(relative)) continue
      const source = await readFile(full, 'utf8')
      for (const match of source.matchAll(forbidden)) failures.push(`${relative}: raw ${match[0].slice(1)}; use a shared primitive`)
      for (const match of source.matchAll(inlineStyle)) {
        const isTenantTheme = relative === 'App.tsx' && source.slice(match.index, match.index + 80).startsWith('style={tenantThemeStyle(tenant.data)}')
        if (!isTenantTheme) failures.push(`${relative}: inline style; use design tokens`)
      }
    }
  }
}

await scan(root)
if (failures.length) {
  console.error(failures.join('\n'))
  process.exitCode = 1
} else console.log('Design consistency source check passed (dynamic tenant branding is the only inline style exception).')
