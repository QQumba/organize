import { describe, expect, it } from 'vitest';
import { searchUrl } from './searchUrl';

describe('searchUrl', () => {
	it('is null for blank text', () => {
		expect(searchUrl('')).toBeNull();
		expect(searchUrl('  \t ')).toBeNull();
	});
	it('trims and encodes', () => {
		expect(searchUrl('  buy milk ')).toBe('/search?q=buy%20milk');
		expect(searchUrl('a&b=c')).toBe('/search?q=a%26b%3Dc');
	});
	it('round-trips Cyrillic', () => {
		const url = searchUrl('купити молоко')!;
		expect(new URL(url, 'http://x').searchParams.get('q')).toBe('купити молоко');
	});
});
