import { useId, useState } from 'react';
import { Platform, StyleSheet, TextInput, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

export function FoodFormField({
  label,
  value,
  onChangeText,
  error,
  numeric = false,
  placeholder,
  accessibilityLabel,
  disabled = false,
}: {
  label: string;
  value: string;
  onChangeText: (value: string) => void;
  error?: string;
  numeric?: boolean;
  placeholder?: string;
  accessibilityLabel?: string;
  disabled?: boolean;
}) {
  const [focused, setFocused] = useState(false);
  const errorId = useId();

  return (
    <View style={styles.field}>
      <AppText>{label}</AppText>
      <TextInput
        value={value}
        editable={!disabled}
        accessibilityState={{ disabled }}
        onChangeText={onChangeText}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        accessibilityLabel={accessibilityLabel ?? label}
        accessibilityHint={error}
        aria-describedby={error ? errorId : undefined}
        aria-invalid={!!error}
        placeholder={placeholder}
        placeholderTextColor={theme.colors.outline}
        selectionColor={theme.colors.primary}
        keyboardType={numeric ? 'decimal-pad' : 'default'}
        autoCorrect={false}
        style={[styles.input, focused && styles.focused, !!error && styles.invalid]}
      />
      {error && (
        <AppText nativeID={errorId} style={styles.error}>
          {error}
        </AppText>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  field: { gap: theme.spacing.xs },
  input: {
    minHeight: 48,
    paddingHorizontal: theme.spacing.md,
    paddingVertical: theme.spacing.md,
    borderWidth: 1,
    borderColor: theme.colors.outlineVariant,
    borderRadius: theme.radii.md,
    color: theme.colors.text,
    backgroundColor: theme.colors.surfaceLow,
    ...theme.typography.bodyLg,
    ...Platform.select({ web: { outlineStyle: 'solid', outlineWidth: 0 } }),
  },
  focused: { borderColor: theme.colors.primary },
  invalid: { borderColor: theme.colors.error },
  error: { color: theme.colors.error },
});
