import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useRouter } from 'expo-router';
import type { PropsWithChildren } from 'react';
import {
  KeyboardAvoidingView,
  Platform,
  Pressable,
  ScrollView,
  StyleSheet,
  View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

export function FoodPage({
  title,
  children,
  onBack,
  backLabel = 'Zurück zu den Lebensmitteln',
  backDisabled = false,
}: PropsWithChildren<{
  title: string;
  onBack?: () => void;
  backLabel?: string;
  backDisabled?: boolean;
}>) {
  const router = useRouter();

  return (
    <SafeAreaView style={styles.screen}>
      <KeyboardAvoidingView
        style={styles.screen}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <View style={styles.header}>
          <Pressable
            onPress={onBack ?? (() => router.dismissTo('/foods'))}
            disabled={backDisabled}
            accessibilityState={{ disabled: backDisabled }}
            accessibilityRole="button"
            accessibilityLabel={backLabel}
            style={({ pressed }) => [styles.back, pressed && styles.pressed]}
          >
            <MaterialDesignIcons
              name="arrow-left"
              size={24}
              color={theme.colors.primary}
              accessible={false}
              aria-hidden
            />
          </Pressable>
          <AppText variant="headlineMd" accessibilityRole="header" style={styles.title}>
            {title}
          </AppText>
        </View>
        <ScrollView
          contentContainerStyle={styles.content}
          keyboardShouldPersistTaps="handled"
          keyboardDismissMode="on-drag"
        >
          {children}
        </ScrollView>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: theme.colors.background },
  header: {
    width: '100%',
    maxWidth: 640,
    alignSelf: 'center',
    flexDirection: 'row',
    alignItems: 'center',
    gap: theme.spacing.sm,
    paddingHorizontal: theme.spacing.screen,
    paddingVertical: theme.spacing.sm,
  },
  title: { flex: 1 },
  back: {
    minWidth: 44,
    minHeight: 44,
    alignItems: 'center',
    justifyContent: 'center',
    borderRadius: theme.radii.full,
  },
  pressed: { backgroundColor: theme.colors.surfaceHighest },
  content: {
    width: '100%',
    maxWidth: 640,
    alignSelf: 'center',
    padding: theme.spacing.screen,
    gap: theme.spacing.lg,
    paddingBottom: theme.spacing.xl,
  },
});
