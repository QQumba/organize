/** A failed API call: the response (with the parsed error body), or what fetch threw. */
export type Failure = { response: { status: number }; error?: unknown } | { thrown: unknown };

export const unreachableText = "Can't reach the Core";

export function isUnreachableStatus(status: number): boolean {
	return status === 502 || status === 503 || status === 504;
}

function problemText(body: unknown): string | null {
	if (typeof body !== 'object' || body === null) return null;
	const { detail, title } = body as { detail?: unknown; title?: unknown };
	if (typeof detail === 'string' && detail) return detail;
	if (typeof title === 'string' && title) return title;
	return null;
}

/**
 * Text for a failed API call, for a screen to show next to the field or action that caused it.
 * A 401 gives "Wrong secret" only with `wrongSecret` (the login page); elsewhere the API
 * middleware has already redirected to the login page.
 */
export function errorText(failure: Failure, options: { wrongSecret?: boolean } = {}): string {
	if ('thrown' in failure) return unreachableText;
	const { status } = failure.response;
	if (isUnreachableStatus(status)) return unreachableText;
	if (status === 401 && options.wrongSecret) return 'Wrong secret';
	if (status === 400 || status === 409) {
		const text = problemText(failure.error);
		if (text) return text;
	}
	return status ? `Something went wrong (status ${status})` : 'Something went wrong';
}
