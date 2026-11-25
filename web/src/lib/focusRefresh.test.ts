import { describe, expect, it } from 'vitest';
import { focusRefreshInterval, shouldRefresh } from './focusRefresh';

const base = { enabled: true, unreachable: false, unsaved: false, now: 100_000, last: 0 };

describe('shouldRefresh', () => {
	it('refreshes when enabled and the interval has passed', () => {
		expect(shouldRefresh(base)).toBe(true);
	});
	it('throttles to one refresh per interval', () => {
		expect(shouldRefresh({ ...base, last: base.now - focusRefreshInterval + 1 })).toBe(false);
		expect(shouldRefresh({ ...base, last: base.now - focusRefreshInterval })).toBe(true);
	});
	it('does nothing when switched off', () => {
		expect(shouldRefresh({ ...base, enabled: false })).toBe(false);
	});
	it('does nothing while the Core banner is shown', () => {
		expect(shouldRefresh({ ...base, unreachable: true })).toBe(false);
	});
	it('does nothing while a form has unsaved changes', () => {
		expect(shouldRefresh({ ...base, unsaved: true })).toBe(false);
	});
});
