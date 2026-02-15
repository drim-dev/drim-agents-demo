# Drim Agents Design System

## Overview

Drim Agents uses a **Modern Minimal** design style with sharp, compact aesthetics and subtle brand color accents. The design emphasizes clarity, hierarchy, and professionalism while maintaining visual warmth through strategic use of the brand orange color.

## Design Philosophy

**Core Principles:**
- **Minimal and Sharp**: Clean lines, compact spacing, sharp corners (4px border radius)
- **Visual Hierarchy**: Logo as primary focal point, supporting elements use muted tones
- **Subtle Gradients**: Very gentle color transitions to add depth without overwhelming
- **Theme-Aware**: Different color intensities for light and dark modes to maintain proper contrast and visual comfort
- **Accessibility First**: Proper contrast ratios, readable typography, clear focus states

## Color Palette

### Brand Colors

The brand color is **rgb(255, 95, 36)** - a vibrant orange that represents energy, creativity, and innovation in the AI era. Used for primary actions and core brand elements.

### Accent Colors

The accent color is a **teal (#14b8a6)** used for secondary content types that need visual distinction from the primary brand.

```typescript
brand: {
  50: '#fff5f2',
  100: '#ffe8e0',
  200: '#ffd5c7',
  300: '#ffb89f',
  400: '#ff8c66',
  500: '#ff5f24',  // Primary brand color - exact rgb(255, 95, 36)
  550: '#ea5722',  // Subtle gradient end for light mode
  600: '#d55020',  // Muted for buttons (darker, less saturated)
  650: '#c4481e',  // Subtle gradient end for dark mode
  700: '#b3451c',
  800: '#8f3817',
  900: '#702d13',
}

accent: {
  50: '#f0fdfa',
  100: '#ccfbf1',
  200: '#99f6e4',
  300: '#5eead4',
  400: '#2dd4bf',
  500: '#14b8a6',  // Primary accent color (teal)
  550: '#10a599',  // Subtle gradient end for light mode
  600: '#0d9488',  // Muted for buttons
  650: '#0b847a',  // Subtle gradient end for dark mode
  700: '#0f766e',
  800: '#115e59',
  900: '#134e4a',
}
```

### Neutral Colors

We use a **mixed palette approach** optimized for each theme:

- **Light Mode**: Tailwind's **stone** palette (warm-tinted neutrals)
- **Dark Mode**: Tailwind's **gray** palette (cool-tinted neutrals)

## Light Theme

### Color Usage

**Backgrounds:**
- Primary background: `bg-white`
- Secondary background: `bg-stone-50`
- Card backgrounds: `bg-white` with `border-stone-200` borders
- Input backgrounds: `bg-white`
- Navbar: `bg-white` with `border-stone-200` bottom border

**Text:**
- Primary text: `text-stone-900`
- Secondary text: `text-stone-600`
- Tertiary/muted text: `text-stone-500`

**Brand Color Applications:**
- **Logo**: `text-brand-500`
- **Primary buttons**: `bg-gradient-to-r from-brand-500 to-brand-550`
- **Button hover**: `from-brand-600 to-brand-650`
- **Links**: `text-brand-600`
- **Focus rings**: `ring-brand-500`

## Dark Theme

### Color Usage

**Backgrounds:**
- Primary background: `dark:bg-gray-950`
- Card backgrounds: `dark:bg-gray-900` with `dark:border-gray-800` borders
- Input backgrounds: `dark:bg-gray-800`
- Navbar: `dark:bg-gray-900` with `dark:border-gray-800` bottom border

**Text:**
- Primary text: `dark:text-stone-100`
- Secondary text: `dark:text-stone-400`
- Tertiary/muted text: `dark:text-stone-500`

**Brand Color Applications:**
- **Logo**: `dark:text-brand-500`
- **Primary buttons**: `dark:from-brand-600 dark:to-brand-650`
- **Button hover**: `dark:from-brand-700 dark:to-brand-700`
- **Links**: `dark:text-brand-400`
- **Focus rings**: `dark:ring-brand-600`
- **Focus ring offset**: `dark:focus-visible:ring-offset-gray-950`

## Component Patterns

### Buttons

**Button Sizes:**
- **Medium (md)**: `px-4 py-2 text-sm` - Default
- **Large (lg)**: `px-6 py-3 text-base` - For primary CTAs

**Universal Focus Ring Pattern:**

ALL interactive elements use this consistent focus pattern:

```typescript
focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500
dark:focus-visible:ring-brand-600 focus-visible:ring-offset-2
dark:focus-visible:ring-offset-gray-950
```

### Cards

**Feature Card:**
```typescript
className="bg-white dark:bg-gray-900 border border-stone-200
dark:border-gray-800 p-6 rounded shadow-sm dark:shadow-gray-900/50
hover:shadow-md dark:hover:shadow-gray-900/70 transition-shadow"
```

### Forms

**Input Field:**
```typescript
className="w-full px-3 py-2 bg-white dark:bg-gray-800 border
border-stone-300 dark:border-gray-700 rounded text-stone-900
dark:text-stone-100 placeholder:text-stone-500 dark:placeholder:text-stone-400
focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-brand-500
dark:focus-visible:ring-brand-600 focus:border-transparent transition-shadow"
```

## Typography

- **H1**: `text-4xl md:text-5xl`, `font-bold`, `leading-tight`
- **H2**: `text-2xl`, `font-bold`, `leading-tight`
- **H3**: `text-lg`, `font-bold`, `leading-snug`
- **Body**: `text-base`, `font-normal`, `leading-normal`
- **Body small**: `text-sm`, `font-normal`, `leading-relaxed`
- **Labels**: `text-sm`, `font-medium`, `leading-normal`

## Layout

**Border Radius:** `rounded` (4px)

**Container padding:** `px-4 sm:px-6 lg:px-8`

**Responsive breakpoints:**
- **sm**: 640px
- **md**: 768px
- **lg**: 1024px

## Animation and Transitions

- **Hover**: `hover:scale-[1.01]`
- **Active/Click**: `active:scale-[0.99]`
- **Disabled**: `disabled:hover:scale-100`
- **Transition**: `transition-all duration-200`

## Icon System

Using `lucide-react` for consistent, clean icons.

Standard icon size: `h-4 w-4` or `h-5 w-5` depending on context.

---

**Last Updated**: 2026-02-16
**Maintained By**: Drim Agents Team
