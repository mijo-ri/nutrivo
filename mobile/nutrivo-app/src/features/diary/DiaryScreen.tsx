import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { useIsClient } from '@/shared/hooks/useIsClient';
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
const weekdayDateFormat = new Intl.DateTimeFormat('de-CH', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
});

export function DiaryScreen() {
    const isClient = useIsClient();
    const [selectedDate, setSelectedDate] = useState(() => new Date());

    // Date and responsive labels depend on the device, not the static exporter.
    if (!isClient) {
        return null;
    }

    const isToday = selectedDate.toDateString() === new Date().toDateString();
    const dateLabel = isToday
        ? `Heute, ${dateFormat.format(selectedDate)}`
        : weekdayDateFormat.format(selectedDate);

    function changeDate(days: -1 | 1) {
        setSelectedDate(previousDate => {
            const nextDate = new Date(previousDate);
            nextDate.setDate(nextDate.getDate() + days);
            return nextDate;
        });
    }

    return (
        <Screen>
            <View style={styles.dateSection}>
                <View style={styles.dateBar}>
                    <DateArrow direction="left" onPress={() => changeDate(-1)} />
                    <Pressable
                        disabled
                        accessibilityRole="button"
                        accessibilityLabel={`Datum auswählen: ${dateLabel}`}
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
                            {dateLabel}
                        </AppText>
                        <MaterialDesignIcons
                            name="chevron-down"
                            size={18}
                            color={theme.colors.textSecondary}
                            accessible={false}
                            aria-hidden
                        />
                    </Pressable>
                    <DateArrow direction="right" onPress={() => changeDate(1)} />
                </View>
            </View>
            <DiarySummary total={total} targets={exampleTargets} />
            <View style={styles.meals}>
                {exampleMeals.map(meal => (
                    <DiaryMealCard key={meal.id} meal={meal} />
                ))}
            </View>
        </Screen>
    );
}

function DateArrow({
    direction,
    onPress,
}: {
    direction: 'left' | 'right';
    onPress: () => void;
}) {
    return (
        <Pressable
            onPress={onPress}
            accessibilityRole="button"
            accessibilityLabel={
                direction === 'left' ? 'Vorheriger Tag' : 'Nächster Tag'
            }
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
