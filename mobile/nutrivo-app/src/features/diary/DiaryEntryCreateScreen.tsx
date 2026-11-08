import { useLocalSearchParams, useRouter } from 'expo-router';
import { useRef, useState } from 'react';
import { Platform, Pressable, type PressableProps, StyleSheet, View } from 'react-native';

import { type Food, formatFoodNumber, formatFoodQuantity } from '@/features/foods/food';
import { FoodButton } from '@/features/foods/FoodButton';
import { FoodFormField } from '@/features/foods/FoodFormField';
import { FoodPage } from '@/features/foods/FoodPage';
import { useFood } from '@/features/foods/foodsQueries';
import { FoodQueryFeedback } from '@/features/foods/FoodQueryFeedback';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { type DiaryAmountDraft, formatServingSize, isMealId, parseDiaryDate, previewDiaryEntry } from './diary';
import { type MealId, mealNames } from './diaryMeals';
import { useDiary } from './DiaryProvider';

const dateFormat = new Intl.DateTimeFormat('de-CH', { dateStyle: 'long' });

export function DiaryEntryCreateScreen() {
    const { foodId, meal, date } = useLocalSearchParams<{
        foodId?: string | string[];
        meal?: string | string[];
        date?: string | string[];
    }>();
    const router = useRouter();
    const isClient = useIsClient();
    const parsedDate = parseDiaryDate(date);
    const validContext = isMealId(meal) && typeof date === 'string' && !!parsedDate;
    const query = useFood(foodId, validContext);
    const food = query.data;
    const valid = food && isMealId(meal) && typeof date === 'string' && parsedDate;

    function goBack() {
        if (router.canGoBack()) {
            router.back();
        } else {
            router.replace('/');
        }
    }

    return (
        <FoodPage title="Menge erfassen" onBack={goBack} backLabel="Zurück ohne Hinzufügen">
            {isClient && validContext && !query.invalidId && (
                <FoodQueryFeedback loading={query.isPending} error={query.error} onRetry={query.refetch} />
            )}
            {isClient && (valid ? (
                <EntryForm
                    key={`${food.id}-${meal}-${date}`}
                    food={food}
                    mealId={meal}
                    date={date}
                    dateLabel={dateFormat.format(parsedDate)}
                    onCancel={goBack}
                    onAdded={() => router.dismissTo({ pathname: '/food-selection', params: { meal, date } })}
                />
            ) : !validContext || query.invalidId ? (
                <Card>
                    <AppText variant="headlineMd" accessibilityRole="header">Eintrag nicht möglich</AppText>
                    <AppText tone="secondary">
                        Lebensmittel, Datum oder Mahlzeit ist nicht verfügbar. Bitte wähle im Tagebuch erneut eine Mahlzeit und ein Lebensmittel aus.
                    </AppText>
                    <FoodButton label="Zum Tagebuch" onPress={() => router.dismissTo('/')} />
                </Card>
            ) : null)}
        </FoodPage>
    );
}

function EntryForm({ food, mealId, date, dateLabel, onCancel, onAdded }: {
    food: Food;
    mealId: MealId;
    date: string;
    dateLabel: string;
    onCancel: () => void;
    onAdded: () => void;
}) {
    const { addEntry } = useDiary();
    const referenceInput = food.referenceQuantity.toLocaleString('en-US', { useGrouping: false, maximumSignificantDigits: 21 });
    const [draft, setDraft] = useState<DiaryAmountDraft>(() => ({
        servingSizeId: food.servingSizes[0]?.id,
        value: food.servingSizes.length ? '1' : referenceInput,
    }));
    const [touched, setTouched] = useState(false);
    const [saveError, setSaveError] = useState<string>();
    const [saved, setSaved] = useState(false);
    const submitted = useRef(false);
    const result = previewDiaryEntry(food, draft);
    const serving = draft.servingSizeId !== undefined;

    function changeDraft(next: DiaryAmountDraft) {
        setDraft(next);
        setTouched(true);
        setSaveError(undefined);
    }

    function save() {
        if (submitted.current) {
            return;
        }
        setTouched(true);
        submitted.current = true;
        const outcome = addEntry(food, date, mealId, draft);
        if (!outcome.entry) {
            submitted.current = false;
            setSaveError(outcome.error);
            return;
        }
        setSaved(true);
        onAdded();
    }

    return (
        <>
            <Card>
                <AppText variant="headlineLg" accessibilityRole="header">{food.name}</AppText>
                {food.brand && <AppText tone="secondary">{food.brand}</AppText>}
                <AppText tone="accent">{mealNames[mealId]} · {dateLabel}</AppText>
            </Card>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Mengenangabe</AppText>
                <View accessibilityRole="radiogroup" accessibilityLabel="Menge oder Portionsgrösse" style={styles.options}>
                    <AmountOption
                        label={`Direkt in ${food.referenceUnit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}`}
                        selected={!serving}
                        onPress={() => {
                            if (serving) changeDraft({ value: referenceInput });
                        }}
                    />
                    {food.servingSizes.map(size => (
                        <AmountOption
                            key={size.id}
                            label={formatServingSize(size, food.referenceUnit)}
                            selected={draft.servingSizeId === size.id}
                            onPress={() => {
                                if (draft.servingSizeId !== size.id) changeDraft({ servingSizeId: size.id, value: '1' });
                            }}
                        />
                    ))}
                </View>
                <FoodFormField
                    label={serving ? 'Anzahl Portionen' : `Menge (${food.referenceUnit})`}
                    numeric
                    value={draft.value}
                    onChangeText={value => changeDraft({ ...draft, value })}
                    error={touched ? result.error : undefined}
                />
                {serving && <AppText variant="bodySm" tone="secondary">Die Anzahl gilt für die gesamte gewählte Portion. Auch halbe Portionen wie 0,5 sind möglich.</AppText>}
            </Card>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Für deine Menge</AppText>
                {result.value ? (
                    <>
                        <AppText tone="secondary">Gesamtmenge: {formatFoodQuantity(result.value.quantity)} {food.referenceUnit}</AppText>
                        <AppText variant="numericLg" tone="accent">{formatFoodNumber(result.value.nutrition.calories)} kcal</AppText>
                        <AppText>Protein: {formatFoodNumber(result.value.nutrition.protein)} g</AppText>
                        <AppText>Kohlenhydrate: {formatFoodNumber(result.value.nutrition.carbohydrates)} g</AppText>
                        <AppText>Fett: {formatFoodNumber(result.value.nutrition.fat)} g</AppText>
                    </>
                ) : <AppText tone="secondary">Gib eine gültige Menge ein, um die Nährwerte zu sehen.</AppText>}
            </Card>
            {saveError && <AppText accessibilityRole="alert" style={styles.error}>{saveError}</AppText>}
            <FoodButton label="Hinzufügen" onPress={save} disabled={!result.value || saved} />
            <FoodButton label="Abbrechen" secondary onPress={onCancel} />
        </>
    );
}

function AmountOption({ label, selected, onPress }: { label: string; selected: boolean; onPress: () => void }) {
    // React Native Web activates radio Pressables with Enter, but not Space/arrows.
    const keyboardProps: Pick<PressableProps, 'onKeyDown'> = Platform.OS === 'web' ? {
        onKeyDown: event => {
            const { key, repeat } = event.nativeEvent;
            if (key === ' ') {
                event.preventDefault();
                if (!repeat) onPress();
                return;
            }
            if (!['ArrowDown', 'ArrowRight', 'ArrowUp', 'ArrowLeft', 'Home', 'End'].includes(key)) {
                return;
            }
            event.preventDefault();
            const target = event.currentTarget as unknown as HTMLElement;
            const options = Array.from(target.parentElement?.querySelectorAll<HTMLElement>('[role="radio"]') ?? []);
            const index = options.indexOf(target);
            const next = key === 'Home' ? 0 : key === 'End' ? options.length - 1
                : (index + (key === 'ArrowDown' || key === 'ArrowRight' ? 1 : -1) + options.length) % options.length;
            options[next]?.focus();
            options[next]?.click();
        },
    } : {};

    return (
        <Pressable
            {...keyboardProps}
            onPress={onPress}
            accessibilityRole="radio"
            accessibilityLabel={label}
            accessibilityState={{ checked: selected }}
            aria-checked={selected}
            style={({ pressed }) => [styles.option, selected && styles.selected, pressed && styles.pressed]}
        >
            <View style={[styles.radio, selected && styles.checked]} accessible={false} aria-hidden>
                {selected && <View style={styles.dot} />}
            </View>
            <AppText style={styles.optionLabel} tone={selected ? 'accent' : 'default'}>{label}</AppText>
        </Pressable>
    );
}

const styles = StyleSheet.create({
    options: { gap: theme.spacing.sm },
    option: {
        minHeight: 48, flexDirection: 'row', alignItems: 'center', gap: theme.spacing.sm,
        padding: theme.spacing.md, borderWidth: 1, borderColor: theme.colors.outlineVariant,
        borderRadius: theme.radii.md, backgroundColor: theme.colors.surfaceLow,
    },
    optionLabel: { flex: 1 },
    selected: { borderColor: theme.colors.primary, backgroundColor: theme.colors.surfaceHigh },
    pressed: { opacity: 0.75 },
    radio: {
        width: 20, height: 20, borderRadius: 10, borderWidth: 2,
        borderColor: theme.colors.outline, alignItems: 'center', justifyContent: 'center',
    },
    checked: { borderColor: theme.colors.primary },
    dot: { width: 10, height: 10, borderRadius: 5, backgroundColor: theme.colors.primary },
    error: { color: theme.colors.error },
});
