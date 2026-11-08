import { useLocalSearchParams, useRouter } from 'expo-router';
import { useRef, useState } from 'react';

import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { formatFoodNumber, formatFoodQuantity } from './food';
import { FoodButton } from './FoodButton';
import { FoodDeleteDialog } from './FoodDeleteDialog';
import { FoodPage } from './FoodPage';
import { useFoods } from './FoodsProvider';

export function FoodDetailsScreen() {
    const { id } = useLocalSearchParams<{ id?: string | string[] }>();
    const { foods, deleteFood } = useFoods();
    const [confirmDelete, setConfirmDelete] = useState(false);
    const deleted = useRef(false);
    const router = useRouter();
    const isClient = useIsClient();
    const food = typeof id === 'string' ? foods.find(item => item.id === id) : undefined;

    return (
        <FoodPage title="Lebensmitteldetails">
            {isClient && (food ? (
                <>
                    <Card>
                        <AppText variant="headlineLg" accessibilityRole="header">{food.name}</AppText>
                        {food.brand && <AppText variant="bodyLg" tone="secondary">{food.brand}</AppText>}
                    </Card>
                    <Card>
                        <AppText variant="headlineMd" accessibilityRole="header">Nährwerte</AppText>
                        <AppText tone="secondary">Pro {formatFoodQuantity(food.referenceQuantity)} {food.referenceUnit}</AppText>
                        <AppText variant="numericLg" tone="accent">{formatFoodNumber(food.kilocalories)} kcal</AppText>
                        <AppText>Protein: {formatFoodNumber(food.proteinGrams)} g</AppText>
                        <AppText>Kohlenhydrate: {formatFoodNumber(food.carbohydratesGrams)} g</AppText>
                        <AppText>Fett: {formatFoodNumber(food.fatGrams)} g</AppText>
                    </Card>
                    <Card>
                        <AppText variant="headlineMd" accessibilityRole="header">Portionsgrössen</AppText>
                        {food.servingSizes.length === 0 && <AppText tone="secondary">Keine zusätzlichen Portionsgrössen erfasst.</AppText>}
                        {food.servingSizes.map(serving => (
                            <AppText key={serving.id}>
                                {formatFoodQuantity(serving.quantity)} {serving.unit}{serving.label ? ` (${serving.label})` : ''} = {formatFoodQuantity(serving.equivalentQuantity)} {food.referenceUnit}
                            </AppText>
                        ))}
                    </Card>
                    <FoodButton label="Bearbeiten" onPress={() => router.push({ pathname: '/food-edit', params: { id: food.id } })} />
                    <FoodButton label="Löschen" destructive onPress={() => setConfirmDelete(true)} />
                    {confirmDelete && (
                        <FoodDeleteDialog
                            name={food.name}
                            onCancel={() => setConfirmDelete(false)}
                            onConfirm={() => {
                                if (deleted.current) return;
                                deleted.current = true;
                                setConfirmDelete(false);
                                deleteFood(food.id);
                                router.dismissTo('/foods');
                            }}
                        />
                    )}
                </>
            ) : (
                <Card>
                    <AppText variant="headlineMd" accessibilityRole="header">Lebensmittel nicht gefunden</AppText>
                    <AppText tone="secondary">Dieses Lebensmittel ist in dieser App-Sitzung nicht verfügbar.</AppText>
                    <FoodButton label="Zur Lebensmittelübersicht" onPress={() => router.dismissTo('/foods')} />
                </Card>
            ))}
        </FoodPage>
    );
}
