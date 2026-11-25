import { beforeEach, describe, expect, it, vi } from 'vitest';

const get = vi.hoisted(() => vi.fn());
vi.mock('$app/navigation', () => ({ goto: vi.fn(), invalidateAll: vi.fn() }));
vi.mock('./api/client', () => ({ api: { GET: get, DELETE: vi.fn() } }));

const { checkSession, retry } = await import('./session');

const answer = (status: number) => get.mockResolvedValueOnce({ response: { status } });

describe('checkSession', () => {
	beforeEach(async () => {
		get.mockReset();
		await retry(); // resets the once-per-page-load flag
	});

	it('asks the Core once, then skips', async () => {
		answer(204);
		expect(await checkSession(fetch)).toBe(true);
		expect(await checkSession(fetch)).toBeNull();
		expect(get).toHaveBeenCalledTimes(1);
	});

	it('reports 401 as not logged in', async () => {
		answer(401);
		expect(await checkSession(fetch)).toBe(false);
	});

	it('does not count an unreachable Core or a thrown error as an answer, so it asks again', async () => {
		answer(502);
		expect(await checkSession(fetch)).toBeNull();
		get.mockRejectedValueOnce(new TypeError('Failed to fetch'));
		expect(await checkSession(fetch)).toBeNull();
		answer(204);
		expect(await checkSession(fetch)).toBe(true);
	});

	it('asks again after Retry', async () => {
		answer(204);
		await checkSession(fetch);
		await retry();
		answer(401);
		expect(await checkSession(fetch)).toBe(false);
		expect(get).toHaveBeenCalledTimes(2);
	});
});
