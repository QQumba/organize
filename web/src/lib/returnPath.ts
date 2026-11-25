const origin = 'http://same-origin.invalid';

/**
 * Checks the `return` query value of the login page. Returns the path (with query) when it is
 * a same-origin path that is not the login page itself, otherwise null.
 */
export function safeReturnPath(raw: string | null | undefined): string | null {
	if (!raw || !raw.startsWith('/') || raw.startsWith('//') || raw.startsWith('/\\')) return null;
	// Browsers drop tabs and newlines inside URLs, so `/\t/host` would become `//host`.
	// eslint-disable-next-line no-control-regex
	if (/[\u0000-\u001f\u007f\\]/.test(raw)) return null;
	let url: URL;
	try {
		url = new URL(raw, origin);
	} catch {
		return null;
	}
	if (url.origin !== origin) return null;
	if (url.pathname === '/login' || url.pathname === '/login/') return null;
	return raw;
}

export const defaultLanding = '/inbox';

/** Where to go after login: the checked `return` path, or the Inbox. */
export function landingPath(raw: string | null | undefined): string {
	return safeReturnPath(raw) ?? defaultLanding;
}

/** The login URL that brings the user back to `current` (a path with query) afterwards. */
export function loginUrl(current?: string): string {
	const back = safeReturnPath(current);
	return back ? `/login?return=${encodeURIComponent(back)}` : '/login';
}
