import type { TextStyle } from 'react-native';

export const fontFamilies = {
  regular: 'Geist-Regular',
  medium: 'Geist-Medium',
  semiBold: 'Geist-SemiBold',
  bold: 'Geist-Bold',
} as const;

const typography = {
  headlineXl: {
    fontFamily: fontFamilies.bold,
    fontSize: 36,
    lineHeight: 40,
    letterSpacing: -1.08,
  },
  headlineXlMobile: {
    fontFamily: fontFamilies.bold,
    fontSize: 28,
    lineHeight: 32,
    letterSpacing: -0.7,
  },
  headlineLg: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 24,
    lineHeight: 28,
    letterSpacing: -0.48,
  },
  headlineMd: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 18,
    lineHeight: 24,
    letterSpacing: -0.27,
  },
  bodyLg: {
    fontFamily: fontFamilies.regular,
    fontSize: 16,
    lineHeight: 22,
    letterSpacing: -0.16,
  },
  bodyMd: {
    fontFamily: fontFamilies.regular,
    fontSize: 14,
    lineHeight: 20,
    letterSpacing: -0.07,
  },
  bodySm: {
    fontFamily: fontFamilies.regular,
    fontSize: 12,
    lineHeight: 16,
  },
  numericLg: {
    fontFamily: fontFamilies.bold,
    fontSize: 28,
    lineHeight: 32,
    letterSpacing: -0.84,
    fontVariant: ['tabular-nums'],
  },
  numericMd: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 18,
    lineHeight: 22,
    letterSpacing: -0.36,
    fontVariant: ['tabular-nums'],
  },
  numericSm: {
    fontFamily: fontFamilies.medium,
    fontSize: 13,
    lineHeight: 16,
    fontVariant: ['tabular-nums'],
  },
  labelCaps: {
    fontFamily: fontFamilies.semiBold,
    fontSize: 11,
    lineHeight: 14,
    letterSpacing: 0.66,
    textTransform: 'uppercase',
  },
} satisfies Record<string, TextStyle>;

// The visible Stitch export takes precedence over the DESIGN.md prose.
export const theme = {
  colors: {
    background: '#131317',
    surfaceLow: '#1b1b1f',
    surface: '#1f1f23',
    surfaceHigh: '#2a292e',
    surfaceHighest: '#353439',
    text: '#e4e1e7',
    textSecondary: '#ccc3d8',
    primary: '#d2bbff',
    onPrimary: '#3f008e',
    primaryContainer: '#7c3aed',
    onPrimaryContainer: '#ede0ff',
    outline: '#958da1',
    outlineVariant: '#4a4455',
    error: '#ffb4ab',
    protein: '#7bd0ff',
    carbohydrates: '#e8ddff',
    fat: '#ffb4ab',
  },
  spacing: { xs: 4, sm: 8, md: 12, lg: 16, xl: 24, screen: 16 },
  radii: { sm: 4, md: 8, lg: 12, full: 9999 },
  typography,
} as const;
