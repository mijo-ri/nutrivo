import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { Pressable, StyleSheet, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Screen } from '@/shared/ui/Screen';

import { DiaryMealCard } from './DiaryMealCard';
import { DiarySummary } from './DiarySummary';
import { exampleMeals, exampleTargets, sumNutrition } from './diaryExample';

const total = sumNutrition(
    exampleMeals.flatMap(meal => meal.entries.map(entry => entry.nutrition)),
);
const dateFormat = new Intl.DateTimeFormat('de-CH', {
    day: 'numeric',
    month: 'short',
});

export function DiaryScreen() {
    const today = dateFormat.format(new Date());

    return (
        <Screen>
            <View style={styles.dateSection}>
                <View style={styles.dateBar}>
                    <DateArrow direction="left" />
                    <Pressable
                        disabled
                        accessibilityRole="button"
                        accessibilityLabel={`Datum auswählen: Heute, ${today}`}
                        accessibilityState={{ disabled: true }}
                        style={styles.date}
                    >
                        <MaterialDesignIcons
                            name="calendar-blank-outline"
                            size={20}
                            color={theme.colors.primary}
                            accessible={false}
                            aria-hidden
                        />
                        <AppText variant="headlineMd" style={styles.dateText}>
                            Heute, {today}
                        </AppText>
                        <MaterialDesignIcons
                            name="chevron-down"
                            size={18}
                            color={theme.colors.textSecondary}
                            accessible={false}
                            aria-hidden
                        />
                    </Pressable>
                    <DateArrow direction="right" />
                </View>
            </View>
            <DiarySummary total={total} targets={exampleTargets} />
            <View style={styles.meals}>
                {exampleMeals.map(meal => (
                    <DiaryMealCard key={meal.name} meal={meal} />
                ))}
            </View>
        </Screen>
    );
}

function DateArrow({ direction }: { direction: 'left' | 'right' }) {
    return (
        <Pressable
            disabled
            accessibilityRole="button"
            accessibilityLabel={
                direction === 'left' ? 'Vorheriger Tag' : 'Nächster Tag'
            }
            accessibilityState={{ disabled: true }}
            style={styles.dateArrow}
        >
            <MaterialDesignIcons
                name={direction === 'left' ? 'chevron-left' : 'chevron-right'}
                size={24}
                color={theme.colors.textSecondary}
                accessible={false}
                aria-hidden
            />
        </Pressable>
    );
}

const styles = StyleSheet.create({
    dateSection: {
        gap: theme.spacing.sm,
    },
    dateBar: {
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: theme.spacing.xs,
        padding: theme.spacing.xs,
        borderRadius: theme.radii.md,
        backgroundColor: theme.colors.surfaceLow,
    },
    dateArrow: {
        minWidth: 44,
        minHeight: 44,
        alignItems: 'center',
        justifyContent: 'center',
        opacity: 0.45,
    },
    date: {
        minHeight: 44,
        flexShrink: 1,
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'center',
        gap: theme.spacing.xs,
        padding: theme.spacing.xs,
        borderRadius: theme.radii.sm,
        backgroundColor: theme.colors.surface,
        opacity: 0.65,
    },
    dateText: {
        flexShrink: 1,
        textAlign: 'center',
    },
    exampleLabel: {
        textAlign: 'center',
    },
    meals: {
        gap: theme.spacing.lg,
    },
});
