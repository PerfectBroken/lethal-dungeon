import test from 'node:test';
import assert from 'node:assert/strict';
import {readFileSync,readdirSync} from 'node:fs';
import {gunzipSync} from 'node:zlib';
const json=p=>JSON.parse(readFileSync(p,'utf8'));
test('ART051 identity matches canonical catalog and exported artifacts',()=>{
 const identity=json('identity.json');
 const catalog=json('../../../../../docs/examples/base-rooms-v1.json');
 const room=catalog.rooms.find(r=>r.id===identity.roomId);
 assert.ok(room);assert.equal(identity.roomName,room.name);assert.equal(identity.prefabKey,room.prefabKey);
 assert.equal(identity.catalogVersion,catalog.catalogVersion);assert.equal(identity.roomRevision,room.revision);
 assert.equal(identity.variantId,'goblin_habitat');
 assert.deepEqual(json('model/model-info.json').roomIdentity,identity);
 const b=readFileSync('model/dead-end-cave.glb');
 const gltf=JSON.parse(b.subarray(20,20+b.readUInt32LE(12)).toString().trim());
 assert.deepEqual(gltf.nodes.find(n=>n.name==='dead_end_cave_v1').extras.roomIdentity,identity);
 const packed=Buffer.concat(readdirSync('source/packed').filter(n=>n.startsWith('cave.glb.gz.part')).sort().map(n=>readFileSync('source/packed/'+n)));
 assert.deepEqual(gunzipSync(packed),b);
});
