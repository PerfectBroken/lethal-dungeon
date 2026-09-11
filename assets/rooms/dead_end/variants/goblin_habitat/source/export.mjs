import {writeFile,mkdir} from 'node:fs/promises';
import {GLTFExporter} from 'three/addons/exporters/GLTFExporter.js';
import {buildRoom} from './room.mjs';
globalThis.FileReader=class {readAsArrayBuffer(blob){blob.arrayBuffer().then(r=>{this.result=r;this.onloadend?.();});}readAsDataURL(blob){blob.arrayBuffer().then(r=>{this.result='data:application/octet-stream;base64,'+Buffer.from(r).toString('base64');this.onloadend?.();});}};
const room=buildRoom();const buffer=await new GLTFExporter().parseAsync(room,{binary:true,onlyVisible:false});
await mkdir('model',{recursive:true});await writeFile('model/dead-end-cave.glb',Buffer.from(buffer));
let triangles=0;room.traverse(o=>{if(o.isMesh)triangles+=(o.geometry.index?.count??o.geometry.attributes.position.count)/3});
await writeFile('model/model-info.json',JSON.stringify({name:room.name,roomIdentity:room.userData.roomIdentity,triangles,meshes:room.children.length,bytes:buffer.byteLength,units:'metres',bounds:'8 x 8 x 4',stage:'review model',collision:'design envelopes only; engine validation pending'},null,2));console.log({triangles,meshes:room.children.length,bytes:buffer.byteLength});
