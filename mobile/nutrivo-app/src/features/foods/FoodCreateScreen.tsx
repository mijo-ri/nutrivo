import { useRouter } from 'expo-router';
import { useRef, useState } from 'react';
import { Pressable, StyleSheet, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { FoodButton } from './FoodButton';
import { FoodFormField } from './FoodFormField';
import { emptyFoodDraft, type FoodDraft, type ServingDraft, validateFoodDraft } from './foodForm';
import { FoodPage } from './FoodPage';
import { useFoods } from './FoodsProvider';

const nutrients = [
    ['kilocalories', 'Kalorien (kcal)'],
    ['proteinGrams', 'Protein (g)'],
    ['carbohydratesGrams', 'Kohlenhydrate (g)'],
    ['fatGrams', 'Fett (g)'],
] as const;

export function FoodCreateScreen() {
    const router = useRouter();
    const { addFood } = useFoods();
    const [draft, setDraft] = useState<FoodDraft>(emptyFoodDraft);
    const [submitted, setSubmitted] = useState(false);
    const nextServing = useRef(1);
    const saved = useRef(false);
    const validation = validateFoodDraft(draft);
    const errors = submitted ? validation.errors : {};

    function change<K extends keyof FoodDraft>(key: K, value: FoodDraft[K]) {
        setDraft(current => ({ ...current, [key]: value }));
    }

    function changeServing(key: string, field: keyof Omit<ServingDraft, 'key'>, value: string) {
        setDraft(current => ({
            ...current,
            servingSizes: current.servingSizes.map(serving => serving.key === key ? { ...serving, [field]: value } : serving),
        }));
    }

    function save() {
        setSubmitted(true);
        if (!validation.food || saved.current) {
            return;
        }
        saved.current = true;
        const food = addFood(validation.food);
        router.replace({ pathname: '/food-details', params: { id: food.id } });
    }

    return (
        <FoodPage title="Lebensmittel hinzufügen">
            <AppText tone="secondary">Deine Lebensmittel bleiben für diese App-Sitzung erhalten.</AppText>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Stammdaten</AppText>
                <FoodFormField
                    label="Name"
                    value={draft.name}
                    onChangeText={value => change('name', value)}
                    error={errors.name}
                />
                <FoodFormField
                    label="Marke (optional)"
                    value={draft.brand}
                    onChangeText={value => change('brand', value)}
                    error={errors.brand}
                />
            </Card>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Bezugsmenge</AppText>
                <AppText tone="secondary">Alle Nährwerte unten gelten für diese Menge.</AppText>
                <FoodFormField
                    label="Menge"
                    value={draft.referenceQuantity}
                    onChangeText={value => change('referenceQuantity', value)}
                    error={errors.referenceQuantity}
                    numeric
                />
                <View style={styles.units} accessibilityRole="radiogroup" accessibilityLabel="Referenzeinheit">
                    {(['g', 'ml'] as const).map(unit => (
                        <Pressable
                            key={unit}
                            onPress={() => change('referenceUnit', unit)}
                            accessibilityRole="radio"
                            accessibilityState={{ checked: draft.referenceUnit === unit }}
                            aria-checked={draft.referenceUnit === unit}
                            accessibilityLabel={unit === 'g' ? 'Gramm' : 'Milliliter'}
                            style={({ pressed }) => [styles.unit, draft.referenceUnit === unit && styles.selectedUnit, pressed && styles.pressed]}
                        >
                            <AppText tone={draft.referenceUnit === unit ? 'accent' : 'secondary'}>{unit === 'g' ? 'Gramm (g)' : 'Milliliter (ml)'}</AppText>
                        </Pressable>
                    ))}
                </View>
            </Card>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Nährwerte</AppText>
                <AppText tone="secondary">Pro {draft.referenceQuantity || '…'} {draft.referenceUnit}</AppText>
                {nutrients.map(([key, label]) => (
                    <FoodFormField
                        key={key}
                        label={label}
                        value={draft[key]}
                        onChangeText={value => change(key, value)}
                        error={errors[key]}
                        numeric
                    />
                ))}
            </Card>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Portionsgrössen (optional)</AppText>
                <AppText tone="secondary">Zum Beispiel: 1 Becher = 150 {draft.referenceUnit}. Die Entsprechung gilt für die gesamte angegebene Portion.</AppText>
                {draft.servingSizes.map((serving, index) => (
                    <View key={serving.key} style={styles.serving}>
                        <AppText variant="headlineMd" accessibilityRole="header">Portion {index + 1}</AppText>
                        <FoodFormField
                            label="Portionsmenge"
                            accessibilityLabel={`Portion ${index + 1}: Menge`}
                            value={serving.quantity}
                            onChangeText={value => changeServing(serving.key, 'quantity', value)}
                            error={errors[`${serving.key}.quantity`]}
                            numeric
                        />
                        <FoodFormField
                            label="Einheit"
                            accessibilityLabel={`Portion ${index + 1}: Einheit`}
                            placeholder="z. B. EL, Stück, Becher"
                            value={serving.unit}
                            onChangeText={value => changeServing(serving.key, 'unit', value)}
                            error={errors[`${serving.key}.unit`]}
                        />
                        <FoodFormField
                            label="Bezeichnung (optional)"
                            accessibilityLabel={`Portion ${index + 1}: Bezeichnung (optional)`}
                            placeholder="z. B. Gestrichen"
                            value={serving.label}
                            onChangeText={value => changeServing(serving.key, 'label', value)}
                            error={errors[`${serving.key}.label`]}
                        />
                        <FoodFormField
                            label={`Entspricht (${draft.referenceUnit})`}
                            accessibilityLabel={`Portion ${index + 1}: Entspricht in ${draft.referenceUnit}`}
                            value={serving.equivalentQuantity}
                            onChangeText={value => changeServing(serving.key, 'equivalentQuantity', value)}
                            error={errors[`${serving.key}.equivalentQuantity`]}
                            numeric
                        />
                        <FoodButton
                            label={`Portion ${index + 1} entfernen`}
                            secondary
                            onPress={() => setDraft(current => ({
                                ...current,
                                servingSizes: current.servingSizes.filter(item => item.key !== serving.key),
                            }))}
                        />
                    </View>
                ))}
                <FoodButton
                    label="Portionsgrösse hinzufügen"
                    secondary
                    onPress={() => {
                        const key = `serving-${nextServing.current++}`;
                        setDraft(current => ({
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
                <AppText accessibilityRole="alert" style={styles.error}>Bitte prüfe die markierten Felder. Das Lebensmittel wurde noch nicht gespeichert.</AppText>
            )}
            <FoodButton label="Lebensmittel speichern" onPress={save} />
            <FoodButton label="Abbrechen" secondary onPress={() => router.dismissTo('/foods')} />
        </FoodPage>
    );
}

const styles = StyleSheet.create({
    units: { flexDirection: 'row', flexWrap: 'wrap', gap: theme.spacing.sm },
    unit: {
        minHeight: 48, justifyContent: 'center', padding: theme.spacing.md, borderRadius: theme.radii.md,
        borderWidth: 1, borderColor: theme.colors.outlineVariant,
    },
    selectedUnit: { borderColor: theme.colors.primary, backgroundColor: theme.colors.surfaceHigh },
    pressed: { opacity: 0.75 },
    serving: { gap: theme.spacing.md, borderTopWidth: 1, borderTopColor: theme.colors.outlineVariant, paddingTop: theme.spacing.lg },
    error: { color: theme.colors.error },
});
