import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useFocusEffect, useLocalSearchParams, useRouter } from 'expo-router';
import { useCallback, useState } from 'react';
import {
    AccessibilityInfo,
    FlatList,
    KeyboardAvoidingView,
    Platform,
    Pressable,
    StyleSheet,
    View,
} from 'react-native';
import { SafeAreaView } from 'react-native-safe-area-context';

import { isMealId, parseDiaryDate } from '@/features/diary/diary';
import { getMealName } from '@/features/diary/diaryMeals';
import { useDiary } from '@/features/diary/DiaryProvider';
import { useIsClient } from '@/shared/hooks/useIsClient';
import { theme } from '@/shared/theme/theme';
import { AppText } from '@/shared/ui/AppText';

import { searchFoods } from './food';
import { FoodButton } from './FoodButton';
import { FoodRow } from './FoodRow';
import { FoodSearchInput } from './FoodSearchInput';
import { useFoods } from './FoodsProvider';

const dateFormat = new Intl.DateTimeFormat('de-CH', { dateStyle: 'long' });

export function FoodSelectionScreen() {
    const router = useRouter();
    const { meal, date } = useLocalSearchParams<{ meal?: string | string[]; date?: string | string[] }>();
    const isClient = useIsClient();
    const mealName = isClient ? getMealName(meal) : undefined;
    const parsedDate = parseDiaryDate(date);
    const validContext = isMealId(meal) && typeof date === 'string' && !!parsedDate;
    const [search, setSearch] = useState('');
    const [confirmation, setConfirmation] = useState('');
    const { lastAddedEntry, claimConfirmation } = useDiary();
    const { foods: catalogue } = useFoods();
    const hasQuery = search.trim().length > 0;
    const foods = hasQuery ? searchFoods(catalogue, search) : [];

    useFocusEffect(useCallback(() => {
        if (!lastAddedEntry || lastAddedEntry.date !== date || lastAddedEntry.mealId !== meal
            || !claimConfirmation(lastAddedEntry.id)) {
            return;
        }
        const message = `${lastAddedEntry.food.name} wurde hinzugefügt.`;
        setConfirmation(message);
        if (Platform.OS !== 'web') {
            AccessibilityInfo.announceForAccessibility(message);
        }
        const timer = setTimeout(() => setConfirmation(''), 4000);
        return () => {
            clearTimeout(timer);
            setConfirmation('');
        };
    }, [date, meal, lastAddedEntry, claimConfirmation]));

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
                                <AppText tone="secondary">
                                    {mealName}{parsedDate ? ` · ${dateFormat.format(parsedDate)}` : ''}
                                </AppText>
                            )}
                        </View>
                    </View>
                    {isClient && !validContext ? (
                        <View style={styles.searchSection}>
                            <AppText accessibilityRole="alert">Datum oder Mahlzeit ist ungültig. Bitte beginne im Tagebuch erneut.</AppText>
                            <FoodButton label="Zum Tagebuch" onPress={() => router.dismissTo('/')} />
                        </View>
                    ) : (
                        <>
                            <View style={styles.searchSection}>
                                <FoodSearchInput value={search} onChangeText={setSearch} />
                                <AppText tone="accent" accessibilityLiveRegion="polite" role="status">{confirmation}</AppText>
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
                                renderItem={({ item }) => (
                                    <FoodRow
                                        food={item}
                                        accessibilityHint="Portion oder Menge erfassen"
                                        onPress={validContext ? () => router.push({
                                            pathname: '/diary-entry-create',
                                            params: { foodId: item.id, meal, date },
                                        }) : undefined}
                                    />
                                )}
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
                        </>
                    )}
                </View>
            </KeyboardAvoidingView>
        </SafeAreaView>
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
});
