<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import { api } from '#lib/api/client.ts';
	import ItemRow from '#lib/components/ItemRow.svelte';
	import { errorText } from '#lib/errorText.ts';
	import { isFiltered } from '#lib/inboxFilter.ts';
	import FilterBar from './FilterBar.svelte';

	let { data } = $props();

	// Failed "done" calls by Item id; the Item stays in the list with the message on its row.
	let rowErrors = $state<Record<number, string>>({});

	async function markDone(id: number, box: HTMLInputElement) {
		delete rowErrors[id];
		try {
			const {
				data: item,
				response,
				error
			} = await api.POST('/api/items/{id}/done', {
				params: { path: { id } }
			});
			if (item) {
				await invalidateAll();
				return;
			}
			rowErrors[id] = errorText({ response, error });
		} catch (thrown) {
			rowErrors[id] = errorText({ thrown });
		}
		box.checked = false;
	}
</script>

<svelte:head><title>Inbox · Organizer</title></svelte:head>

<h1 class="mb-3 text-xl font-semibold">Inbox</h1>

<FilterBar filter={data.filter} categories={data.categories} tags={data.tags} />

{#if data.error}
	<p class="text-danger" role="alert">{data.error}</p>
{:else if data.items.length === 0}
	<p class="text-text-muted">
		{isFiltered(data.filter) ? 'No Items match this filter' : 'Inbox is empty'}
	</p>
{:else}
	<ul class="rounded-panel border border-border bg-surface-raised">
		{#each data.items as item (item.id)}
			<ItemRow {item} overdueHighlight error={rowErrors[item.id]}>
				{#snippet action()}
					<input
						type="checkbox"
						class="m-0 accent-accent"
						aria-label="Mark done"
						onchange={(e) => void markDone(item.id, e.currentTarget)}
					/>
				{/snippet}
			</ItemRow>
		{/each}
	</ul>
{/if}
