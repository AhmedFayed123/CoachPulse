import { readdir, readFile } from 'node:fs/promises'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'

const sourceRoot = fileURLToPath(new URL('../src/', import.meta.url))
const suspicious = /(?:[\u00c3\u00d8\u00d9][\u0080-\u00ff]|[\u0637\u0638][\u0080-\u00ff]|\u00ef\u061f\u00bd|\ufffd)/u
const failures = []

async function inspect(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name)
    if (entry.isDirectory()) {
      await inspect(path)
      continue
    }
    if (!/\.(?:tsx?|css|html)$/.test(entry.name)) continue
    const contents = await readFile(path, 'utf8')
    contents.split(/\r?\n/).forEach((line, index) => {
      if (suspicious.test(line)) failures.push(`${path}:${index + 1}`)
    })
  }
}

await inspect(sourceRoot)
if (failures.length) {
  process.stderr.write(`Suspicious text encoding found:\n${failures.join('\n')}\n`)
  process.exitCode = 1
} else {
  process.stdout.write('Source text encoding check passed.\n')
}
