// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// tsc only emits TypeScript. This copies the stylesheets that ship with the package from src to dist,
// keeping their relative paths, so that the `./collaboration/collaboration.css` export resolves.

import { cpSync, mkdirSync, readdirSync, statSync } from 'node:fs';
import { dirname, join, relative } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const source = join(root, 'src');
const destination = join(root, 'dist');

const stylesheets = [];
const collect = directory => {
    for (const entry of readdirSync(directory)) {
        const path = join(directory, entry);
        if (statSync(path).isDirectory()) {
            collect(path);
        } else if (entry.endsWith('.css')) {
            stylesheets.push(path);
        }
    }
};
collect(source);

if (stylesheets.length === 0) {
    throw new Error('No stylesheets found under src; the package would ship without its styles.');
}

for (const stylesheet of stylesheets) {
    const target = join(destination, relative(source, stylesheet));
    mkdirSync(dirname(target), { recursive: true });
    cpSync(stylesheet, target);
    console.log(`Copied ${relative(root, stylesheet)} -> ${relative(root, target)}`);
}
