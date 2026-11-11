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
		// The Core compares Origin with Host on cookie-authenticated writes, so the browser's Host must
		// reach it unchanged. Vite's proxy rewrites Host to the target, so put the original one back.
		proxy: {
			'/api': {
				target: 'http://127.0.0.1:47822',
				configure: (proxy) => {
					proxy.on('proxyReq', (proxyReq, req) => {
						if (req.headers.host) proxyReq.setHeader('host', req.headers.host);
					});
				}
			}
		}
	},
	test: { include: ['src/**/*.test.ts'] }
});
