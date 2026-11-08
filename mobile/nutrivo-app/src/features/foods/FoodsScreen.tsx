import { useRouter } from 'expo-router';
import { useState } from 'react';
import { FlatList, KeyboardAvoidingView, Platform, StyleSheet, View } from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { searchFoods } from './food';
import { FoodButton } from './FoodButton';
import { FoodRow } from './FoodRow';
import { FoodSearchInput } from './FoodSearchInput';
import { useFoods } from './FoodsProvider';

export function FoodsScreen() {
    const router = useRouter();
    const { foods } = useFoods();
    const [search, setSearch] = useState('');
    const results = searchFoods(foods, search);

    return (
        <SafeAreaView edges={['left', 'right']} style={styles.screen}>
            <KeyboardAvoidingView style={styles.screen} behavior={Platform.OS === 'ios' ? 'padding' : undefined}>
                <View style={styles.content}>
                    <View style={styles.tools}>
                        <FoodButton label="Lebensmittel hinzufügen" onPress={() => router.push('/food-create')} />
                        <FoodSearchInput value={search} onChangeText={setSearch} />
                        <AppText tone="secondary" accessibilityLiveRegion="polite">
                            {results.length} Lebensmittel{search.trim() ? ' gefunden' : ''}
                        </AppText>
                    </View>
                    <FlatList
                        data={results}
                        keyExtractor={food => food.id}
                        style={styles.screen}
                        contentContainerStyle={styles.list}
                        keyboardShouldPersistTaps="handled"
                        keyboardDismissMode="on-drag"
                        renderItem={({ item }) => (
                            <FoodRow food={item} onPress={() => router.push({ pathname: '/food-details', params: { id: item.id } })} />
                        )}
                        ListEmptyComponent={
                            <View style={styles.empty}>
                                <AppText variant="headlineMd">Keine Lebensmittel gefunden</AppText>
                                <AppText tone="secondary">Versuche einen anderen Namen oder eine andere Marke.</AppText>
                            </View>
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
    list: { flexGrow: 1, paddingHorizontal: theme.spacing.screen, paddingBottom: theme.spacing.screen, gap: theme.spacing.sm },
    empty: { gap: theme.spacing.sm, paddingVertical: theme.spacing.xl },
});
