import { useRouter } from 'expo-router';
import { useState } from 'react';
import { FlatList, KeyboardAvoidingView, Platform, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { FoodButton } from './FoodButton';
import { FoodRow } from './FoodRow';
import { FoodSearchInput } from './FoodSearchInput';
import { FoodQueryFeedback } from './FoodQueryFeedback';
import { useFoodSearch } from './foodsQueries';

export function FoodsScreen() {
  const router = useRouter();
  const [search, setSearch] = useState('');
  const query = useFoodSearch(search);

  return (
    <SafeAreaView edges={['left', 'right']} style={styles.screen}>
      <KeyboardAvoidingView
        style={styles.screen}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <View style={styles.content}>
          <View style={styles.tools}>
            <FoodButton
              label="Lebensmittel hinzufügen"
              onPress={() => router.push('/food-create')}
            />
            <FoodSearchInput value={search} onChangeText={setSearch} />
            <AppText tone="secondary" accessibilityLiveRegion="polite">
              {query.totalCount === undefined
                ? 'Lebensmittel suchen'
                : `${query.totalCount} Lebensmittel${search.trim() ? ' gefunden' : ''}`}
            </AppText>
          </View>
          <FlatList
            data={query.foods}
            onEndReached={query.loadMore}
            onEndReachedThreshold={0.4}
            keyExtractor={(food) => food.id}
            style={styles.screen}
            contentContainerStyle={styles.list}
            keyboardShouldPersistTaps="handled"
            keyboardDismissMode="on-drag"
            renderItem={({ item }) => (
              <FoodRow
                food={item}
                onPress={() =>
                  router.push({ pathname: '/food-details', params: { id: item.id } })
                }
              />
            )}
            ListHeaderComponent={
              <FoodQueryFeedback
                loading={query.loading}
                error={query.error}
                onRetry={query.retry}
              />
            }
            ListFooterComponent={
              <FoodQueryFeedback
                loading={query.loadingMore}
                error={query.moreError}
                onRetry={query.retry}
              />
            }
            ListEmptyComponent={
              !query.loading && !query.error ? (
                <View style={styles.empty}>
                  <AppText variant="headlineMd">Keine Lebensmittel gefunden</AppText>
                  <AppText tone="secondary">
                    Versuche einen anderen Namen oder eine andere Marke.
                  </AppText>
                </View>
              ) : undefined
            }
          />
        </View>
      </KeyboardAvoidingView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  screen: { flex: 1, backgroundColor: theme.colors.background },
  content: { flex: 1, width: '100%', maxWidth: 640, alignSelf: 'center' },
  tools: { padding: theme.spacing.screen, gap: theme.spacing.md },
  list: {
    flexGrow: 1,
    paddingHorizontal: theme.spacing.screen,
    paddingBottom: theme.spacing.screen,
    gap: theme.spacing.sm,
  },
  empty: { gap: theme.spacing.sm, paddingVertical: theme.spacing.xl },
});
