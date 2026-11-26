<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { api } from '#lib/api/client.ts';
	import ItemRow from '#lib/components/ItemRow.svelte';
	import { stampOf } from '#lib/dueDate.ts';
	import { errorText } from '#lib/errorText.ts';
	import type { components } from '#lib/api/schema.d.ts';

	type Item = components['schemas']['ItemDto'];

	// The list shared by Done and Archive: the shared Item row with a Restore button.
	let {
		items,
		error,
		emptyText,
		stampField,
		stampPrefix
	}: {
		items: Item[];
		error: string;
		emptyText: string;
		stampField: 'doneAt' | 'archivedAt';
		stampPrefix: string;
	} = $props();

	// Failed Restore calls by Item id; the Item stays in the list with the message on its row.
	let rowErrors = $state<Record<number, string>>({});
	const today = new Date();

	async function restore(id: number) {
		delete rowErrors[id];
		try {
			const {
				data: item,
				response,
				error: failure
			} = await api.POST('/api/items/{id}/restore', { params: { path: { id } } });
			if (item) {
				await invalidateAll();
				return;
			}
			rowErrors[id] = errorText({ response, error: failure });
		} catch (thrown) {
			rowErrors[id] = errorText({ thrown });
		}
	}

	function stamp(item: Item) {
		const at = item[stampField];
		return at ? stampOf(stampPrefix, at, today) : undefined;
	}
</script>

{#if error}
	<p class="text-danger" role="alert">{error}</p>
{:else if items.length === 0}
	<p class="text-text-muted">{emptyText}</p>
{:else}
	<ul class="rounded-panel border border-border bg-surface-raised">
		{#each items as item (item.id)}
			<ItemRow {item} error={rowErrors[item.id]} stamp={stamp(item)} {today}>
				{#snippet action()}
					<button class="btn btn-secondary" onclick={() => void restore(item.id)}>Restore</button>
				{/snippet}
			</ItemRow>
		{/each}
	</ul>
{/if}
