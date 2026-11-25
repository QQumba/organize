<script lang="ts">
	import type { Snippet } from 'svelte';
	import type { components } from '../api/schema';
	import { dueLabel, dueTooltip, isOverdue } from '#lib/dueDate.ts';

	type Item = components['schemas']['ItemDto'];

	// The shared Item row (Inbox, Done, Archive, Search). The per-view action (the done checkbox,
	// Restore) comes in as the `action` snippet and sits beside the link, never inside it.
	let {
		item,
		overdueHighlight = false,
		error = '',
		today = new Date(),
		action
	}: {
		item: Item;
		/** Highlight a due date before today. Only open Items (the Inbox) turn this on. */
		overdueHighlight?: boolean;
		/** A failed action's message, shown on the row. */
		error?: string;
		today?: Date;
		action?: Snippet;
	} = $props();

	const overdue = $derived(overdueHighlight && isOverdue(item.dueDate, today));
	const hasMeta = $derived(
		item.important ||
			item.urgent ||
			item.dueDate !== null ||
			item.tags.length > 0 ||
			!!item.notes ||
			!!error
	);
</script>

<li
	class="flex h-row items-center gap-2 border-b border-border px-3 last:border-b-0 focus-within:bg-surface hover:bg-surface"
>
	{#if action}{@render action()}{/if}
	<div class="flex min-w-0 flex-1 flex-col gap-1">
		<!-- eslint-disable-next-line svelte/no-navigation-without-resolve -- app has no base path; the edit page comes with a later ticket -->
		<a class="block truncate" href="/items/{item.id}" title={item.text}>{item.text}</a>
		{#if hasMeta}
			<div class="flex items-center gap-2 text-xs">
				{#if item.important}
					<span class="text-mark-important" title="Important" aria-label="Important">■</span>
				{/if}
				{#if item.urgent}
					<span class="text-mark-urgent" title="Urgent" aria-label="Urgent">▲</span>
				{/if}
				{#if item.dueDate}
					<span
						class="tabular-nums {overdue ? 'text-danger' : 'text-text-muted'}"
						title={dueTooltip(item.dueDate)}>{dueLabel(item.dueDate, today)}</span
					>
				{/if}
				{#if item.notes}
					<span class="text-text-muted" title="Has notes" aria-label="Has notes">≡</span>
				{/if}
				{#each item.tags as tag (tag)}
					<span class="rounded-full border border-border px-2 whitespace-nowrap text-text-muted"
						>{tag}</span
					>
				{/each}
				{#if error}<span class="text-danger" role="alert">{error}</span>{/if}
			</div>
		{/if}
	</div>
	{#if item.category}
		<span class="max-w-40 shrink-0 truncate text-xs text-text-muted">{item.category}</span>
	{/if}
</li>
