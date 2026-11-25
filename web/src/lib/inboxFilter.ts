/** The Inbox filter: at most one Category and any number of Tags (all must match). */
export type InboxFilter = { category: string | null; tags: string[] };

export const noFilter: InboxFilter = { category: null, tags: [] };

/** The filter in a page URL's query (`?category=Work&tag=a&tag=b`). Blank values are ignored. */
export function parseFilter(params: URLSearchParams): InboxFilter {
	const category = params.get('category')?.trim() || null;
	const tags = [...new Set(params.getAll('tag').filter((t) => t.trim()))];
	return { category, tags };
}

/** The query string for a filter: empty, or starting with `?`. Names are percent-encoded. */
export function filterQuery(filter: InboxFilter): string {
	const params = new URLSearchParams();
	if (filter.category) params.set('category', filter.category);
	for (const tag of filter.tags) params.append('tag', tag);
	const text = params.toString();
	return text ? `?${text}` : '';
}

export function isFiltered(filter: InboxFilter): boolean {
	return filter.category !== null || filter.tags.length > 0;
}
