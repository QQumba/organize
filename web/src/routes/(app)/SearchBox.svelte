<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { searchUrl } from '#lib/searchUrl.ts';

	let text = $state('');

	// On the search page the box shows the current query.
	$effect(() => {
		if (page.url.pathname === '/search') text = page.url.searchParams.get('q') ?? '';
	});

	async function submit(event: SubmitEvent) {
		event.preventDefault();
		const url = searchUrl(text);
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the URL is built by searchUrl
		if (url) await goto(url);
	}
</script>

<form class="w-56 shrink-0" role="search" onsubmit={submit}>
	<input
		class="input w-full"
		type="text"
		name="q"
		placeholder="Search…"
		aria-label="Search"
		autocomplete="off"
		bind:value={text}
	/>
</form>
