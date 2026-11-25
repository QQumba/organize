import { goto } from '$app/navigation';
import createClient from 'openapi-fetch';
import { markReachable, markUnreachable } from '../coreStatus.svelte';
import { loginUrl } from '../returnPath';
import { classify, once } from './guard';
import type { paths } from './schema';

// The paths in docs/openapi.json already start with /api, so the base is empty.
export const api = createClient<paths>({ baseUrl: '', credentials: 'same-origin' });

const toLogin = once(async () => {
	if (location.pathname === '/login') return;
	// eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the target is a checked same-origin path
	await goto(loginUrl(location.pathname + location.search), { replaceState: true });
});

// The one place that handles 401 (go to login) and an unreachable Core (banner).
api.use({
	onResponse({ request, response }) {
		const verdict = classify(new URL(request.url).pathname, response.status);
		if (verdict === 'unreachable') markUnreachable();
		else markReachable();
		if (verdict === 'unauthorized') toLogin();
	},
	onError({ error }) {
		// A request cancelled on purpose (navigation, AbortSignal) says nothing about the Core.
		if (error instanceof Error && error.name === 'AbortError') return;
		markUnreachable();
	}
});
