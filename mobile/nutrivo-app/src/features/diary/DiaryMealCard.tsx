import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { Pressable, StyleSheet, View, useWindowDimensions } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import {
    formatNutrition,
    sumNutrition,
    type DiaryExampleMeal,
} from './diaryExample';

export function DiaryMealCard({ meal }: { meal: DiaryExampleMeal }) {
    const { width, fontScale } = useWindowDimensions();
    const compactAction = width < 360 || fontScale > 1.2;
    const calories = sumNutrition(
        meal.entries.map(entry => entry.nutrition),
    ).calories;
    const empty = meal.entries.length === 0;

    return (
        <View style={styles.card}>
            <View style={styles.header}>
                <View style={styles.heading}>
                    <AppText variant="headlineMd" accessibilityRole="header">
                        {meal.name}
                    </AppText>
                    <AppText variant="numericSm" tone="secondary">
                        {formatNutrition(calories)} kcal
                    </AppText>
                </View>
                {empty ? (
                    <AppText variant="labelCaps" tone="secondary">
                        Leer
                    </AppText>
                ) : (
                    <AddEntryButton mealName={meal.name} compact={compactAction} />
                )}
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
                        <AppText style={styles.centered}>
                            Noch keine Einträge
                        </AppText>
                        <AppText
                            variant="bodySm"
                            tone="secondary"
                            style={styles.centered}
                        >
                            Zwischenmahlzeiten oder Shakes hier erfassen
                        </AppText>
                    </View>
                    <AddEntryButton mealName={meal.name} />
                </View>
            ) : meal.entries.map((entry, index) => (
                <View
                    key={entry.name}
                    accessible
                    accessibilityLabel={`${entry.name}, ${entry.quantity}, ${formatNutrition(entry.nutrition.calories)} Kilokalorien`}
                    style={[styles.entry, index % 2 === 1 && styles.alternateEntry]}
                >
                    <View style={styles.food}>
                        <AppText>{entry.name}</AppText>
                        <AppText variant="bodySm" tone="secondary" style={styles.quantity}>
                            {entry.quantity}
                        </AppText>
                    </View>
                    <AppText variant="numericSm" style={styles.calories}>
                        {formatNutrition(entry.nutrition.calories)} kcal
                    </AppText>
                </View>
            ))}
        </View>
    );
}

function AddEntryButton({
    mealName,
    compact = false,
}: {
    mealName: string;
    compact?: boolean;
}) {
    return (
        <Pressable
            disabled
            accessibilityRole="button"
            accessibilityLabel={`Eintrag zu ${mealName} hinzufügen`}
            accessibilityHint="In der Designansicht noch nicht verfügbar"
            accessibilityState={{ disabled: true }}
            style={styles.addButton}
        >
            <MaterialDesignIcons
                name="plus"
                size={20}
                color={theme.colors.primary}
                accessible={false}
                aria-hidden
            />
            {!compact && (
                <AppText variant="labelCaps" tone="accent">
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
        alignItems: 'center',
        justifyContent: 'space-between',
        columnGap: theme.spacing.sm,
        paddingHorizontal: theme.spacing.md,
        paddingVertical: theme.spacing.sm,
        backgroundColor: theme.colors.surfaceHigh,
        minHeight: 60,
    },
    heading: {
        flex: 1,
        flexDirection: 'row',
        flexWrap: 'wrap',
        alignItems: 'baseline',
        columnGap: theme.spacing.sm,
        minWidth: 0,
    },
    addButton: {
        minWidth: 44,
        minHeight: 44,
        flexDirection: 'row',
        flexWrap: 'wrap',
        alignItems: 'center',
        justifyContent: 'center',
        gap: theme.spacing.xs,
        paddingHorizontal: theme.spacing.sm,
        opacity: 0.45,
    },
    entry: {
        minHeight: 52,
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        paddingHorizontal: theme.spacing.md,
        paddingVertical: theme.spacing.sm,
    },
    alternateEntry: {
        backgroundColor: theme.colors.surfaceLow,
    },
    food: {
        flex: 1,
        minWidth: 0,
    },
    quantity: {
        fontVariant: ['tabular-nums'],
    },
    calories: {
        flexShrink: 1,
        textAlign: 'right',
        maxWidth: '40%',
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
