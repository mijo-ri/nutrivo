import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useRef, useState } from 'react';
import {
  Platform,
  Pressable,
  StyleSheet,
  TextInput,
  type TextInputInstance,
  View,
} from 'react-native';

import { theme } from '@/shared/theme/theme';

export function FoodSearchInput({
  value,
  onChangeText,
}: {
  value: string;
  onChangeText: (value: string) => void;
}) {
  const [focused, setFocused] = useState(false);
  const input = useRef<TextInputInstance>(null);

  return (
    <View style={[styles.searchBox, focused && styles.focused]}>
      <MaterialDesignIcons
        name="magnify"
        size={22}
        color={theme.colors.textSecondary}
        accessible={false}
        aria-hidden
      />
      <TextInput
        ref={input}
        value={value}
        onChangeText={onChangeText}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        accessibilityLabel="Lebensmittel oder Marke suchen"
        placeholder="Lebensmittel oder Marke suchen"
        placeholderTextColor={theme.colors.textSecondary}
        selectionColor={theme.colors.primary}
        autoCorrect={false}
        autoCapitalize="none"
        returnKeyType="search"
        style={styles.input}
      />
      {value.length > 0 && (
        <Pressable
          onPress={() => {
            onChangeText('');
            input.current?.focus();
          }}
          accessibilityRole="button"
          accessibilityLabel="Suche löschen"
          style={({ pressed }) => [styles.clear, pressed && styles.pressed]}
        >
          <MaterialDesignIcons
            name="close"
            size={22}
            color={theme.colors.textSecondary}
            accessible={false}
            aria-hidden
          />
        </Pressable>
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  searchBox: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    minHeight: 52,
    paddingLeft: theme.spacing.md,
    paddingRight: theme.spacing.xs,
    borderWidth: 1,
    borderColor: theme.colors.outlineVariant,
    borderRadius: theme.radii.lg,
    backgroundColor: theme.colors.surface,
  },
  focused: { borderColor: theme.colors.primary },
  input: {
    flex: 1,
    minWidth: 0,
    minHeight: 50,
    paddingVertical: theme.spacing.md,
    color: theme.colors.text,
    ...theme.typography.bodyLg,
    ...Platform.select({ web: { outlineStyle: 'solid', outlineWidth: 0 } }),
  },
  clear: {
    minWidth: 44,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: theme.radii.full,
  },
  pressed: { backgroundColor: theme.colors.surfaceHighest },
});
