<script lang="ts">
	import RestoreList from '../RestoreList.svelte';

	let { data } = $props();
</script>

<svelte:head><title>Archive · Organizer</title></svelte:head>

<h1 class="mb-3 text-xl font-semibold">Archive</h1>

<RestoreList
	items={data.items}
	error={data.error}
	emptyText="Archive is empty"
	stampField="archivedAt"
	stampPrefix="Archived"
/>
