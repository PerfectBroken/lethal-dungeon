import{readFile,writeFile,readdir,mkdir}from'node:fs/promises';
import{gunzipSync}from'node:zlib';
const files=(await readdir('source/packed')).filter(n=>n.startsWith('cave.glb.gz.part')).sort();const packed=Buffer.concat(await Promise.all(files.map(n=>readFile('source/packed/'+n))));await mkdir('model',{recursive:true});await writeFile('model/dead-end-cave.glb',gunzipSync(packed));
