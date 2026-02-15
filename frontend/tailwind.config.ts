import type { Config } from "tailwindcss";

export default {
  content: [
    "./pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./components/**/*.{js,ts,jsx,tsx,mdx}",
    "./app/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  darkMode: "class",
  theme: {
    extend: {
      colors: {
        background: "var(--background)",
        foreground: "var(--foreground)",
        brand: {
          50: '#fff5f2',
          100: '#ffe8e0',
          200: '#ffd5c7',
          300: '#ffb89f',
          400: '#ff8c66',
          500: '#ff5f24',
          550: '#ea5722',
          600: '#d55020',
          650: '#c4481e',
          700: '#b3451c',
          800: '#8f3817',
          900: '#702d13',
        },
        accent: {
          50: '#f0fdfa',
          100: '#ccfbf1',
          200: '#99f6e4',
          300: '#5eead4',
          400: '#2dd4bf',
          500: '#14b8a6',
          550: '#10a599',
          600: '#0d9488',
          650: '#0b847a',
          700: '#0f766e',
          800: '#115e59',
          900: '#134e4a',
        },
      },
      typography: {
        DEFAULT: {
          css: {
            'code::before': {
              content: '""',
            },
            'code::after': {
              content: '""',
            },
            maxWidth: 'none',
            p: {
              marginTop: '0.75em',
              marginBottom: '0.75em',
            },
            'p:first-child': {
              marginTop: '0',
            },
            'p:last-child': {
              marginBottom: '0',
            },
            strong: {
              fontWeight: '600',
            },
            ul: {
              marginTop: '0.75em',
              marginBottom: '0.75em',
            },
            ol: {
              marginTop: '0.75em',
              marginBottom: '0.75em',
            },
            li: {
              marginTop: '0.25em',
              marginBottom: '0.25em',
            },
          },
        },
      },
    },
  },
  plugins: [require('@tailwindcss/typography')],
} satisfies Config;
