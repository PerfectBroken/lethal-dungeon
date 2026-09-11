import test from 'node:test';
import assert from 'node:assert/strict';
import {readFile} from 'node:fs/promises';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
import {Box3} from 'three';
import sharp from 'sharp';
// ImageBitmapLoader adapter decodes embedded PNGs for a Node-side GLB round trip.
globalThis.self=globalThis;
globalThis.createImageBitmap=async blob=>{const {data,info}=await sharp(Buffer.from(await blob.arrayBuffer())).ensureAlpha().raw().toBuffer({resolveWithObject:true});return {data,width:info.width,height:info.height,close(){}}};
test('ART011 self-contained textured GLB decodes and round-trips',async()=>{const b=await readFile('model/dead-end-cave.glb');assert.equal(b.toString('ascii',0,4),'glTF');assert.equal(b.readUInt32LE(4),2);assert.equal(b.readUInt32LE(8),b.length);const j=JSON.parse(b.subarray(20,20+b.readUInt32LE(12)).toString().trim());assert.equal(j.meshes.length,17);assert.ok(j.buffers.every(b=>!b.uri));assert.equal(j.images.length,12);assert.ok(j.images.every(i=>i.bufferView!==undefined&&!i.uri));assert.ok(!j.cameras);assert.ok(!j.extensions?.KHR_lights_punctual);const g=await new GLTFLoader().parseAsync(b.buffer.slice(b.byteOffset,b.byteOffset+b.length),'');for(const name of ['crag','walls','floor','columns','timber','hides']){const mesh=g.scene.getObjectByName(name);assert.ok(mesh.material.map?.image.width>=1024);assert.ok(mesh.material.normalMap?.image.width>=1024);}const box=new Box3().setFromObject(g.scene);assert.ok(box.max.y>=3.99);});
