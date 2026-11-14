import { useRef, useState } from 'react';
import { Platform, Pressable, type PressableProps, StyleSheet, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { FoodButton } from './FoodButton';
import { FoodFormField } from './FoodFormField';
import {
  emptyFoodDraft,
  type FoodDraft,
  type ServingDraft,
  validateFoodDraft,
} from './foodForm';
import type { FoodInput } from './food';
import { foodErrorMessage } from './foodsApi';

const nutrients = [
  ['kilocalories', 'Kalorien (kcal)'],
  ['proteinGrams', 'Protein (g)'],
  ['carbohydratesGrams', 'Kohlenhydrate (g)'],
  ['fatGrams', 'Fett (g)'],
] as const;

export function FoodForm({
  initialDraft = emptyFoodDraft,
  saveLabel = 'Lebensmittel speichern',
  onSave,
  onCancel,
}: {
  initialDraft?: FoodDraft;
  saveLabel?: string;
  onSave: (food: FoodInput) => Promise<void>;
  onCancel: () => void;
}) {
  const [draft, setDraft] = useState<FoodDraft>(initialDraft);
  const [submitted, setSubmitted] = useState(false);
  const [saving, setSaving] = useState(false);
  const [saveError, setSaveError] = useState<string>();
  const nextServing = useRef(1);
  const saved = useRef(false);
  const validation = validateFoodDraft(draft);
  const errors = submitted ? validation.errors : {};

  function change<K extends keyof FoodDraft>(key: K, value: FoodDraft[K]) {
    if (saved.current) return;
    setSaveError(undefined);
    setDraft((current) => ({ ...current, [key]: value }));
  }

  function changeServing(
    key: string,
    field: keyof Omit<ServingDraft, 'key' | 'id'>,
    value: string,
  ) {
    if (saved.current) return;
    setSaveError(undefined);
    setDraft((current) => ({
      ...current,
      servingSizes: current.servingSizes.map((serving) =>
        serving.key === key ? { ...serving, [field]: value } : serving,
      ),
    }));
  }

  async function save() {
    setSubmitted(true);
    if (!validation.food || saved.current) {
      return;
    }
    saved.current = true;
    setSaving(true);
    setSaveError(undefined);
    try {
      await onSave(validation.food);
    } catch (error) {
      saved.current = false;
      setSaveError(foodErrorMessage(error));
      setSaving(false);
    }
  }

  return (
    <>
      <AppText tone="secondary">Deine Lebensmittel werden dauerhaft gespeichert.</AppText>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Stammdaten
        </AppText>
        <FoodFormField
          disabled={saving}
          label="Name"
          value={draft.name}
          onChangeText={(value) => change('name', value)}
          error={errors.name}
        />
        <FoodFormField
          disabled={saving}
          label="Marke (optional)"
          value={draft.brand}
          onChangeText={(value) => change('brand', value)}
          error={errors.brand}
        />
      </Card>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Bezugsmenge
        </AppText>
        <AppText tone="secondary">Alle Nährwerte unten gelten für diese Menge.</AppText>
        <FoodFormField
          disabled={saving}
          label="Menge"
          value={draft.referenceQuantity}
          onChangeText={(value) => change('referenceQuantity', value)}
          error={errors.referenceQuantity}
          numeric
        />
        <View
          style={styles.units}
          accessibilityRole="radiogroup"
          accessibilityLabel="Referenzeinheit"
        >
          {(['g', 'ml'] as const).map((unit) => (
            <Pressable
              key={unit}
              disabled={saving}
              {...(Platform.OS === 'web'
                ? ({
                    onKeyDown: (event) => {
                      if (saved.current) return;
                      const { key, repeat } = event.nativeEvent;
                      if (key === ' ') {
                        event.preventDefault();
                        if (!repeat) change('referenceUnit', unit);
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
                        const next =
                          key === 'Home'
                            ? 'g'
                            : key === 'End'
                              ? 'ml'
                              : unit === 'g'
                                ? 'ml'
                                : 'g';
                        change('referenceUnit', next);
                        const target = event.currentTarget as unknown as HTMLElement;
                        const options =
                          target.parentElement?.querySelectorAll<HTMLElement>(
                            '[role="radio"]',
                          );
                        options?.[next === 'g' ? 0 : 1]?.focus();
                      }
                    },
                  } satisfies Pick<PressableProps, 'onKeyDown'>)
                : {})}
              onPress={() => change('referenceUnit', unit)}
              accessibilityRole="radio"
              accessibilityState={{
                checked: draft.referenceUnit === unit,
                disabled: saving,
              }}
              aria-checked={draft.referenceUnit === unit}
              accessibilityLabel={unit === 'g' ? 'Gramm' : 'Milliliter'}
              style={({ pressed }) => [
                styles.unit,
                draft.referenceUnit === unit && styles.selectedUnit,
                pressed && styles.pressed,
              ]}
            >
              <AppText tone={draft.referenceUnit === unit ? 'accent' : 'secondary'}>
                {unit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}
              </AppText>
            </Pressable>
          ))}
        </View>
      </Card>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Nährwerte
        </AppText>
        <AppText tone="secondary">
          Pro {draft.referenceQuantity || '…'} {draft.referenceUnit}
        </AppText>
        {nutrients.map(([key, label]) => (
          <FoodFormField
            disabled={saving}
            key={key}
            label={label}
            value={draft[key]}
            onChangeText={(value) => change(key, value)}
            error={errors[key]}
            numeric
          />
        ))}
      </Card>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Portionsgrössen (optional)
        </AppText>
        <AppText tone="secondary">
          Zum Beispiel: 1 Becher = 150 {draft.referenceUnit}. Die Entsprechung gilt für
          die gesamte angegebene Portion.
        </AppText>
        {draft.servingSizes.map((serving, index) => (
          <View key={serving.key} style={styles.serving}>
            <AppText variant="headlineMd" accessibilityRole="header">
              Portion {index + 1}
            </AppText>
            <FoodFormField
              disabled={saving}
              label="Portionsmenge"
              accessibilityLabel={`Portion ${index + 1}: Menge`}
              value={serving.quantity}
              onChangeText={(value) => changeServing(serving.key, 'quantity', value)}
              error={errors[`${serving.key}.quantity`]}
              numeric
            />
            <FoodFormField
              disabled={saving}
              label="Einheit"
              accessibilityLabel={`Portion ${index + 1}: Einheit`}
              placeholder="z. B. EL, Stück, Becher"
              value={serving.unit}
              onChangeText={(value) => changeServing(serving.key, 'unit', value)}
              error={errors[`${serving.key}.unit`]}
            />
            <FoodFormField
              disabled={saving}
              label="Bezeichnung (optional)"
              accessibilityLabel={`Portion ${index + 1}: Bezeichnung (optional)`}
              placeholder="z. B. Gestrichen"
              value={serving.label}
              onChangeText={(value) => changeServing(serving.key, 'label', value)}
              error={errors[`${serving.key}.label`]}
            />
            <FoodFormField
              disabled={saving}
              label={`Entspricht (${draft.referenceUnit})`}
              accessibilityLabel={`Portion ${index + 1}: Entspricht in ${draft.referenceUnit}`}
              value={serving.equivalentQuantity}
              onChangeText={(value) =>
                changeServing(serving.key, 'equivalentQuantity', value)
              }
              error={errors[`${serving.key}.equivalentQuantity`]}
              numeric
            />
            <FoodButton
              label={`Portion ${index + 1} entfernen`}
              disabled={saving}
              secondary
              onPress={() =>
                setDraft((current) => ({
                  ...current,
                  servingSizes: current.servingSizes.filter(
                    (item) => item.key !== serving.key,
                  ),
                }))
              }
            />
          </View>
        ))}
        <FoodButton
          label="Portionsgrösse hinzufügen"
          disabled={saving}
          secondary
          onPress={() => {
            const key = `serving-${nextServing.current++}`;
            setDraft((current) => ({
              ...current,
              servingSizes: [
                ...current.servingSizes,
                { key, quantity: '1', unit: '', label: '', equivalentQuantity: '' },
              ],
            }));
          }}
        />
      </Card>
      {submitted && !validation.food && (
        <AppText accessibilityRole="alert" style={styles.error}>
          Bitte prüfe die markierten Felder. Das Lebensmittel wurde noch nicht
          gespeichert.
        </AppText>
      )}
      {saveError && (
        <AppText accessibilityRole="alert" style={styles.error}>
          {saveError}
        </AppText>
      )}
      <FoodButton
        label={saving ? 'Wird gespeichert …' : saveLabel}
        disabled={saving}
        onPress={() => {
          void save();
        }}
      />
      <FoodButton label="Abbrechen" secondary disabled={saving} onPress={onCancel} />
    </>
  );
}

const styles = StyleSheet.create({
  units: { flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.sm },
  unit: {
    minHeight: 48,
    justifyContent: 'center',
    padding: theme.spacing.md,
    borderRadius: theme.radii.md,
    borderWidth: 1,
    borderColor: theme.colors.outlineVariant,
  },
  selectedUnit: {
    borderColor: theme.colors.primary,
    backgroundColor: theme.colors.surfaceHigh,
  },
  pressed: { opacity: 0.75 },
  serving: {
    gap: theme.spacing.md,
    borderTopWidth: 1,
    borderTopColor: theme.colors.outlineVariant,
    paddingTop: theme.spacing.lg,
  },
  error: { color: theme.colors.error },
});
