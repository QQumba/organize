import { goto, invalidateAll } from '$app/navigation';
import { api } from './api/client';

let checked = false;

/**
 * Asks the Core once per page load whether the browser is logged in.
 * Returns true/false, or null when the check was skipped or the Core could not be reached.
 */
export async function checkSession(fetch: typeof globalThis.fetch): Promise<boolean | null> {
	if (checked) return null;
	try {
		const { response } = await api.GET('/api/session', { fetch });
		if (response.status === 204 || response.status === 401) {
			checked = true;
			return response.status === 204;
		}
	} catch {
		// The middleware has shown the banner.
	}
	return null;
}

/** Logs out and goes to the login page. */
export async function logout() {
	try {
		await api.DELETE('/api/session');
	} catch {
		// The banner is shown; still go to login.
	}
	checked = true;
	// eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the target is a checked same-origin path
	await goto('/login', { replace: true });
}

/** Retry button of the banner: reloads the current page's data (never repeats a write). */
export async function retry() {
	checked = false;
	await invalidateAll();
}
