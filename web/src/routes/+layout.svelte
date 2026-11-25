<script lang="ts">
	import '../app.css';
	import CoreBanner from '#lib/components/CoreBanner.svelte';
	import LoadingBar from '#lib/components/LoadingBar.svelte';

	let { children } = $props();
</script>

<CoreBanner />
<LoadingBar />
{@render children()}
