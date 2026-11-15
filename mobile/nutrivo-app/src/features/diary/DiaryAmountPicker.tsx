import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { type ComponentRef, useRef, useState } from 'react';
import {
  AccessibilityInfo,
  findNodeHandle,
  Keyboard,
  Modal,
  Platform,
  Pressable,
  type PressableProps,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { type Food } from '@/features/foods/food';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { formatServingSize } from './diary';

export function DiaryAmountPicker({
  food,
  servingSizeId,
  disabled,
  onSelect,
}: {
  food: Food;
  servingSizeId?: string;
  disabled: boolean;
  onSelect: (servingSizeId?: string) => void;
}) {
  const [open, setOpen] = useState(false);
  const trigger = useRef<ComponentRef<typeof Pressable>>(null);
  const selectedOption = useRef<ComponentRef<typeof Pressable>>(null);
  const options = [
    {
      id: undefined,
      label: `Direkt in ${food.referenceUnit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}`,
    },
    ...food.servingSizes.map((size) => ({
      id: size.id,
      label: formatServingSize(size, food.referenceUnit),
    })),
  ];
  const selectedLabel = options.find((option) => option.id === servingSizeId)?.label;

  function restoreFocus() {
    if (Platform.OS === 'web') {
      trigger.current?.focus();
    } else {
      const tag = findNodeHandle(trigger.current);
      if (tag != null) AccessibilityInfo.setAccessibilityFocus(tag);
    }
  }

  function close() {
    setOpen(false);
    // Web and Android do not expose the iOS Modal onDismiss callback.
    if (Platform.OS !== 'ios') requestAnimationFrame(restoreFocus);
  }

  return (
    <View style={styles.field}>
      <AppText>Mengenart</AppText>
      <Pressable
        ref={trigger}
        disabled={disabled}
        accessibilityRole="button"
        accessibilityLabel={`Mengenart: ${selectedLabel}`}
        accessibilityHint="Öffnet die Auswahl der Menge oder Portionsgrösse"
        accessibilityState={{ disabled, expanded: open }}
        aria-haspopup="dialog"
        onPress={() => {
          Keyboard.dismiss();
          setOpen(true);
        }}
        style={({ pressed }) => [styles.trigger, pressed && styles.pressed]}
      >
        <AppText style={styles.label}>{selectedLabel}</AppText>
        <MaterialDesignIcons
          name="chevron-down"
          size={24}
          color={theme.colors.primary}
          accessible={false}
          aria-hidden
        />
      </Pressable>
      <Modal
        visible={open}
        transparent
        animationType="none"
        onRequestClose={close}
        onDismiss={restoreFocus}
        onShow={() => {
          if (Platform.OS === 'web') selectedOption.current?.focus();
        }}
      >
        <SafeAreaView style={styles.overlay}>
          <View
            style={styles.dialog}
            role="dialog"
            accessibilityLabel="Mengenart auswählen"
            accessibilityViewIsModal
          >
            <AppText variant="headlineMd" accessibilityRole="header">
              Mengenart auswählen
            </AppText>
            <ScrollView
              style={styles.list}
              contentContainerStyle={styles.options}
              keyboardShouldPersistTaps="handled"
            >
              <View accessibilityRole="radiogroup" accessibilityLabel="Mengenart">
                {options.map((option) => {
                  const selected = option.id === servingSizeId;
                  const select = () => {
                    if (!selected) onSelect(option.id);
                    close();
                  };
                  const keyboardProps: Pick<PressableProps, 'onKeyDown'> =
                    Platform.OS === 'web'
                      ? {
                          onKeyDown: (event) => {
                            const { key, repeat } = event.nativeEvent;
                            if (key === ' ') {
                              event.preventDefault();
                              if (!repeat) select();
                            } else if (
                              [
                                'ArrowDown',
                                'ArrowRight',
                                'ArrowUp',
                                'ArrowLeft',
                                'Home',
                                'End',
                              ].includes(key)
                            ) {
                              event.preventDefault();
                              const target =
                                event.currentTarget as unknown as HTMLElement;
                              const radios = Array.from(
                                target.parentElement?.querySelectorAll<HTMLElement>(
                                  '[role="radio"]',
                                ) ?? [],
                              );
                              const index = radios.indexOf(target);
                              const next =
                                key === 'Home'
                                  ? 0
                                  : key === 'End'
                                    ? radios.length - 1
                                    : (index +
                                        (key === 'ArrowDown' || key === 'ArrowRight'
                                          ? 1
                                          : -1) +
                                        radios.length) %
                                      radios.length;
                              radios[next]?.focus();
                              radios[next]?.click();
                            }
                          },
                        }
                      : {};
                  return (
                    <Pressable
                      {...keyboardProps}
                      key={option.id ?? 'direct'}
                      ref={selected ? selectedOption : undefined}
                      accessibilityRole="radio"
                      accessibilityLabel={option.label}
                      accessibilityState={{ checked: selected }}
                      aria-checked={selected}
                      onPress={select}
                      style={({ pressed }) => [
                        styles.option,
                        selected && styles.selected,
                        pressed && styles.pressed,
                      ]}
                    >
                      <MaterialDesignIcons
                        name={selected ? 'radiobox-marked' : 'radiobox-blank'}
                        size={24}
                        color={selected ? theme.colors.primary : theme.colors.outline}
                        accessible={false}
                        aria-hidden
                      />
                      <AppText
                        style={styles.label}
                        tone={selected ? 'accent' : 'default'}
                      >
                        {option.label}
                      </AppText>
                    </Pressable>
                  );
                })}
              </View>
            </ScrollView>
            <Pressable
              accessibilityRole="button"
              onPress={close}
              style={({ pressed }) => [styles.cancel, pressed && styles.pressed]}
            >
              <AppText tone="accent">Abbrechen</AppText>
            </Pressable>
          </View>
        </SafeAreaView>
      </Modal>
    </View>
  );
}

const styles = StyleSheet.create({
  field: { gap: theme.spacing.xs },
  trigger: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    padding: theme.spacing.md,
    borderWidth: 1,
    borderColor: theme.colors.outlineVariant,
    borderRadius: theme.radii.md,
    backgroundColor: theme.colors.surfaceLow,
  },
  label: { flex: 1, minWidth: 0 },
  overlay: {
    flex: 1,
    justifyContent: 'center',
    alignItems: 'center',
    padding: theme.spacing.md,
    backgroundColor: 'rgba(0, 0, 0, 0.6)',
  },
  dialog: {
    width: '100%',
    maxWidth: 420,
    maxHeight: '90%',
    padding: theme.spacing.md,
    gap: theme.spacing.md,
    backgroundColor: theme.colors.surface,
    borderRadius: theme.radii.lg,
  },
  list: { flexShrink: 1 },
  options: { flexGrow: 0 },
  option: {
    minHeight: 48,
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    padding: theme.spacing.md,
    borderRadius: theme.radii.md,
  },
  selected: { backgroundColor: theme.colors.surfaceHigh },
  cancel: {
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    padding: theme.spacing.sm,
    borderRadius: theme.radii.md,
  },
  pressed: { opacity: 0.75 },
});
