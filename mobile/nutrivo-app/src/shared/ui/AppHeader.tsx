import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

type AppHeaderProps = {
  title: string;
};

export function AppHeader({ title }: AppHeaderProps) {
  return (
    <SafeAreaView edges={['top', 'left', 'right']} style={styles.header}>
      <View style={styles.content}>
        <View style={styles.titles}>
          <AppText variant="bodyLg" tone="accent" style={styles.brand}>
            Nutrivo
          </AppText>
          <AppText variant="bodyLg" accessibilityRole="header" style={styles.title}>
            {title}
          </AppText>
        </View>
        <View accessible accessibilityLabel="Benutzerprofil" style={styles.profile}>
          <MaterialDesignIcons
            name="account-outline"
            size={24}
            color={theme.colors.onPrimary}
            accessible={false}
            aria-hidden
          />
        </View>
      </View>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  header: {
    backgroundColor: theme.colors.background,
  },
  content: {
    minHeight: 56,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    gap: theme.spacing.lg,
    paddingHorizontal: theme.spacing.screen,
    paddingVertical: theme.spacing.sm,
  },
  titles: {
    flex: 1,
  },
  brand: {
    lineHeight: 20,
  },
  title: {
    lineHeight: 20,
    textTransform: 'uppercase',
  },
  profile: {
    width: 36,
    height: 36,
    borderRadius: theme.radii.full,
    alignItems: 'center',
    justifyContent: 'center',
    backgroundColor: theme.colors.primary,
  },
});
