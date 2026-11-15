import { useFocusEffect } from 'expo-router';
import { type ReactNode, useCallback, useRef, useState } from 'react';
import {
  BackHandler,
  Platform,
  StyleSheet,
  View,
  useWindowDimensions,
} from 'react-native';
import { type Food, formatFoodNumber, formatFoodQuantity } from '@/features/foods/food';
import { FoodButton } from '@/features/foods/FoodButton';
import { FoodFormField } from '@/features/foods/FoodFormField';
import { FoodPage } from '@/features/foods/FoodPage';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { type DiaryAmountDraft, formatServingSize, previewDiaryEntry } from './diary';
import { type MealId, mealNames } from './diaryMeals';
import { diaryErrorMessage, isUncertainDiaryWrite } from './diaryApi';
import { DiaryAmountPicker } from './DiaryAmountPicker';

const macros = [
  { key: 'protein', label: 'Protein', short: 'P', color: theme.colors.protein },
  {
    key: 'carbohydrates',
    label: 'Kohlenhydrate',
    short: 'K',
    color: theme.colors.carbohydrates,
  },
  { key: 'fat', label: 'Fett', short: 'F', color: theme.colors.fat },
] as const;

export function DiaryAmountForm({
  food,
  mealId,
  dateLabel,
  initialDraft,
  editing = false,
  feedback,
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
  feedback?: ReactNode;
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
  const preview = result.value;
  const serving = draft.servingSizeId !== undefined;
  const disabled = pending || saved || isUncertainDiaryWrite(saveError);
  const { fontScale } = useWindowDimensions();
  const [macroWidth, setMacroWidth] = useState(0);
  const stackedMacros = fontScale > 1.2 || (macroWidth > 0 && macroWidth < 240);

  useFocusEffect(
    useCallback(() => {
      if (Platform.OS !== 'android' || !pending) return;
      const listener = BackHandler.addEventListener('hardwareBackPress', () => true);
      return () => listener.remove();
    }, [pending]),
  );

  function changeDraft(next: DiaryAmountDraft) {
    if (pending || saved || isUncertainDiaryWrite(saveError)) return;
    setDraft(next);
    setTouched(true);
    setSaveError(undefined);
  }

  async function save() {
    if (submitted.current || !preview || disabled) return;
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
    <FoodPage
      title={editing ? 'Menge bearbeiten' : 'Menge erfassen'}
      onBack={onCancel}
      backLabel={editing ? 'Zurück ohne Änderung' : 'Zurück ohne Hinzufügen'}
      backDisabled={pending}
      contentContainerStyle={styles.content}
      footer={
        <View style={styles.actions}>
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
              pending
                ? 'Wird gespeichert …'
                : editing
                  ? 'Änderungen speichern'
                  : 'Hinzufügen'
            }
            onPress={() => {
              void save();
            }}
            disabled={!preview || disabled}
          />
        </View>
      }
    >
      {feedback}
      <View style={styles.foodHeading}>
        <View style={styles.foodTitle}>
          <AppText
            variant="headlineMd"
            accessibilityRole="header"
            style={styles.foodName}
          >
            {food.name}
          </AppText>
          {food.brand && (
            <AppText variant="bodySm" tone="secondary" style={styles.foodName}>
              {food.brand}
            </AppText>
          )}
        </View>
        <AppText variant="bodySm" tone="accent">
          {mealNames[mealId]} · {dateLabel}
        </AppText>
      </View>
      <Card style={styles.card}>
        {editing ? (
          <AppText tone="secondary">
            {initialDraft?.servingSizeId && food.servingSizes[0]
              ? formatServingSize(food.servingSizes[0], food.referenceUnit)
              : `Direkt in ${food.referenceUnit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}`}
          </AppText>
        ) : food.servingSizes.length > 0 ? (
          <DiaryAmountPicker
            food={food}
            servingSizeId={draft.servingSizeId}
            disabled={disabled}
            onSelect={(servingSizeId) =>
              changeDraft(
                servingSizeId === undefined
                  ? { value: referenceInput }
                  : { servingSizeId, value: '1' },
              )
            }
          />
        ) : null}
        <FoodFormField
          label={serving ? 'Anzahl Portionen' : `Menge (${food.referenceUnit})`}
          numeric
          disabled={disabled}
          value={draft.value}
          onChangeText={(value) => changeDraft({ ...draft, value })}
          error={touched ? result.error : undefined}
        />
        {serving && (
          <AppText variant="bodySm" tone="secondary">
            Auch 0.5 möglich
          </AppText>
        )}
      </Card>
      <Card style={[styles.card, styles.nutritionCard]}>
        <View style={styles.nutritionHeading}>
          <AppText variant="bodyMd" accessibilityRole="header">
            Für deine Menge
          </AppText>
          {preview && (
            <AppText
              variant="bodySm"
              tone="secondary"
              accessibilityLabel={`Gesamtmenge: ${formatFoodQuantity(preview.quantity)} ${food.referenceUnit}`}
            >
              {formatFoodQuantity(preview.quantity)} {food.referenceUnit}
            </AppText>
          )}
        </View>
        {preview ? (
          <>
            <AppText variant="numericLg" tone="accent">
              {formatFoodNumber(preview.nutrition.calories)}
              <AppText variant="bodySm" tone="secondary">
                {' '}
                kcal
              </AppText>
            </AppText>
            <View
              onLayout={(event) => setMacroWidth(event.nativeEvent.layout.width)}
              style={[styles.macros, stackedMacros && styles.stackedMacros]}
            >
              {macros.map(({ key, label, short, color }) => (
                <View
                  key={key}
                  accessible
                  accessibilityLabel={`${label}: ${formatFoodNumber(preview.nutrition[key])} Gramm`}
                  style={[styles.macro, stackedMacros && styles.stackedMacro]}
                >
                  <AppText variant="labelCaps" style={{ color }}>
                    {stackedMacros ? label : short}
                  </AppText>
                  <AppText variant="numericSm">
                    {formatFoodNumber(preview.nutrition[key])} g
                  </AppText>
                </View>
              ))}
            </View>
          </>
        ) : (
          <AppText tone="secondary">
            Gib eine gültige Menge ein, um die Nährwerte zu sehen.
          </AppText>
        )}
      </Card>
    </FoodPage>
  );
}

const styles = StyleSheet.create({
  content: {
    gap: theme.spacing.md,
    paddingTop: theme.spacing.md,
    paddingBottom: theme.spacing.md,
  },
  foodHeading: { gap: theme.spacing.xs },
  foodTitle: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'baseline',
    columnGap: theme.spacing.sm,
  },
  foodName: { flexShrink: 1 },
  card: { padding: theme.spacing.md, gap: theme.spacing.md },
  nutritionCard: { gap: theme.spacing.sm },
  nutritionHeading: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignItems: 'baseline',
    justifyContent: 'space-between',
    gap: theme.spacing.xs,
  },
  macros: { flexDirection: 'row', gap: theme.spacing.md },
  macro: { flex: 1, minWidth: 0, gap: theme.spacing.xs },
  stackedMacros: { flexDirection: 'column' },
  stackedMacro: {
    flex: 0,
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'space-between',
  },
  actions: { gap: theme.spacing.sm },
  error: { color: theme.colors.error },
});
