import { MaterialDesignIcons } from '@react-native-vector-icons/material-design-icons';
import { useFonts } from 'expo-font';
import { DarkTheme, Tabs, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { StyleSheet, Text, useWindowDimensions } from 'react-native';
import {
    SafeAreaProvider,
    SafeAreaView,
    useSafeAreaInsets,
} from 'react-native-safe-area-context';

import { fontFamilies, theme } from '@/shared/theme/theme';
import { AppHeader } from '@/shared/ui/AppHeader';
import { AppText } from '@/shared/ui/AppText';

void SplashScreen.preventAutoHideAsync();

export const unstable_settings = {
    anchor: 'index',
};

const fonts = {
    [fontFamilies.regular]: require('../../assets/fonts/geist/Geist-Regular.ttf'),
    [fontFamilies.medium]: require('../../assets/fonts/geist/Geist-Medium.ttf'),
    [fontFamilies.semiBold]: require('../../assets/fonts/geist/Geist-SemiBold.ttf'),
    [fontFamilies.bold]: require('../../assets/fonts/geist/Geist-Bold.ttf'),
    MaterialDesignIcons: require('@react-native-vector-icons/material-design-icons/fonts/MaterialDesignIcons.ttf'),
};

const navigationTheme = {
    ...DarkTheme,
    colors: {
        ...DarkTheme.colors,
        primary: theme.colors.primary,
        background: theme.colors.background,
        card: theme.colors.surface,
        text: theme.colors.text,
        border: theme.colors.outlineVariant,
        notification: theme.colors.primary,
    },
};

export default function RootLayout() {
    const [fontsLoaded, fontError] = useFonts(fonts);

    useEffect(() => {
        if (fontsLoaded || fontError) {
            void SplashScreen.hideAsync();
        }
    }, [fontsLoaded, fontError]);

    if (!fontsLoaded && !fontError) {
        return null;
    }

    return (
        <SafeAreaProvider>
            <StatusBar style="light" />
            {fontError ? (
                <SafeAreaView style={styles.errorScreen}>
                    <Text accessibilityRole="alert" style={styles.errorText}>
                        Nutrivo konnte nicht geladen werden. Bitte öffne die App erneut.
                    </Text>
                </SafeAreaView>
            ) : (
                <ThemeProvider value={navigationTheme}>
                    <AppTabs />
                </ThemeProvider>
            )}
        </SafeAreaProvider>
    );
}

function AppTabs() {
    const insets = useSafeAreaInsets();
    const { fontScale } = useWindowDimensions();

    return (
        <Tabs
            backBehavior="initialRoute"
            screenOptions={({ route }) => ({
                header: ({ options }) => (
                    <AppHeader title={options.title ?? route.name} />
                ),
                sceneStyle: { backgroundColor: theme.colors.background },
                tabBarPosition: 'bottom',
                tabBarLabelPosition: 'below-icon',
                tabBarActiveTintColor: theme.colors.primary,
                tabBarInactiveTintColor: theme.colors.textSecondary,
                tabBarLabel: ({ color, children }) => (
                    <AppText
                        accessible={false}
                        variant="bodySm"
                        style={[styles.tabLabel, { color }]}
                    >
                        {children}
                    </AppText>
                ),
                tabBarStyle: [
                    styles.tabBar,
                    { height: Math.ceil(56 * Math.max(1, fontScale)) + insets.bottom },
                ],
                tabBarItemStyle: styles.tabItem,
            })}
        >
            <Tabs.Screen
                name="index"
                options={{
                    title: 'Tagebuch',
                    tabBarAccessibilityLabel: 'Tagebuch',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="calendar-blank-outline"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
            <Tabs.Screen
                name="foods"
                options={{
                    title: 'Lebensmittel',
                    tabBarAccessibilityLabel: 'Lebensmittel',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="food-apple-outline"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
            <Tabs.Screen
                name="targets"
                options={{
                    title: 'Ziele',
                    tabBarAccessibilityLabel: 'Ziele',
                    tabBarIcon: ({ color }) => (
                        <MaterialDesignIcons
                            name="tune"
                            size={22}
                            color={color}
                            accessible={false}
                            aria-hidden
                        />
                    ),
                }}
            />
        </Tabs>
    );
}

const styles = StyleSheet.create({
    tabBar: {
        backgroundColor: theme.colors.background,
        borderTopColor: theme.colors.outlineVariant,
        elevation: 0,
        shadowOpacity: 0,
    },
    tabItem: {
        flex: 1,
        minHeight: 44,
    },
    tabLabel: {
        textAlign: 'center',
        width: '100%',
    },
    errorScreen: {
        flex: 1,
        justifyContent: 'center',
        backgroundColor: theme.colors.background,
        padding: theme.spacing.screen,
    },
    errorText: {
        color: theme.colors.text,
        fontSize: 16,
        lineHeight: 24,
    },
});
