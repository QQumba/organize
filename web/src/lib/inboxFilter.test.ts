import { describe, expect, it } from 'vitest';
import { filterQuery, isFiltered, noFilter, parseFilter } from './inboxFilter';

const parse = (query: string) => parseFilter(new URLSearchParams(query));

describe('parseFilter', () => {
	it('reads one category and repeated tags', () => {
		expect(parse('?category=Work&tag=a&tag=b')).toEqual({ category: 'Work', tags: ['a', 'b'] });
	});

	it('gives no filter for an empty query', () => {
		expect(parse('')).toEqual(noFilter);
	});

	it('ignores blank values and repeated tags', () => {
		expect(parse('?category=%20&tag=&tag=a&tag=a')).toEqual({ category: null, tags: ['a'] });
	});

	it('reads Cyrillic names', () => {
		expect(
			parse('?category=%D0%A0%D0%BE%D0%B1%D0%BE%D1%82%D0%B0&tag=%D0%B7%D0%B2%D1%96%D1%82')
		).toEqual({
			category: 'Робота',
			tags: ['звіт']
		});
	});
});

describe('filterQuery', () => {
	it('is empty without a filter', () => {
		expect(filterQuery(noFilter)).toBe('');
	});

	it('repeats tag', () => {
		expect(filterQuery({ category: 'Work', tags: ['a', 'b'] })).toBe('?category=Work&tag=a&tag=b');
	});

	it('encodes names that need it', () => {
		expect(filterQuery({ category: null, tags: ['a&b', 'x y'] })).toBe('?tag=a%26b&tag=x+y');
	});

	it('round-trips Cyrillic, spaces and symbols', () => {
		const filter = { category: 'Робота й дім', tags: ['звіт', 'їжак', 'a&b=c'] };
		expect(parse(filterQuery(filter))).toEqual(filter);
	});
});

describe('isFiltered', () => {
	it('is true for a category or a tag', () => {
		expect(isFiltered(noFilter)).toBe(false);
		expect(isFiltered({ category: 'Work', tags: [] })).toBe(true);
		expect(isFiltered({ category: null, tags: ['a'] })).toBe(true);
	});
});
