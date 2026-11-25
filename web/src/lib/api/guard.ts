import { isUnreachableStatus } from '../errorText';

export type Verdict = 'unauthorized' | 'unreachable' | 'ok';

/** What the API middleware does with a response. The session calls never count as unauthorized. */
export function classify(path: string, status: number): Verdict {
	if (isUnreachableStatus(status)) return 'unreachable';
	if (status === 401 && path !== '/api/session') return 'unauthorized';
	return 'ok';
}

/** Wraps `action` so concurrent calls run it once, until its promise settles. */
export function once(action: () => Promise<unknown>): () => void {
	let running = false;
	return () => {
		if (running) return;
		running = true;
		action()
			.catch(() => {})
			.finally(() => (running = false));
	};
}
