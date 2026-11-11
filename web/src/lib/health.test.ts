import { describe, expect, it } from 'vitest';
import { coreStatusText } from './health';

describe('coreStatusText', () => {
	it('shows ok for 200', () => {
		expect(coreStatusText({ status: 200 })).toBe('Core: ok');
	});
	it('shows login required for 401', () => {
		expect(coreStatusText({ status: 401 })).toBe('Core: up, login required');
	});
	it('shows unreachable for other statuses and network errors', () => {
		expect(coreStatusText({ status: 500 })).toBe('Core: unreachable');
		expect(coreStatusText({ error: new TypeError('fetch failed') })).toBe('Core: unreachable');
	});
});
