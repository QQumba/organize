import { api } from '#lib/api/client.ts';
import { errorText, type Failure } from '#lib/errorText.ts';

export async function load() {
	try {
		const result = await api.GET('/api/archive');
		return {
			items: result.data ?? [],
			error: result.error !== undefined ? errorText(result as Failure) : ''
		};
	} catch (thrown) {
		return { items: [], error: errorText({ thrown }) };
	}
}
