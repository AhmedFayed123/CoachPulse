import js from '@eslint/js'
import globals from 'globals'
import tseslint from 'typescript-eslint'
import reactHooks from 'eslint-plugin-react-hooks'
import reactRefresh from 'eslint-plugin-react-refresh'

export default tseslint.config(
  { ignores: ['dist/**', 'src/api/generated.ts'] },
  js.configs.recommended,
  ...tseslint.configs.recommended,
  {
    files: ['src/**/*.{ts,tsx}'],
    languageOptions: { globals: globals.browser },
    plugins: { 'react-hooks': reactHooks, 'react-refresh': reactRefresh },
    rules: {
      'no-undef': 'off',
      'react-hooks/rules-of-hooks': 'error',
      'react-hooks/exhaustive-deps': 'warn',
      'react-refresh/only-export-components': ['warn', { allowConstantExport: true }],
    },
  },
  {
    files: ['src/**/*.{ts,tsx}'],
    ignores: ['src/components/ui/primitives.tsx'],
    rules: {
      'no-restricted-syntax': ['error',
        { selector: "JSXOpeningElement[name.name='button']", message: 'Use the shared Button primitive.' },
        { selector: "JSXOpeningElement[name.name='input']", message: 'Use the shared Input primitive.' },
        { selector: "JSXOpeningElement[name.name='select']", message: 'Use the shared Select primitive.' },
        { selector: "JSXOpeningElement[name.name='textarea']", message: 'Use the shared Textarea primitive.' },
        { selector: "JSXOpeningElement[name.name='table']", message: 'Use the shared Table primitive.' },
      ],
    },
  },
)
