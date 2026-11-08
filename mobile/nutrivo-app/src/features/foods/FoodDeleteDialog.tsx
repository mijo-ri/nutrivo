import { Modal, ScrollView, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { FoodButton } from './FoodButton';

export function FoodDeleteDialog({ name, onCancel, onConfirm }: {
    name: string;
    onCancel: () => void;
    onConfirm: () => void;
}) {
    return (
        <Modal
            visible
            transparent
            animationType="none"
            onRequestClose={onCancel}
            accessibilityLabel="Lebensmittel löschen"
        >
            <SafeAreaView style={styles.overlay}>
                <ScrollView contentContainerStyle={styles.container} bounces={false}>
                    <View style={styles.dialog} accessibilityViewIsModal>
                        <AppText variant="headlineMd" accessibilityRole="header">Lebensmittel löschen?</AppText>
                        <AppText>Möchtest du „{name}“ aus deinen Lebensmitteln löschen? Bisherige Tagebucheinträge bleiben unverändert erhalten.</AppText>
                        <View style={styles.actions}>
                            <FoodButton label="Abbrechen" secondary onPress={onCancel} />
                            <FoodButton label="Löschen" destructive onPress={onConfirm} />
                        </View>
                    </View>
                </ScrollView>
            </SafeAreaView>
        </Modal>
    );
}

const styles = StyleSheet.create({
    overlay: { flex: 1, backgroundColor: 'rgba(0, 0, 0, 0.6)' },
    container: { flexGrow: 1, justifyContent: 'center', alignItems: 'center', padding: theme.spacing.md },
    dialog: {
        width: '100%', maxWidth: 420, padding: theme.spacing.lg, gap: theme.spacing.lg,
        borderRadius: theme.radii.lg, backgroundColor: theme.colors.surface,
    },
    actions: { gap: theme.spacing.sm },
});
