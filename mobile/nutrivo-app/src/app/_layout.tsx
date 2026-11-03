import { useFonts } from 'expo-font';
import { DarkTheme, Stack, ThemeProvider } from 'expo-router';
import * as SplashScreen from 'expo-splash-screen';
import { StatusBar } from 'expo-status-bar';
import { useEffect } from 'react';
import { StyleSheet, Text } from 'react-native';
import { SafeAreaProvider, SafeAreaView } from 'react-native-safe-area-context';

import { fontFamilies, theme } from '@/shared/theme/theme';

void SplashScreen.preventAutoHideAsync();

const fonts = {
    [fontFamilies.regular]: require('../../assets/fonts/geist/Geist-Regular.ttf'),
    [fontFamilies.medium]: require('../../assets/fonts/geist/Geist-Medium.ttf'),
    [fontFamilies.semiBold]: require('../../assets/fonts/geist/Geist-SemiBold.ttf'),
    [fontFamilies.bold]: require('../../assets/fonts/geist/Geist-Bold.ttf'),
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
                    <Stack
                        screenOptions={{
                            headerShown: false,
                            contentStyle: { backgroundColor: theme.colors.background },
                        }}
                    >
                        <Stack.Screen name="index" options={{ title: 'Tagebuch' }} />
                    </Stack>
                </ThemeProvider>
            )}
        </SafeAreaProvider>
    );
}

const styles = StyleSheet.create({
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
