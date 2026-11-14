import { AppText } from '@/shared/ui/AppText';
import { Card } from '@/shared/ui/Card';
import { Screen } from '@/shared/ui/Screen';

export function TargetsScreen() {
  return (
    <Screen>
      <Card>
        <AppText variant="headlineMd" accessibilityRole="header">
          Deine Ernährungsziele
        </AppText>
        <AppText variant="bodyLg" tone="secondary">
          Hier kannst du bald deine täglichen Kalorien- und Makronährstoffziele festlegen.
        </AppText>
      </Card>
    </Screen>
  );
}
