import { StyleSheet, View } from 'react-native';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { Screen } from '@/shared/ui/Screen';

export function DiaryScreen() {
    return (
        <Screen>
            <View style={styles.header}>
                <AppText variant="headlineMd" tone="accent">
                    Nutrivo
                </AppText>
                <AppText variant="headlineXlMobile" accessibilityRole="header">
                    Tagebuch
                </AppText>
            </View>

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

const styles = StyleSheet.create({
    header: {
        gap: theme.spacing.xs,
        paddingVertical: theme.spacing.sm,
    },
});
