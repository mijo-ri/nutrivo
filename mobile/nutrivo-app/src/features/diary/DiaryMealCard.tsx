import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useRouter } from 'expo-router';
import { Pressable, StyleSheet, View, useWindowDimensions } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { type DiaryMeal } from './diary';
import { DiaryEntryRow } from './DiaryEntryRow';
import { formatNutrition } from './nutrition';
import { type MealId } from './diaryMeals';

export function DiaryMealCard({ meal, date }: { meal: DiaryMeal; date: string }) {
  const { width, fontScale } = useWindowDimensions();
  const compactAction = width < 360 || fontScale > 1.2;
  const nutrition = meal.nutrition;
  const empty = meal.entries.length === 0;

  return (
    <View style={styles.card}>
      <View style={styles.header}>
        <View style={styles.heading}>
          <AppText variant="headlineMd" accessibilityRole="header">
            {meal.name}
          </AppText>
          <View
            accessible
            accessibilityLabel={`Protein: ${formatNutrition(nutrition.protein)} Gramm, Kohlenhydrate: ${formatNutrition(nutrition.carbohydrates)} Gramm, Fett: ${formatNutrition(nutrition.fat)} Gramm`}
            style={styles.macros}
          >
            <AppText variant="bodySm" tone="secondary">
              P {formatNutrition(nutrition.protein)} g
            </AppText>
            <AppText variant="bodySm" tone="secondary">
              K {formatNutrition(nutrition.carbohydrates)} g
            </AppText>
            <AppText variant="bodySm" tone="secondary">
              F {formatNutrition(nutrition.fat)} g
            </AppText>
          </View>
        </View>
        <AppText variant="headlineMd" style={styles.mealCalories}>
          {formatNutrition(nutrition.calories)} kcal
        </AppText>
      </View>
      {empty ? (
        <View style={styles.empty}>
          <View style={styles.emptyIcon}>
            <MaterialDesignIcons
              name="cookie-outline"
              size={20}
              color={theme.colors.textSecondary}
              accessible={false}
              aria-hidden
            />
          </View>
          <View style={styles.emptyText}>
            <AppText style={styles.centered}>Noch keine Einträge</AppText>
            <AppText variant="bodySm" tone="secondary" style={styles.centered}>
              Füge ein Lebensmittel zu dieser Mahlzeit hinzu
            </AppText>
          </View>
        </View>
      ) : (
        meal.entries.map((entry, index) => (
          <DiaryEntryRow key={entry.id} entry={entry} alternate={index % 2 === 1} />
        ))
      )}
      <View style={styles.footer}>
        <AddEntryButton
          mealId={meal.id}
          mealName={meal.name}
          date={date}
          compact={compactAction}
        />
      </View>
    </View>
  );
}

function AddEntryButton({
  mealId,
  mealName,
  date,
  compact = false,
}: {
  mealId: MealId;
  mealName: string;
  date: string;
  compact?: boolean;
}) {
  const router = useRouter();

  return (
    <Pressable
      onPress={() =>
        router.push({
          pathname: '/food-selection',
          params: { meal: mealId, date },
        })
      }
      accessibilityRole="button"
      accessibilityLabel={`Eintrag zu ${mealName} hinzufügen`}
      accessibilityHint="Öffnet die Lebensmittelsuche"
      hitSlop={theme.spacing.sm}
      style={({ pressed }) => [styles.addButton, pressed && styles.pressedButton]}
    >
      <MaterialDesignIcons
        name="plus"
        size={16}
        color={theme.colors.onPrimary}
        accessible={false}
        aria-hidden
      />
      {!compact && (
        <AppText variant="labelCaps" style={styles.addButtonText}>
          Hinzufügen
        </AppText>
      )}
    </Pressable>
  );
}

const styles = StyleSheet.create({
  card: {
    borderRadius: theme.radii.lg,
    overflow: 'hidden',
    backgroundColor: theme.colors.surface,
  },
  header: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignContent: 'center',
    alignItems: 'flex-start',
    justifyContent: 'space-between',
    gap: theme.spacing.sm,
    paddingHorizontal: theme.spacing.md,
    paddingVertical: theme.spacing.sm,
    backgroundColor: theme.colors.surfaceHigh,
    minHeight: 60,
  },
  heading: {
    flexGrow: 1,
    flexShrink: 1,
    flexBasis: 180,
    gap: theme.spacing.xs,
    minWidth: 0,
  },
  macros: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    columnGap: theme.spacing.md,
    rowGap: theme.spacing.xs,
  },
  mealCalories: {
    flexShrink: 1,
    maxWidth: '100%',
    marginLeft: 'auto',
    textAlign: 'right',
    fontVariant: ['tabular-nums'],
  },
  footer: {
    alignItems: 'flex-end',
    paddingHorizontal: theme.spacing.md,
    paddingVertical: theme.spacing.sm,
  },
  addButton: {
    minWidth: 28,
    minHeight: 28,
    flexDirection: 'row',
    flexWrap: 'wrap',
    alignContent: 'center',
    alignItems: 'center',
    justifyContent: 'center',
    gap: theme.spacing.xs,
    paddingHorizontal: theme.spacing.sm,
    paddingVertical: theme.spacing.xs,
    borderRadius: theme.radii.full,
    backgroundColor: theme.colors.primary,
  },
  pressedButton: {
    opacity: 0.75,
  },
  addButtonText: {
    color: theme.colors.onPrimary,
  },
  empty: {
    padding: theme.spacing.lg,
    alignItems: 'center',
    gap: theme.spacing.sm,
    backgroundColor: theme.colors.surfaceLow,
  },
  emptyIcon: {
    width: 40,
    height: 40,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: theme.radii.full,
    backgroundColor: theme.colors.surfaceHighest,
  },
  emptyText: {
    gap: theme.spacing.xs,
  },
  centered: {
    textAlign: 'center',
  },
});
