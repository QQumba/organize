<script lang="ts">
	import { resolve } from '$app/paths';

	const names = Object.keys(import.meta.glob('./*/+page.svelte'))
		.map((path) => path.split('/')[1])
		.sort();
</script>

<svelte:head><title>Playground</title></svelte:head>

<main class="mx-auto max-w-[760px] p-6">
	<h1 class="mb-4 text-xl font-semibold">Playground</h1>

	<h2 class="mb-2 font-semibold">Rules</h2>
	<ul class="mb-6 list-disc space-y-1 pl-5 text-sm">
		<li>
			Each prototype lives in its own folder <code>src/routes/dev/playground/&lt;name&gt;/</code> and
			starts with a header comment: the question it answers, the date and the ticket.
		</li>
		<li>
			The playground is an archive. The user deletes old prototypes by hand (deleting the folder is
			enough). Agents never delete or overwrite an existing prototype; a new idea gets a new folder.
		</li>
		<li>
			Prototypes are references only. They are never moved or copied into production code.
			Production code is written fresh in <code>src/lib</code> and <code>src/routes</code>,
			following the theme tokens and the component gallery.
		</li>
		<li>
			Nothing in <code>src/lib</code> imports from the playground, and prototypes do not import from
			each other or from <code>src/lib</code>.
		</li>
		<li>The playground is safe to deploy; nothing hides it in production.</li>
	</ul>

	<h2 class="mb-2 font-semibold">Prototypes</h2>
	<ul class="list-disc space-y-1 pl-5">
		{#each names as name (name)}
			<li>
				<a class="underline" href={resolve(`/dev/playground/${name}` as '/dev/playground/look')}
					>{name}</a
				>
			</li>
		{/each}
	</ul>
</main>
