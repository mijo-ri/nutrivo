---
name: Technical Precision Dark
colors:
  surface: '#131317'
  surface-dim: '#131317'
  surface-bright: '#39393d'
  surface-container-lowest: '#0e0e12'
  surface-container-low: '#1b1b1f'
  surface-container: '#1f1f23'
  surface-container-high: '#2a292e'
  surface-container-highest: '#353439'
  on-surface: '#e4e1e7'
  on-surface-variant: '#ccc3d8'
  inverse-surface: '#e4e1e7'
  inverse-on-surface: '#303034'
  outline: '#958da1'
  outline-variant: '#4a4455'
  surface-tint: '#d2bbff'
  primary: '#d2bbff'
  on-primary: '#3f008e'
  primary-container: '#7c3aed'
  on-primary-container: '#ede0ff'
  inverse-primary: '#732ee4'
  secondary: '#cebdff'
  on-secondary: '#381385'
  secondary-container: '#4f319c'
  on-secondary-container: '#bea8ff'
  tertiary: '#7bd0ff'
  on-tertiary: '#00354a'
  tertiary-container: '#006e95'
  on-tertiary-container: '#caeaff'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#eaddff'
  primary-fixed-dim: '#d2bbff'
  on-primary-fixed: '#25005a'
  on-primary-fixed-variant: '#5a00c6'
  secondary-fixed: '#e8ddff'
  secondary-fixed-dim: '#cebdff'
  on-secondary-fixed: '#21005e'
  on-secondary-fixed-variant: '#4f319c'
  tertiary-fixed: '#c4e7ff'
  tertiary-fixed-dim: '#7bd0ff'
  on-tertiary-fixed: '#001e2c'
  on-tertiary-fixed-variant: '#004c69'
  background: '#131317'
  on-background: '#e4e1e7'
  surface-variant: '#353439'
typography:
  headline-xl:
    fontFamily: Geist
    fontSize: 36px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.03em
  headline-xl-mobile:
    fontFamily: Geist
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.025em
  headline-lg:
    fontFamily: Geist
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Geist
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.015em
  body-lg:
    fontFamily: Geist
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 22px
    letterSpacing: -0.01em
  body-md:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: -0.005em
  body-sm:
    fontFamily: Geist
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
    letterSpacing: 0em
  label-numeric-lg:
    fontFamily: Geist
    fontSize: 28px
    fontWeight: '700'
    lineHeight: 32px
    letterSpacing: -0.03em
  label-numeric-md:
    fontFamily: Geist
    fontSize: 18px
    fontWeight: '600'
    lineHeight: 22px
    letterSpacing: -0.02em
  label-numeric-sm:
    fontFamily: Geist
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0em
  label-caps:
    fontFamily: Geist
    fontSize: 11px
    fontWeight: '600'
    lineHeight: 14px
    letterSpacing: 0.06em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  gutter: 0.75rem
  margin: 1rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-lg: 1rem
  space-xl: 1.5rem
---

## Brand & Style

This design system is engineered for experienced nutrition trackers who treat body composition and dietary intake as an exact, high-performance discipline. Drawing direct inspiration from elite developer environments, performance telemetry, and professional athletics, the visual character is austere, rapid, and profoundly functional.

The aesthetic fuses **Modern Technical Minimalism** with **Instrument-Grade UI**:
- **Deep Graphite Foundations**: Canvas and surfaces absorb ambient glare, focusing 100% of the tracker’s cognitive load on macro data, timing, and targets.
- **Surgical Violet Accents**: Interactive elements prioritize sharp intent through electric violet, while active states shimmer in soft lavender.
- **Telemetry-Grade Color Coding**: Nutrition requires unambiguous instant recognition. The tripartite macronutrient system—Cyan for Protein, Amber-Gold for Kohlenhydrate, Rose-Coral for Fett—acts as a universal data visualization protocol across charts, inline bars, and micro badges.
- **Zero Fluff**: Decorative illustrations, soft skeuomorphic gradients, and empty ornamental padding are completely absent. Every pixel serves dense data scanning and single-tap input efficiency.

## Colors

The palette operates in a calibrated low-reflectance space designed for quick logging in low-light gym or kitchen environments.

### Core Canvas & Surfacing
- **Base Canvas (`#101014`)**: Pitch graphite background providing deep contrast without total OLED smearing.
- **Card Surface (`#1B1B24`)**: Elevated component foundation for meals, charts, and input sheets.
- **Interactive Surface / Sub-card (`#242432`)**: Secondary elevation for input fields, segment selectors, and active state wells.
- **Structural Stroke (`#2B2A3A`)**: Crisp 1px division token separating dense rows and framing container boundaries.

### Brand & Interactive Accents
- **Primary Violet (`#7C3AED`)**: High-priority actions, primary confirmations, and active selection anchors.
- **Active Lavender (`#A78BFA`)**: Stepper counters, active tab indicators, and micro-interactions.

### Functional Macronutrient Palette
Macro indicators are immutable across the entire interface. They must never be repurposed for semantic statuses like errors or warnings:
- **Protein (`#38BDF8`)**: Vibrant sky cyan.
- **Kohlenhydrate (`#F59E0B`)**: Amber gold.
- **Fett (`#F43F5E`)**: Rose coral.

### Typography & Data Contrast
- **Text Primary (`#F5F5FA`)**: Pure legibility for numeric calorie readouts, headers, and active values.
- **Text Secondary (`#B3B1C2`)**: Cool technical gray for units (`kcal`, `g`), timestamps, and column labels.
- **Text Disabled (`#5A586B`)**: Inactive states and placeholder glyphs.

## Typography

Typography acts as a calibrated metric readout. Using `Geist`, characters maintain razor-sharp legibility at minimal sizes while rendering numbers with absolute engineering clarity.

### Tabular Numerical Discipline (`tnum`)
Every numeric display token (`label-numeric-lg`, `label-numeric-md`, `label-numeric-sm`) and data-driven table row **must** enforce `font-variant-numeric: tabular-nums`. This prevents layout jitter during real-time value scrubbers, barcode scans, and quick-entry updates.

### Unit Pairings
Calorie and macro figures display an asymmetric unit relationship:
- In `label-numeric-lg` (e.g., `2.450 kcal`), the integer is rendered in `#F5F5FA` at `700` weight, while the trailing unit `kcal` is locked at `14px` (`body-md`), weight `400`, color `#B3B1C2`, offset by `4px` horizontal spacing.
- Gram indicators (`g`) next to macros must maintain a 1:1 baseline lock with their numeric value rather than superscripting.

## Layout & Spacing

This layout adheres strictly to an 8-point base grid with a 4-point micro-step:

- **Mobile Viewport Structure**: Single-column fluid presentation optimized for one-handed operation. Screen margins are hard-locked to `16px` (`margin`) to maximize information bandwidth on compact viewports.
- **Section Gaps**: Meal segments (`Frühstück`, `Mittagessen`, `Abendessen`, `Snacks`) are spaced by `space-lg` (16px).
- **Dense Item Rhythms**: Logged line items within a meal card use compact vertical padding of `8px` (`space-sm`) with a persistent `4px` (`space-xs`) separator to balance tap separation with high scan density.
- **Safe Area Insets**: Header areas respect top system notches with a standard minimum padding of `48px`. Bottom viewports extend padding beneath the `48px` action bar to avoid gesture bar conflicts.
- **Touch Bounds**: While typography and data rows remain visually compact (32–36px height), native invisible hitboxes are enforced to a minimum of `44x44px` on all actionable triggers.

## Elevation & Depth

To sustain the technical tool ethos, elevation is achieved purely via **Tonal Layering** and **Crisp Borders**, entirely avoiding heavy diffuse shadows.

### Elevation Hierarchy
1. **Level 0 (Canvas)**: `#101014` — Base viewport background.
2. **Level 1 (Card & Group Panels)**: `#1B1B24` with a hairline stroke: `1px solid #2B2A3A`. No dropshadow.
3. **Level 2 (Active Wells / Form Elements / Sliders)**: `#242432` with a subtle inner border `1px solid #2B2A3A`.
4. **Level 3 (Overlays & Sheets)**: `#1B1B24` elevated over a `rgba(0, 0, 0, 0.72)` dimming scrim, framed by a continuous `1px solid #2B2A3A` perimeter.

### Focus & Active State Feedback
Active selection does not raise an element in Z-space. Instead, it activates an inner or bounding border transition: `border-color: #7C3AED` accompanied by a micro inner glow `0 0 0 1px #7C3AED`.

## Shapes

The shape system balances modern ergonomic hand-feel with technical tool precision. It avoids playful bubble forms and uncompromising sharp corners:

- **Cards & Primary Modules**: `8px` to `10px` (`rounded-md` to `rounded-lg`) corner radii.
- **Input Fields & Keypad Buttons**: `8px` radius.
- **Badges, Inline Metric Pills, and Action Chips**: Strictly `4px` or `6px` radii—never full pill radius (50%), preserving the code-editor aesthetic.
- **Linear Telemetry Bars**: `3px` radius on track containers and fill segments, providing rounded terminals without looking like soft stadium capsules.

## Components

### Buttons
- **Primary Action (e.g., "Eintrag speichern")**: Full-width or auto-width on mobile. Background `#7C3AED`, text `#F5F5FA`, font `14px` weight `600`, height `44px`, corner radius `8px`. Active tap state dims background to `#6D28D9`.
- **Secondary / Utility (e.g., "+ Lebensmittel")**: Background `#242432`, border `1px solid #2B2A3A`, text `#A78BFA`.
- **Ghost Action**: Transparent background, text `#B3B1C2`, hover/tap color `#F5F5FA`.

### Horizontal Telemetry Progress Bars (Macro & Calorie Bars)
- Circular gauges are forbidden for daily targets. Instead, full-width multi-segment or single horizontal rails are used.
- **Track**: Height `6px`, background `#242432`, radius `3px`.
- **Progress Segment**: Solid hex matching macro tokens (`#38BDF8`, `#F59E0B`, `#F43F5E`).
- **Over-Target State**: When intake exceeds 100%, the overfilled span flashes a distinct high-intensity warning stroke or pattern overlay.

### Meal Sections & Logged Items
- **Meal Header**: Compact bar displaying the title (e.g., `Frühstück`), summary calories (e.g., `640 kcal`), and macro breakdown pills (`P 42g`, `K 65g`, `F 18g`).
- **Item Row**: Left-aligned food item name and portion (`150g Haferflocken`), right-aligned calorie count (`552 kcal`) in tabular figures. Bottom border `1px solid #2B2A3A`. Swipe-to-delete reveals a `#F43F5E` inline action.

### Numeric Keypad & Weight Inputs
- Custom in-app bottom keypad for frictionless logging. Keys: background `#1B1B24`, border `1px solid #2B2A3A`, active state `#242432`, text `20px` `label-numeric-md`. Zero-lag haptic feedback trigger on touch down.

### Persistent Bottom Navigation
- Fixed 3-tab navigation bar pinned to viewport bottom. Background `#101014`, top border `1px solid #2B2A3A`.
- Height: `56px` + safe-area bottom inset.
- Items: `Tagebuch`, `Lebensmittel`, `Ziele`.
- Inactive icon and label: `#B3B1C2`. Active icon and label: `#A78BFA`.

### Chips & Micro Tags
- Small data chips for macro ratios (e.g., `40% P`): Background `#1B1B24`, border `1px solid #2B2A3A`, text `11px` `label-caps`. Cyan, amber, or rose dot (4px diameter) prefixed to denote nutrient type.