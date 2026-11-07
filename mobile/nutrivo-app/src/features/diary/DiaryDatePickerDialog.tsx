import type { ReactNode } from 'react';
import { Modal, Pressable, ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

type DiaryDatePickerDialogProps = {
    children: ReactNode;
    onCancel: () => void;
    onConfirm: () => void;
    confirmDisabled?: boolean;
};

export function DiaryDatePickerDialog({
    children,
    onCancel,
    onConfirm,
    confirmDisabled = false,
}: DiaryDatePickerDialogProps) {
    return (
        <Modal
            visible
            transparent
            animationType="none"
            onRequestClose={onCancel}
            accessibilityLabel="Datum auswählen"
        >
            <SafeAreaView style={styles.overlay}>
                <ScrollView contentContainerStyle={styles.container} bounces={false}>
                    <View style={styles.dialog} accessibilityViewIsModal>
                        <AppText variant="headlineMd" accessibilityRole="header">
                            Datum auswählen
                        </AppText>
                        {children}
                        <View style={styles.actions}>
                            <Pressable
                                accessibilityRole="button"
                                onPress={onCancel}
                                style={({ pressed }) => [styles.button, pressed && styles.pressed]}
                            >
                                <AppText>Abbrechen</AppText>
                            </Pressable>
                            <Pressable
                                accessibilityRole="button"
                                disabled={confirmDisabled}
                                accessibilityState={{ disabled: confirmDisabled }}
                                onPress={onConfirm}
                                style={({ pressed }) => [
                                    styles.button,
                                    styles.confirm,
                                    confirmDisabled && styles.disabled,
                                    pressed && styles.pressed,
                                ]}
                            >
                                <AppText tone="accent">Übernehmen</AppText>
                            </Pressable>
                        </View>
                    </View>
                </ScrollView>
            </SafeAreaView>
        </Modal>
    );
}

const styles = StyleSheet.create({
    overlay: { flex: 1, backgroundColor: 'rgba(0, 0, 0, 0.6)' },
    container: {
        flexGrow: 1,
        justifyContent: 'center',
        alignItems: 'center',
        padding: theme.spacing.md,
    },
    dialog: {
        width: '100%',
        maxWidth: 420,
        padding: theme.spacing.md,
        gap: theme.spacing.md,
        borderRadius: theme.radii.lg,
        backgroundColor: theme.colors.surface,
    },
    actions: { flexDirection: 'row', flexWrap: 'wrap', justifyContent: 'flex-end', gap: theme.spacing.sm },
    button: {
        minWidth: 44,
        minHeight: 44,
        justifyContent: 'center',
        alignItems: 'center',
        paddingHorizontal: theme.spacing.md,
        paddingVertical: theme.spacing.sm,
        borderRadius: theme.radii.sm,
    },
    confirm: { backgroundColor: theme.colors.primaryContainer },
    disabled: { opacity: 0.45 },
    pressed: { opacity: 0.7 },
});
