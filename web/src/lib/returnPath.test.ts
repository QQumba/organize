import { describe, expect, it } from 'vitest';
import { landingPath, loginUrl, safeReturnPath } from './returnPath';

describe('safeReturnPath', () => {
	it('accepts same-origin paths and keeps the query', () => {
		expect(safeReturnPath('/inbox')).toBe('/inbox');
		expect(safeReturnPath('/inbox?x=1')).toBe('/inbox?x=1');
		expect(safeReturnPath('/')).toBe('/');
	});
	it.each([
		'https://evil.example/inbox',
		'//host/x',
		'/\\host',
		'/\t/host',
		'javascript:alert(1)',
		'inbox',
		'',
		'/login',
		'/login?return=/inbox',
		'/login/'
	])('rejects %j', (raw) => {
		expect(safeReturnPath(raw)).toBeNull();
	});
	it('rejects null and undefined', () => {
		expect(safeReturnPath(null)).toBeNull();
		expect(safeReturnPath(undefined)).toBeNull();
	});
});

describe('landingPath and loginUrl', () => {
	it('falls back to the Inbox', () => {
		expect(landingPath('//x')).toBe('/inbox');
		expect(landingPath('/done?q=a')).toBe('/done?q=a');
	});
	it('builds the login URL with an encoded return', () => {
		expect(loginUrl('/inbox?x=1')).toBe('/login?return=%2Finbox%3Fx%3D1');
		expect(loginUrl('/login')).toBe('/login');
		expect(loginUrl()).toBe('/login');
	});
});
