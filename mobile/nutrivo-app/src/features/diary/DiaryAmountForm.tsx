import { useRef, useState } from 'react';
import { Platform, Pressable, type PressableProps, StyleSheet, View } from 'react-native';
import { type Food, formatFoodNumber, formatFoodQuantity } from '@/features/foods/food';
import { FoodButton } from '@/features/foods/FoodButton';
import { FoodFormField } from '@/features/foods/FoodFormField';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { type DiaryAmountDraft, formatServingSize, previewDiaryEntry } from './diary';
import { type MealId, mealNames } from './diaryMeals';
import { diaryErrorMessage, isUncertainDiaryWrite } from './diaryApi';

export function DiaryAmountForm({
  food,
  mealId,
  dateLabel,
  initialDraft,
  editing = false,
  onCancel,
  onSubmit,
  onSaved,
  onReconcile,
}: {
  food: Food;
  mealId: MealId;
  dateLabel: string;
  initialDraft?: DiaryAmountDraft;
  editing?: boolean;
  onCancel: () => void;
  onSubmit: (draft: DiaryAmountDraft) => Promise<void>;
  onSaved: () => void;
  onReconcile: () => void;
}) {
  const referenceInput = food.referenceQuantity.toLocaleString('en-US', {
    useGrouping: false,
    maximumSignificantDigits: 21,
  });
  const [draft, setDraft] = useState<DiaryAmountDraft>(
    () =>
      initialDraft ?? {
        servingSizeId: food.servingSizes[0]?.id,
        value: food.servingSizes.length ? '1' : referenceInput,
      },
  );
  const [touched, setTouched] = useState(false);
  const [saveError, setSaveError] = useState<unknown>();
  const [pending, setPending] = useState(false);
  const [saved, setSaved] = useState(false);
  const submitted = useRef(false);
  const result = previewDiaryEntry(food, draft);
  const serving = draft.servingSizeId !== undefined;

  function changeDraft(next: DiaryAmountDraft) {
    if (pending || saved || isUncertainDiaryWrite(saveError)) return;
    setDraft(next);
    setTouched(true);
    setSaveError(undefined);
  }

  async function save() {
    if (submitted.current || !result.value) return;
    setTouched(true);
    submitted.current = true;
    setPending(true);
    setSaveError(undefined);
    try {
      await onSubmit(draft);
      setSaved(true);
      onSaved();
    } catch (error) {
      submitted.current = false;
      setSaveError(error);
    } finally {
      setPending(false);
    }
  }

  return (
    <>
      <Card>
        <AppText variant="headlineLg" accessibilityRole="header">
          {food.name}
        </AppText>
        {food.brand && <AppText tone="secondary">{food.brand}</AppText>}
        <AppText tone="accent">
          {mealNames[mealId]} · {dateLabel}
        </AppText>
      </Card>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Mengenangabe
        </AppText>
        {editing ? (
          <AppText tone="secondary">
            {initialDraft?.servingSizeId && food.servingSizes[0]
              ? formatServingSize(food.servingSizes[0], food.referenceUnit)
              : `Direkt in ${food.referenceUnit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}`}
          </AppText>
        ) : (
          <View
            accessibilityRole="radiogroup"
            accessibilityLabel="Menge oder Portionsgrösse"
            style={styles.options}
          >
            <AmountOption
              label={`Direkt in ${food.referenceUnit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}`}
              selected={!serving}
              onPress={() => {
                if (serving) changeDraft({ value: referenceInput });
              }}
            />
            {food.servingSizes.map((size) => (
              <AmountOption
                key={size.id}
                label={formatServingSize(size, food.referenceUnit)}
                selected={draft.servingSizeId === size.id}
                onPress={() => {
                  if (draft.servingSizeId !== size.id)
                    changeDraft({ servingSizeId: size.id, value: '1' });
                }}
              />
            ))}
          </View>
        )}
        <FoodFormField
          label={serving ? 'Anzahl Portionen' : `Menge (${food.referenceUnit})`}
          numeric
          disabled={pending || saved || isUncertainDiaryWrite(saveError)}
          value={draft.value}
          onChangeText={(value) => changeDraft({ ...draft, value })}
          error={touched ? result.error : undefined}
        />
        {serving && (
          <AppText variant="bodySm" tone="secondary">
            Die Anzahl gilt für die gesamte gewählte Portion. Auch halbe Portionen wie 0,5
            sind möglich.
          </AppText>
        )}
      </Card>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Für deine Menge
        </AppText>
        {result.value ? (
          <>
            <AppText tone="secondary">
              Gesamtmenge: {formatFoodQuantity(result.value.quantity)}{' '}
              {food.referenceUnit}
            </AppText>
            <AppText variant="numericLg" tone="accent">
              {formatFoodNumber(result.value.nutrition.calories)} kcal
            </AppText>
            <AppText>
              Protein: {formatFoodNumber(result.value.nutrition.protein)} g
            </AppText>
            <AppText>
              Kohlenhydrate: {formatFoodNumber(result.value.nutrition.carbohydrates)} g
            </AppText>
            <AppText>Fett: {formatFoodNumber(result.value.nutrition.fat)} g</AppText>
          </>
        ) : (
          <AppText tone="secondary">
            Gib eine gültige Menge ein, um die Nährwerte zu sehen.
          </AppText>
        )}
      </Card>
      {!!saveError && (
        <AppText accessibilityRole="alert" style={styles.error}>
          {diaryErrorMessage(saveError)}
        </AppText>
      )}
      {isUncertainDiaryWrite(saveError) && (
        <FoodButton label="Tagebuch prüfen" secondary onPress={onReconcile} />
      )}
      <FoodButton
        label={
          pending ? 'Wird gespeichert …' : editing ? 'Änderungen speichern' : 'Hinzufügen'
        }
        onPress={() => {
          void save();
        }}
        disabled={!result.value || saved || pending || isUncertainDiaryWrite(saveError)}
      />
      <FoodButton label="Abbrechen" secondary onPress={onCancel} disabled={pending} />
    </>
  );
}

function AmountOption({
  label,
  selected,
  onPress,
}: {
  label: string;
  selected: boolean;
  onPress: () => void;
}) {
  // React Native Web activates radio Pressables with Enter, but not Space/arrows.
  const keyboardProps: Pick<PressableProps, 'onKeyDown'> =
    Platform.OS === 'web'
      ? {
          onKeyDown: (event) => {
            const { key, repeat } = event.nativeEvent;
            if (key === ' ') {
              event.preventDefault();
              if (!repeat) onPress();
              return;
            }
            if (
              ![
                'ArrowDown',
                'ArrowRight',
                'ArrowUp',
                'ArrowLeft',
                'Home',
                'End',
              ].includes(key)
            ) {
              return;
            }
            event.preventDefault();
            const target = event.currentTarget as unknown as HTMLElement;
            const options = Array.from(
              target.parentElement?.querySelectorAll<HTMLElement>('[role="radio"]') ?? [],
            );
            const index = options.indexOf(target);
            const next =
              key === 'Home'
                ? 0
                : key === 'End'
                  ? options.length - 1
                  : (index +
                      (key === 'ArrowDown' || key === 'ArrowRight' ? 1 : -1) +
                      options.length) %
                    options.length;
            options[next]?.focus();
            options[next]?.click();
          },
        }
      : {};

  return (
    <Pressable
      {...keyboardProps}
      onPress={onPress}
      accessibilityRole="radio"
      accessibilityLabel={label}
      accessibilityState={{ checked: selected }}
      aria-checked={selected}
      style={({ pressed }) => [
        styles.option,
        selected && styles.selected,
        pressed && styles.pressed,
      ]}
    >
      <View
        style={[styles.radio, selected && styles.checked]}
        accessible={false}
        aria-hidden
      >
        {selected && <View style={styles.dot} />}
      </View>
      <AppText style={styles.optionLabel} tone={selected ? 'accent' : 'default'}>
        {label}
      </AppText>
    </Pressable>
  );
}

const styles = StyleSheet.create({
  options: { gap: theme.spacing.sm },
  option: {
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
  optionLabel: { flex: 1 },
  selected: {
    borderColor: theme.colors.primary,
    backgroundColor: theme.colors.surfaceHigh,
  },
  pressed: { opacity: 0.75 },
  radio: {
    width: 20,
    height: 20,
    borderRadius: 10,
    borderWidth: 2,
    borderColor: theme.colors.outline,
    alignItems: 'center',
    justifyContent: 'center',
  },
  checked: { borderColor: theme.colors.primary },
  dot: { width: 10, height: 10, borderRadius: 5, backgroundColor: theme.colors.primary },
  error: { color: theme.colors.error },
});
