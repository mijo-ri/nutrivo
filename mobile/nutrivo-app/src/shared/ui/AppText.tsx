import { Text, type TextProps } from 'react-native';

import { theme } from '@/shared/theme/theme';

type AppTextProps = TextProps & {
  variant?: keyof typeof theme.typography;
  tone?: 'default' | 'secondary' | 'accent';
};

const toneColors = {
  default: theme.colors.text,
  secondary: theme.colors.textSecondary,
  accent: theme.colors.primary,
};

export function AppText({
  variant = 'bodyMd',
  tone = 'default',
  style,
  ...props
}: AppTextProps) {
  return (
    <Text
      {...props}
      style={[theme.typography[variant], { color: toneColors[tone] }, style]}
    />
  );
}
