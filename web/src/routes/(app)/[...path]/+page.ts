import { error } from '@sveltejs/kit';

// Unmatched URLs land here so the 404 is rendered by (app)/+error.svelte inside the shell.
export function load() {
	error(404, 'Not found');
}
