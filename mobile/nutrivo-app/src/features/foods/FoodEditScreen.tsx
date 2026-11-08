import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import { BackHandler, Platform } from 'react-native';

import { useIsClient } from '@/shared/hooks/useIsClient';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { FoodButton } from './FoodButton';
import { FoodForm } from './FoodFormView';
import { foodToDraft } from './foodForm';
import { FoodPage } from './FoodPage';
import { useFoods } from './FoodsProvider';

export function FoodEditScreen() {
    const { id } = useLocalSearchParams<{ id?: string | string[] }>();
    const router = useRouter();
    const { foods, updateFood } = useFoods();
    const isClient = useIsClient();
    const [missing, setMissing] = useState(false);
    const food = !missing && typeof id === 'string' ? foods.find(item => item.id === id) : undefined;
    const returnToDetails = useCallback(() => {
        router.dismissTo(typeof id === 'string'
            ? { pathname: '/food-details', params: { id } }
            : '/foods');
    }, [id, router]);

    useFocusEffect(useCallback(() => {
        if (Platform.OS !== 'android') return;
        const listener = BackHandler.addEventListener('hardwareBackPress', () => {
            returnToDetails();
            return true;
        });
        return () => listener.remove();
    }, [returnToDetails]));

    return (
        <FoodPage title="Lebensmittel bearbeiten" onBack={returnToDetails} backLabel="Zurück ohne Speichern">
            {isClient && (food ? (
                <FoodForm
                    key={food.id}
                    initialDraft={foodToDraft(food)}
                    saveLabel="Änderungen speichern"
                    onSave={input => {
                        if (updateFood(food.id, input)) {
                            returnToDetails();
                        } else {
                            setMissing(true);
                        }
                    }}
                    onCancel={returnToDetails}
                />
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
