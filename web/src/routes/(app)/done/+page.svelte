<script lang="ts">
	import RestoreList from '../RestoreList.svelte';

	let { data } = $props();
</script>

<svelte:head><title>Done · Organizer</title></svelte:head>

<h1 class="mb-3 text-xl font-semibold">Done</h1>

<RestoreList
	items={data.items}
	error={data.error}
	emptyText="Nothing done yet"
	stampField="doneAt"
	stampPrefix="Done"
/>
