import {defineConfig} from 'vite';import path from 'node:path';export default defineConfig({cacheDir:'.vite-cache',server:{fs:{allow:[path.resolve(import.meta.dirname,'../../..')]}}});
