import { useLocalSearchParams, useRouter } from 'expo-router';

import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { formatFoodNumber, formatFoodQuantity } from './food';
import { FoodButton } from './FoodButton';
import { FoodPage } from './FoodPage';
import { FoodQueryFeedback } from './FoodQueryFeedback';
import { useFood } from './foodsQueries';

export function FoodDetailsScreen() {
    const { id } = useLocalSearchParams<{ id?: string | string[] }>();
    const router = useRouter();
    const isClient = useIsClient();
    const query = useFood(id);
    const food = query.data;

    return (
        <FoodPage title="Lebensmitteldetails">
            {isClient && (query.invalidId ? (
                <Card>
                    <AppText accessibilityRole="alert">Die Lebensmittel-ID fehlt oder ist ungültig.</AppText>
                    <FoodButton label="Zur Lebensmittelübersicht" onPress={() => router.dismissTo('/foods')} />
                </Card>
            ) : <>
                <FoodQueryFeedback loading={query.isPending} error={query.error} onRetry={query.refetch} />
                {food && (
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
                        <FoodButton label="Löschen" destructive disabled onPress={() => undefined} />
                        <AppText tone="secondary">Löschen: Noch nicht verfügbar.</AppText>
                    </>
                )}
            </>)}
        </FoodPage>
    );
}
