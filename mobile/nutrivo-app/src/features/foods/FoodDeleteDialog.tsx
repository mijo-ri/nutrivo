import { type ComponentRef, useRef } from 'react';
import {
  ActivityIndicator,
  Modal,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

export function FoodDeleteDialog({
  name,
  pending,
  onCancel,
  onConfirm,
}: {
  name: string;
  pending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}) {
  const cancel = useRef<ComponentRef<typeof Pressable>>(null);

  return (
    <Modal
      visible
      transparent
      animationType="none"
      onRequestClose={() => {
        if (!pending) onCancel();
      }}
      onShow={() => {
        if (Platform.OS === 'web') cancel.current?.focus();
      }}
    >
      <SafeAreaView style={styles.overlay}>
        <ScrollView contentContainerStyle={styles.container} bounces={false}>
          <View
            style={styles.dialog}
            role="alertdialog"
            accessibilityLabel="Lebensmittel löschen"
            aria-describedby="food-delete-message"
            accessibilityViewIsModal
          >
            <AppText variant="headlineMd" accessibilityRole="header">
              Lebensmittel löschen
            </AppText>
            <AppText nativeID="food-delete-message">
              „{name}“ dauerhaft löschen? Bereits erfasste Tagebucheinträge bleiben
              erhalten.
            </AppText>
            {pending && (
              <View style={styles.progress}>
                <ActivityIndicator color={theme.colors.primary} />
                <AppText accessibilityLiveRegion="polite" role="status">
                  Wird gelöscht …
                </AppText>
              </View>
            )}
            <View style={styles.actions}>
              <Pressable
                ref={cancel}
                onPress={onCancel}
                disabled={pending}
                accessibilityRole="button"
                accessibilityState={{ disabled: pending }}
                style={({ pressed }) => [
                  styles.button,
                  pending && styles.disabled,
                  pressed && styles.pressed,
                ]}
              >
                <AppText>Abbrechen</AppText>
              </Pressable>
              <Pressable
                onPress={onConfirm}
                disabled={pending}
                accessibilityRole="button"
                accessibilityState={{ disabled: pending }}
                style={({ pressed }) => [
                  styles.button,
                  styles.destructive,
                  pending && styles.disabled,
                  pressed && styles.pressed,
                ]}
              >
                <AppText style={{ color: theme.colors.error }}>Dauerhaft löschen</AppText>
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
    padding: theme.spacing.lg,
    gap: theme.spacing.md,
    borderRadius: theme.radii.lg,
    backgroundColor: theme.colors.surface,
  },
  actions: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'flex-end',
    gap: theme.spacing.sm,
  },
  button: {
    minHeight: 48,
    justifyContent: 'center',
    alignItems: 'center',
    padding: theme.spacing.md,
    borderRadius: theme.radii.md,
    backgroundColor: theme.colors.surfaceHigh,
  },
  destructive: { borderWidth: 1, borderColor: theme.colors.error },
  progress: { flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm },
  pressed: { opacity: 0.75 },
  disabled: { opacity: 0.5 },
});
