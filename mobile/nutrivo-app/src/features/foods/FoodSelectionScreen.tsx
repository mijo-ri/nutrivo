import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useLocalSearchParams, useRouter } from 'expo-router';
import { useRef, useState } from 'react';
import {
    FlatList,
    KeyboardAvoidingView,
    Platform,
    Pressable,
    StyleSheet,
    TextInput,
    type TextInputInstance,
    View,
    useWindowDimensions,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { getMealName } from '@/features/diary/diaryMeals';
import { formatNutrition } from '@/features/diary/diaryExample';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { searchExampleFoods, type ExampleFood } from './foodExamples';

export function FoodSelectionScreen() {
    const router = useRouter();
    const { meal } = useLocalSearchParams<{ meal?: string | string[] }>();
    const isClient = useIsClient();
    const mealName = isClient ? getMealName(meal) : undefined;
    const [search, setSearch] = useState('');
    const [focused, setFocused] = useState(false);
    const input = useRef<TextInputInstance>(null);
    const foods = searchExampleFoods(search);
    const hasQuery = search.trim().length > 0;

    function goBack() {
        if (router.canGoBack()) {
            router.back();
        } else {
            router.replace('/');
        }
    }

    return (
        <SafeAreaView style={styles.screen}>
            <KeyboardAvoidingView
                style={styles.screen}
                behavior={Platform.OS === 'ios' ? 'padding' : undefined}
            >
                <View style={styles.content}>
                    <View style={styles.header}>
                        <Pressable
                            onPress={goBack}
                            accessibilityRole="button"
                            accessibilityLabel="Zurück zum Tagebuch"
                            style={({ pressed }) => [styles.iconButton, pressed && styles.pressed]}
                        >
                            <MaterialDesignIcons
                                name="arrow-left"
                                size={24}
                                color={theme.colors.primary}
                                accessible={false}
                                aria-hidden
                            />
                        </Pressable>
                        <View style={styles.titles}>
                            <AppText variant="headlineMd" accessibilityRole="header">
                                Lebensmittel auswählen
                            </AppText>
                            {mealName && (
                                <AppText tone="secondary">{mealName}</AppText>
                            )}
                        </View>
                    </View>
                    <View style={styles.searchSection}>
                        <View style={[styles.searchBox, focused && styles.focusedSearch]}>
                            <MaterialDesignIcons
                                name="magnify"
                                size={22}
                                color={theme.colors.textSecondary}
                                accessible={false}
                                aria-hidden
                            />
                            <TextInput
                                ref={input}
                                value={search}
                                onChangeText={setSearch}
                                onFocus={() => setFocused(true)}
                                onBlur={() => setFocused(false)}
                                accessibilityLabel="Lebensmittel oder Marke suchen"
                                placeholder="Lebensmittel oder Marke suchen"
                                placeholderTextColor={theme.colors.textSecondary}
                                selectionColor={theme.colors.primary}
                                autoCorrect={false}
                                autoCapitalize="none"
                                returnKeyType="search"
                                style={styles.input}
                            />
                            {search.length > 0 && (
                                <Pressable
                                    onPress={() => {
                                        setSearch('');
                                        input.current?.focus();
                                    }}
                                    accessibilityRole="button"
                                    accessibilityLabel="Suche löschen"
                                    style={({ pressed }) => [styles.iconButton, pressed && styles.pressed]}
                                >
                                    <MaterialDesignIcons
                                        name="close"
                                        size={22}
                                        color={theme.colors.textSecondary}
                                        accessible={false}
                                        aria-hidden
                                    />
                                </Pressable>
                            )}
                        </View>
                        <AppText tone="secondary" accessibilityLiveRegion="polite">
                            {hasQuery ? `${foods.length} Lebensmittel gefunden` : 'Suche nach Lebensmitteln oder Marken'}
                        </AppText>
                    </View>
                    <FlatList
                        style={styles.list}
                        contentContainerStyle={styles.listContent}
                        data={foods}
                        keyExtractor={food => food.id}
                        keyboardShouldPersistTaps="handled"
                        keyboardDismissMode="on-drag"
                        renderItem={({ item }) => <FoodRow food={item} />}
                        ListEmptyComponent={
                            <View style={styles.empty}>
                                <View style={styles.emptyIcon}>
                                    <MaterialDesignIcons
                                        name={hasQuery ? 'magnify-close' : 'magnify'}
                                        size={28}
                                        color={theme.colors.primary}
                                        accessible={false}
                                        aria-hidden
                                    />
                                </View>
                                <AppText variant="headlineMd" style={styles.centered}>
                                    {hasQuery ? 'Keine Lebensmittel gefunden' : 'Finde dein Lebensmittel'}
                                </AppText>
                                <AppText tone="secondary" style={styles.centered}>
                                    {hasQuery
                                        ? 'Versuche einen anderen Namen oder eine andere Marke.'
                                        : 'Gib einen Namen oder eine Marke in das Suchfeld ein.'}
                                </AppText>
                            </View>
                        }
                    />
                </View>
            </KeyboardAvoidingView>
        </SafeAreaView>
    );
}

function FoodRow({ food }: { food: ExampleFood }) {
    const { width, fontScale } = useWindowDimensions();
    const stacked = width < 360 || fontScale > 1.2;
    const calories = formatNutrition(food.kilocaloriesPer100);

    return (
        <View
            accessible
            accessibilityLabel={`${food.name}${food.brand ? `, ${food.brand}` : ''}, ${calories} Kilokalorien pro 100 ${food.referenceUnit === 'g' ? 'Gramm' : 'Milliliter'}`}
            style={[styles.foodRow, stacked && styles.stackedRow]}
        >
            <View style={styles.foodName}>
                <AppText variant="bodyLg">{food.name}</AppText>
                {food.brand && <AppText tone="secondary">{food.brand}</AppText>}
            </View>
            <View style={[styles.nutrition, stacked && styles.stackedNutrition]}>
                <AppText variant="numericMd" tone="accent">{calories} kcal</AppText>
                <AppText variant="bodySm" tone="secondary">pro 100 {food.referenceUnit}</AppText>
            </View>
        </View>
    );
}

const styles = StyleSheet.create({
    screen: {
        flex: 1,
        backgroundColor: theme.colors.background,
    },
    content: {
        flex: 1,
        width: '100%',
        maxWidth: 640,
        alignSelf: 'center',
    },
    header: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        paddingHorizontal: theme.spacing.screen,
        paddingVertical: theme.spacing.lg,
    },
    iconButton: {
        minWidth: 44,
        minHeight: 44,
        alignItems: 'center',
        justifyContent: 'center',
        borderRadius: theme.radii.full,
    },
    pressed: {
        backgroundColor: theme.colors.surfaceHighest,
    },
    titles: {
        flex: 1,
        gap: theme.spacing.xs,
    },
    searchSection: {
        paddingHorizontal: theme.spacing.screen,
        paddingBottom: theme.spacing.lg,
        gap: theme.spacing.sm,
    },
    searchBox: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.sm,
        minHeight: 52,
        paddingLeft: theme.spacing.md,
        paddingRight: theme.spacing.xs,
        borderWidth: 1,
        borderColor: theme.colors.outlineVariant,
        borderRadius: theme.radii.lg,
        backgroundColor: theme.colors.surface,
    },
    focusedSearch: {
        borderColor: theme.colors.primary,
    },
    input: {
        flex: 1,
        minWidth: 0,
        minHeight: 50,
        paddingVertical: theme.spacing.md,
        color: theme.colors.text,
        ...theme.typography.bodyLg,
        ...Platform.select({ web: { outlineStyle: 'solid', outlineWidth: 0 } }),
    },
    list: {
        flex: 1,
    },
    listContent: {
        flexGrow: 1,
        paddingHorizontal: theme.spacing.screen,
        paddingBottom: theme.spacing.screen,
        gap: theme.spacing.sm,
    },
    empty: {
        alignItems: 'center',
        paddingHorizontal: theme.spacing.lg,
        paddingVertical: theme.spacing.xl,
        gap: theme.spacing.md,
    },
    emptyIcon: {
        width: 56,
        height: 56,
        borderRadius: theme.radii.full,
        backgroundColor: theme.colors.surfaceHigh,
        alignItems: 'center',
        justifyContent: 'center',
    },
    centered: {
        textAlign: 'center',
    },
    foodRow: {
        flexDirection: 'row',
        alignItems: 'center',
        gap: theme.spacing.lg,
        padding: theme.spacing.lg,
        borderRadius: theme.radii.lg,
        backgroundColor: theme.colors.surface,
    },
    stackedRow: {
        flexDirection: 'column',
        alignItems: 'stretch',
        gap: theme.spacing.sm,
    },
    foodName: {
        flex: 1,
        minWidth: 0,
        gap: theme.spacing.xs,
    },
    nutrition: {
        flexShrink: 1,
        alignItems: 'flex-end',
        gap: theme.spacing.xs,
    },
    stackedNutrition: {
        alignItems: 'flex-start',
    },
});
