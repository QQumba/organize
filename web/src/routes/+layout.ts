import { redirect } from '@sveltejs/kit';
import { landingPath, loginUrl } from '#lib/returnPath.ts';
import { checkSession } from '#lib/session.ts';

export const ssr = false;

// Session check: once per page load, not per navigation (later 401s are caught by the API
// middleware). On the login page it only matters when the browser turns out to be logged in.
export async function load({ url, fetch }) {
	const loggedIn = await checkSession(fetch);
	const onLogin = url.pathname === '/login';
	if (loggedIn === false && !onLogin) redirect(307, loginUrl(url.pathname + url.search));
	if (loggedIn === true && onLogin) redirect(307, landingPath(url.searchParams.get('return')));
}
