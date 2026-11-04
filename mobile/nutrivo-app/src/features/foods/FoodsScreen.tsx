import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { Screen } from '@/shared/ui/Screen';

export function FoodsScreen() {
    return (
        <Screen>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">
                    Deine Lebensmittel
                </AppText>
                <AppText variant="bodyLg" tone="secondary">
                    Hier kannst du bald Lebensmittel suchen und verwalten.
                </AppText>
            </Card>
        </Screen>
    );
}
