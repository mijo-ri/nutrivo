import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { Screen } from '@/shared/ui/Screen';

export function DiaryScreen() {
    return (
        <Screen>
            <Card>
                <AppText variant="headlineMd" accessibilityRole="header">
                    Dein Ernährungstagebuch
                </AppText>
                <AppText variant="bodyLg" tone="secondary">
                    Hier findest du bald deine Mahlzeiten, Kalorien und
                    Makronährstoffe für den Tag.
                </AppText>
            </Card>
        </Screen>
    );
}
