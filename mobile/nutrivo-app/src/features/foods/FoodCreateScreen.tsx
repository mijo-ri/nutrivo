import { useFocusEffect, useRouter } from 'expo-router';
import { useCallback } from 'react';
import { BackHandler, Platform } from 'react-native';

import { FoodForm } from './FoodFormView';
import { FoodPage } from './FoodPage';
import { useCreateFood } from './foodsQueries';

export function FoodCreateScreen() {
    const router = useRouter();
    const create = useCreateFood();

    useFocusEffect(useCallback(() => {
        if (Platform.OS !== 'android' || !create.isPending) return;
        const listener = BackHandler.addEventListener('hardwareBackPress', () => true);
        return () => listener.remove();
    }, [create.isPending]));

    return (
        <FoodPage title="Lebensmittel hinzufügen" backDisabled={create.isPending}>
            <FoodForm
                onSave={async input => {
                    const food = await create.mutateAsync(input);
                    router.replace({ pathname: '/food-details', params: { id: food.id } });
                }}
                onCancel={() => router.dismissTo('/foods')}
            />
        </FoodPage>
    );
}
