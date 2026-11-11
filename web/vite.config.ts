import adapter from '@sveltejs/adapter-static';
import { sveltekit } from '@sveltejs/kit/vite';
import tailwindcss from '@tailwindcss/vite';
import { defineConfig } from 'vitest/config';

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit({
			adapter: adapter({ fallback: 'index.html' }),
			csp: {
				mode: 'hash',
				directives: {
					'script-src': ['self'],
					'style-src': ['self'],
					'object-src': ['none'],
					'base-uri': ['self']
				}
			}
		})
	],
	server: {
		// No changeOrigin: the Core compares Origin with Host on cookie-authenticated writes.
		proxy: { '/api': 'http://127.0.0.1:47822' }
	},
	test: { include: ['src/**/*.test.ts'] }
});
