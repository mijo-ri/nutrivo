import { Pressable, StyleSheet } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

export function FoodButton({ label, onPress, secondary = false, disabled = false }: {
    label: string;
    onPress: () => void;
    secondary?: boolean;
    disabled?: boolean;
}) {
    return (
        <Pressable
            onPress={onPress}
            disabled={disabled}
            accessibilityRole="button"
            accessibilityState={{ disabled }}
            style={({ pressed }) => [styles.button, secondary && styles.secondary, pressed && styles.pressed, disabled && styles.disabled]}
        >
            <AppText variant="bodyLg" style={{ color: secondary ? theme.colors.primary : theme.colors.onPrimaryContainer }}>
                {label}
            </AppText>
        </Pressable>
    );
}

const styles = StyleSheet.create({
    button: {
        minHeight: 48, justifyContent: 'center', alignItems: 'center',
        padding: theme.spacing.md, borderRadius: theme.radii.md,
        backgroundColor: theme.colors.primaryContainer,
    },
    secondary: { backgroundColor: theme.colors.surfaceHigh },
    pressed: { opacity: 0.75 },
    disabled: { opacity: 0.5 },
});
