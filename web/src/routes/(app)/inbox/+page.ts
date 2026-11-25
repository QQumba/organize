import { api } from '#lib/api/client.ts';
import { errorText, type Failure } from '#lib/errorText.ts';
import { parseFilter } from '#lib/inboxFilter.ts';

export async function load({ url }) {
	const filter = parseFilter(url.searchParams);
	const query = { category: filter.category ?? undefined, tag: filter.tags };
	try {
		const [inbox, categories, tags] = await Promise.all([
			api.GET('/api/inbox', { params: { query } }),
			api.GET('/api/categories'),
			api.GET('/api/tags')
		]);
		const failed: Failure | undefined = [inbox, categories, tags].find(
			(r) => r.error !== undefined
		);
		return {
			filter,
			items: inbox.data ?? [],
			categories: (categories.data ?? []).map((c) => c.name),
			tags: tags.data ?? [],
			error: failed ? errorText(failed as Failure) : ''
		};
	} catch (thrown) {
		return { filter, items: [], categories: [], tags: [], error: errorText({ thrown }) };
	}
}
