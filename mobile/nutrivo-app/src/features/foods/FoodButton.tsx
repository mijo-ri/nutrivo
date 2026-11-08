import { Pressable, StyleSheet } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

export function FoodButton({ label, onPress, secondary = false, destructive = false, disabled = false }: {
    label: string;
    onPress: () => void;
    secondary?: boolean;
    destructive?: boolean;
    disabled?: boolean;
}) {
    return (
        <Pressable
            onPress={onPress}
            disabled={disabled}
            accessibilityRole="button"
            accessibilityState={{ disabled }}
            style={({ pressed }) => [styles.button, secondary && styles.secondary, destructive && styles.destructive, pressed && styles.pressed, disabled && styles.disabled]}
        >
            <AppText variant="bodyLg" style={{ color: destructive ? theme.colors.error : secondary ? theme.colors.primary : theme.colors.onPrimaryContainer }}>
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
    destructive: { backgroundColor: theme.colors.surfaceHigh, borderWidth: 1, borderColor: theme.colors.error },
    pressed: { opacity: 0.75 },
    disabled: { opacity: 0.5 },
});
