import { describe, expect, it } from 'vitest';
import { errorText } from './errorText';

const res = (status: number, error?: unknown) => ({ response: { status }, error });

describe('errorText', () => {
	it('says unreachable for a thrown network error', () => {
		expect(errorText({ thrown: new TypeError('Failed to fetch') })).toBe("Can't reach the Core");
	});
	it.each([502, 503, 504])('says unreachable for %i', (s) => {
		expect(errorText(res(s))).toBe("Can't reach the Core");
	});
	it('gives Wrong secret for 401 only when asked', () => {
		expect(errorText(res(401), { wrongSecret: true })).toBe('Wrong secret');
		expect(errorText(res(401))).toBe('Something went wrong (status 401)');
	});
	it.each([400, 409])('gives the detail for %i, falling back to title', (s) => {
		expect(errorText(res(s, { title: 'Bad', detail: 'Name is taken.' }))).toBe('Name is taken.');
		expect(errorText(res(s, { title: 'Conflict' }))).toBe('Conflict');
	});
	it('does not fail on a 400 without problem details', () => {
		expect(errorText(res(400, 'oops'))).toBe('Something went wrong (status 400)');
	});
	it('gives the status for 500 and for a body that is not problem details', () => {
		expect(errorText(res(500, { title: 'Server' }))).toBe('Something went wrong (status 500)');
		expect(errorText(res(404, '<html>'))).toBe('Something went wrong (status 404)');
	});
	it('gives plain text when there is no status', () => {
		expect(errorText(res(0))).toBe('Something went wrong');
	});
});
