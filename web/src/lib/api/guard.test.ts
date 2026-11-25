import { describe, expect, it } from 'vitest';
import { classify, once } from './guard';

describe('classify', () => {
	it('redirects on 401 except for the session calls', () => {
		expect(classify('/api/items', 401)).toBe('unauthorized');
		expect(classify('/api/session', 401)).toBe('ok');
	});
	it('treats 502, 503 and 504 as unreachable, also for session calls', () => {
		for (const s of [502, 503, 504]) {
			expect(classify('/api/items', s)).toBe('unreachable');
			expect(classify('/api/session', s)).toBe('unreachable');
		}
	});
	it('is ok otherwise', () => {
		expect(classify('/api/items', 200)).toBe('ok');
		expect(classify('/api/items', 500)).toBe('ok');
	});
});

describe('once', () => {
	it('runs the action once for concurrent calls, and again after it settles', async () => {
		let runs = 0;
		let release = () => {};
		const run = once(
			() =>
				new Promise<void>((resolve) => {
					runs++;
					release = resolve;
				})
		);
		run();
		run();
		run();
		expect(runs).toBe(1);
		release();
		await new Promise((r) => setTimeout(r));
		run();
		expect(runs).toBe(2);
	});
});
