import createClient from 'openapi-fetch';
import type { paths } from './schema';

// The paths in docs/openapi.json already start with /api, so the base is empty.
export const api = createClient<paths>({ baseUrl: '', credentials: 'same-origin' });
