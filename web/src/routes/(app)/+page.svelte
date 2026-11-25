<script lang="ts">
	import { api } from '#lib/api/client.ts';
	import { coreStatusText } from '#lib/health.ts';

	let text = $state('Core: …');

	$effect(() => {
		api.GET('/api/health').then(
			({ response }) => (text = coreStatusText({ status: response.status })),
			(error) => (text = coreStatusText({ error }))
		);
	});
</script>

<p class="p-4">{text}</p>
