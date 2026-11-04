import { StyleSheet, View, useWindowDimensions } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { formatNutrition, type Nutrition } from './diaryExample';

type DiarySummaryProps = {
    total: Nutrition;
    targets: Nutrition;
};

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

function ProgressRail({
    value,
    target,
    color,
}: {
    value: number;
    target: number;
    color: string;
}) {
    const percent = target > 0
        ? Math.min(100, Math.max(0, value / target * 100))
        : 0;

    return (
        <View style={styles.track}>
            <View
                style={[
                    styles.fill,
                    { width: `${percent}%`, backgroundColor: color },
                ]}
            />
        </View>
    );
}

export function DiarySummary({ total, targets }: DiarySummaryProps) {
    const { width, fontScale } = useWindowDimensions();
    const stacked = width < 360 || fontScale > 1.2;
    const remaining = targets.calories - total.calories;

    return (
        <Card style={styles.card}>
            <View style={[styles.metrics, fontScale > 1.2 && styles.stacked]}>
                <CalorieMetric
                    label="Gegessen"
                    value={total.calories}
                    stacked={fontScale > 1.2}
                />
                <CalorieMetric
                    label="Ziel"
                    value={targets.calories}
                    align="center"
                    stacked={fontScale > 1.2}
                />
                <CalorieMetric
                    label={remaining >= 0 ? 'Verbleibend' : 'Über Ziel'}
                    value={Math.abs(remaining)}
                    align="flex-end"
                    prominent
                    stacked={fontScale > 1.2}
                />
            </View>
            <View
                accessible
                accessibilityLabel={`Kalorien: ${formatNutrition(total.calories)} von ${formatNutrition(targets.calories)} Kilokalorien`}
            >
                <ProgressRail
                    value={total.calories}
                    target={targets.calories}
                    color={theme.colors.primaryContainer}
                />
            </View>
            <View style={[styles.macros, stacked && styles.stacked]}>
                {macros.map(({ key, label, short, color }) => (
                    <View
                        key={key}
                        accessible
                        accessibilityLabel={`${label}: ${formatNutrition(total[key])} von ${formatNutrition(targets[key])} Gramm${total[key] > targets[key] ? ', über dem Beispielziel' : ''}`}
                        style={[styles.macro, stacked && styles.naturalHeight]}
                    >
                        <View style={styles.macroHeading}>
                            <AppText variant="labelCaps" style={{ color }}>
                                {stacked ? label : short}
                            </AppText>
                            <AppText variant="numericSm">
                                {formatNutrition(total[key])}
                                <AppText variant="numericSm" tone="secondary">
                                    /{formatNutrition(targets[key])} g
                                </AppText>
                            </AppText>
                        </View>
                        <ProgressRail
                            value={total[key]}
                            target={targets[key]}
                            color={color}
                        />
                    </View>
                ))}
            </View>
        </Card>
    );
}

function CalorieMetric({
    label,
    value,
    align = 'flex-start',
    prominent = false,
    stacked = false,
}: {
    label: string;
    value: number;
    align?: 'flex-start' | 'center' | 'flex-end';
    prominent?: boolean;
    stacked?: boolean;
}) {
    return (
        <View
            accessible
            accessibilityLabel={`${label}: ${formatNutrition(value)} Kilokalorien`}
            style={[
                styles.metric,
                { alignItems: align },
                stacked && styles.naturalHeight,
            ]}
        >
            <AppText variant="labelCaps" tone={prominent ? 'accent' : 'secondary'}>
                {label}
            </AppText>
            <View style={[styles.value, { justifyContent: align }]}>
                <AppText
                    variant={prominent ? 'numericLg' : 'numericMd'}
                    tone={prominent ? 'accent' : 'default'}
                >
                    {formatNutrition(value)}
                </AppText>
                <AppText variant="bodySm" tone={prominent ? 'accent' : 'secondary'}>
                    kcal
                </AppText>
            </View>
        </View>
    );
}

const styles = StyleSheet.create({
    card: {
        padding: theme.spacing.md,
        gap: theme.spacing.md,
    },
    metrics: {
        flexDirection: 'row',
        alignItems: 'flex-end',
        gap: theme.spacing.xs,
    },
    metric: {
        flex: 1,
        minWidth: 0,
        gap: theme.spacing.xs,
    },
    value: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        alignItems: 'baseline',
        columnGap: theme.spacing.xs,
    },
    track: {
        height: 6,
        borderRadius: 3,
        backgroundColor: theme.colors.surfaceHighest,
        overflow: 'hidden',
    },
    fill: {
        height: '100%',
        borderRadius: 3,
    },
    macros: {
        flexDirection: 'row',
        gap: theme.spacing.md,
        padding: theme.spacing.sm,
        borderRadius: theme.radii.md,
        backgroundColor: theme.colors.surfaceLow,
    },
    macro: {
        flex: 1,
        minWidth: 0,
        gap: theme.spacing.xs,
    },
    macroHeading: {
        flexDirection: 'row',
        flexWrap: 'wrap',
        alignItems: 'baseline',
        justifyContent: 'space-between',
        columnGap: theme.spacing.xs,
    },
    stacked: {
        flexDirection: 'column',
        alignItems: 'stretch',
    },
    naturalHeight: {
        flexGrow: 0,
        flexShrink: 0,
        flexBasis: 'auto',
    },
});
