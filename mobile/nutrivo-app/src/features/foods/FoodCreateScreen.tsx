import { useRouter } from 'expo-router';

import { FoodForm } from './FoodFormView';
import { FoodPage } from './FoodPage';
import { useFoods } from './FoodsProvider';

export function FoodCreateScreen() {
    const router = useRouter();
    const { addFood } = useFoods();

    return (
        <FoodPage title="Lebensmittel hinzufügen">
            <FoodForm
                onSave={input => {
                    const food = addFood(input);
                    router.replace({ pathname: '/food-details', params: { id: food.id } });
                }}
                onCancel={() => router.dismissTo('/foods')}
            />
        </FoodPage>
    );
}
