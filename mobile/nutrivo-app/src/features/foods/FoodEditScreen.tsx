import { useRouter } from 'expo-router';

import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';

import { FoodButton } from './FoodButton';
import { FoodPage } from './FoodPage';

export function FoodEditScreen() {
    const router = useRouter();
    return (
        <FoodPage title="Lebensmittel bearbeiten">
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">Noch nicht verfügbar</AppText>
                <AppText tone="secondary">Lebensmittel können noch nicht bearbeitet werden.</AppText>
                <FoodButton label="Zur Lebensmittelübersicht" onPress={() => router.dismissTo('/foods')} />
            </Card>
        </FoodPage>
    );
}
