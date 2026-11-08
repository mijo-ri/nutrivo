export class ApiError extends Error {
    constructor(message: string, readonly status?: number, readonly code?: string) {
        super(message);
        this.name = 'ApiError';
    }
}

export async function apiRequest<T>(path: string, options: {
    method?: 'GET' | 'POST';
    body?: unknown;
    signal?: AbortSignal;
} = {}): Promise<T> {
    const configuredUrl = process.env.EXPO_PUBLIC_API_URL?.trim();
    if (!configuredUrl || !/^https?:\/\//i.test(configuredUrl)) {
        throw new ApiError('Die API-Adresse fehlt oder ist ungültig. Bitte EXPO_PUBLIC_API_URL konfigurieren.');
    }

    const controller = new AbortController();
    const abort = () => controller.abort();
    options.signal?.addEventListener('abort', abort, { once: true });
    if (options.signal?.aborted) controller.abort();
    const timer = setTimeout(abort, 15_000);
    try {
        const response = await fetch(`${configuredUrl.replace(/\/+$/, '')}${path}`, {
            method: options.method ?? 'GET',
            headers: options.body === undefined ? undefined : { 'Content-Type': 'application/json' },
            body: options.body === undefined ? undefined : JSON.stringify(options.body),
            signal: controller.signal,
        });
        if (!response.ok) {
            const problem: unknown = await response.json().catch(() => undefined);
            const code = problem && typeof problem === 'object' && 'code' in problem
                && typeof problem.code === 'string' ? problem.code : undefined;
            throw new ApiError(
                response.status === 404 ? 'Dieses Lebensmittel wurde nicht gefunden.'
                    : response.status === 400 ? 'Bitte prüfe deine Eingaben.'
                        : 'Die Anfrage konnte nicht abgeschlossen werden. Bitte versuche es erneut.',
                response.status, code,
            );
        }
        return await response.json() as T;
    } catch (error) {
        if (error instanceof ApiError || options.signal?.aborted) throw error;
        throw new ApiError(options.method === 'POST'
            ? 'Die Verbindung wurde unterbrochen. Bitte prüfe in der Übersicht, ob das Lebensmittel gespeichert wurde, bevor du erneut speicherst.'
            : 'Die API ist nicht erreichbar. Bitte prüfe die Verbindung und versuche es erneut.');
    } finally {
        clearTimeout(timer);
        options.signal?.removeEventListener('abort', abort);
    }
}
