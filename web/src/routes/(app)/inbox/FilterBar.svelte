<script lang="ts">
	import { goto } from '$app/navigation';
	import { filterQuery, isFiltered, type InboxFilter } from '#lib/inboxFilter.ts';

	let { filter, categories, tags }: { filter: InboxFilter; categories: string[]; tags: string[] } =
		$props();

	// A filter value from the URL that no picker offers (renamed Category, old bookmark) is still
	// shown as given, so it can be cleared. Matching names is the Core's job.
	const categoryOptions = $derived(
		filter.category && !categories.includes(filter.category)
			? [filter.category, ...categories]
			: categories
	);
	const addableTags = $derived(tags.filter((t) => !filter.tags.includes(t)));

	function apply(next: InboxFilter) {
		// replace: Back does not step through every pick.
		// eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path
		void goto(`/inbox${filterQuery(next)}`, { replace: true, reset: false });
	}
</script>

<div class="mb-3 flex flex-wrap items-center gap-2" role="group" aria-label="Filter">
	<select
		class="input"
		aria-label="Category"
		value={filter.category ?? ''}
		onchange={(e) => apply({ ...filter, category: e.currentTarget.value || null })}
	>
		<option value="">Any Category</option>
		{#each categoryOptions as name (name)}
			<option value={name}>{name}</option>
		{/each}
	</select>
	<select
		class="input"
		aria-label="Add Tag"
		value=""
		onchange={(e) => {
			const tag = e.currentTarget.value;
			e.currentTarget.value = '';
			if (tag) apply({ ...filter, tags: [...filter.tags, tag] });
		}}
	>
		<option value="">Add Tag…</option>
		{#each addableTags as name (name)}
			<option value={name}>{name}</option>
		{/each}
	</select>
	{#each filter.tags as tag (tag)}
		<span
			class="inline-flex items-center gap-1 rounded-full border border-border px-2 text-xs text-text-muted"
		>
			{tag}
			<button
				type="button"
				class="cursor-pointer hover:text-text"
				aria-label="Remove Tag {tag}"
				onclick={() => apply({ ...filter, tags: filter.tags.filter((t) => t !== tag) })}>×</button
			>
		</span>
	{/each}
	{#if isFiltered(filter)}
		<button
			type="button"
			class="cursor-pointer text-xs text-text-muted hover:text-text"
			onclick={() => apply({ category: null, tags: [] })}>Clear filter</button
		>
	{/if}
</div>
