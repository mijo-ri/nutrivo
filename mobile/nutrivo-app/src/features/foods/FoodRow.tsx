import { Pressable, StyleSheet, useWindowDimensions, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { type Food, formatFoodNumber, formatFoodQuantity } from './food';

export function FoodRow({
  food,
  onPress,
  accessibilityHint = 'Lebensmitteldetails anzeigen',
}: {
  food: Food;
  onPress?: () => void;
  accessibilityHint?: string;
}) {
  const { width, fontScale } = useWindowDimensions();
  const stacked = width < 360 || fontScale > 1.2;
  const calories = formatFoodNumber(food.kilocalories);
  const reference = formatFoodQuantity(food.referenceQuantity);
  const label = `${food.name}${food.brand ? `, ${food.brand}` : ''}, ${calories} Kilokalorien pro ${reference} ${food.referenceUnit === 'g' ? 'Gramm' : 'Milliliter'}`;
  const content = (
    <>
      <View style={styles.foodName}>
        <AppText variant="bodyLg">{food.name}</AppText>
        {food.brand && <AppText tone="secondary">{food.brand}</AppText>}
      </View>
      <View style={[styles.nutrition, stacked && styles.stackedNutrition]}>
        <AppText variant="numericMd" tone="accent">
          {calories} kcal
        </AppText>
        <AppText variant="bodySm" tone="secondary">
          pro {reference} {food.referenceUnit}
        </AppText>
      </View>
    </>
  );

  return onPress ? (
    <Pressable
      onPress={onPress}
      accessibilityRole="button"
      accessibilityLabel={label}
      accessibilityHint={accessibilityHint}
      style={({ pressed }) => [
        styles.row,
        stacked && styles.stacked,
        pressed && styles.pressed,
      ]}
    >
      {content}
    </Pressable>
  ) : (
    <View
      accessible
      accessibilityLabel={label}
      style={[styles.row, stacked && styles.stacked]}
    >
      {content}
    </View>
  );
}

const styles = StyleSheet.create({
  row: {
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.lg,
    padding: theme.spacing.lg,
    borderRadius: theme.radii.lg,
    backgroundColor: theme.colors.surface,
  },
  stacked: { flexDirection: 'column', alignItems: 'stretch', gap: theme.spacing.sm },
  foodName: { flex: 1, minWidth: 0, gap: theme.spacing.xs },
  nutrition: { flexShrink: 1, alignItems: 'flex-end', gap: theme.spacing.xs },
  stackedNutrition: { alignItems: 'flex-start' },
  pressed: { backgroundColor: theme.colors.surfaceHighest },
});
