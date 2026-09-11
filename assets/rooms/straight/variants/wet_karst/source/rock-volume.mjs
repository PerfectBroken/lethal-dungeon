import * as T from 'three';
import {mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';

// A scan is only a skin. Retain its cave-facing surface and close a second,
// corridor-facing surface against every true boundary (UV seams are welded
// for boundary detection only, so the scan's texture coordinates survive).
export function solidifyRock(g,options={}){
 if(!g.index)g.setIndex(Array.from({length:g.attributes.position.count},(_,i)=>i));
 const p=g.attributes.position,uv=g.attributes.uv,c=g.attributes.color,n=p.count;
 // Remove scan-boundary stair steps before making their depth visible.
 const boundaryEdges=new Map(),vertexKeys=[],equivalent=new Map();
 for(let i=0;i<n;i++){const k=[p.getX(i),p.getY(i),p.getZ(i)].map(v=>Math.round(v*1e5)).join(',');vertexKeys.push(k);if(!equivalent.has(k))equivalent.set(k,[]);equivalent.get(k).push(i);}
 for(let i=0;i<g.index.count;i+=3)for(let j=0;j<3;j++){const a=vertexKeys[g.index.getX(i+j)],b=vertexKeys[g.index.getX(i+(j+1)%3)];if(a===b)continue;const key=[a,b].sort().join('|');const e=boundaryEdges.get(key);if(e)e.count++;else boundaryEdges.set(key,{a,b,count:1});}
 const adjacency=new Map();for(const {a,b,count}of boundaryEdges.values())if(count===1){if(!adjacency.has(a))adjacency.set(a,new Set());if(!adjacency.has(b))adjacency.set(b,new Set());adjacency.get(a).add(b);adjacency.get(b).add(a);}
 for(let iteration=0;iteration<24;iteration++){const updates=[];for(const [k,neighbours]of adjacency){if(neighbours.size!==2)continue;const v=new T.Vector3().fromBufferAttribute(p,equivalent.get(k)[0]),mean=new T.Vector3();for(const q of neighbours)mean.add(new T.Vector3().fromBufferAttribute(p,equivalent.get(q)[0]));mean.multiplyScalar(.5);v.lerp(mean,.45);updates.push([k,v]);}for(const [k,v]of updates)for(const i of equivalent.get(k))p.setXYZ(i,v.x,v.y,v.z);}

 const positions=[...p.array],uvs=[...uv.array],colors=[...c.array],indices=[...g.index.array];
 const keys=[],ids=new Map(),edges=new Map();
 for(let i=0;i<n;i++){
  const x=p.getX(i),y=p.getY(i),z=p.getZ(i),thickness=.95+.55*Math.exp(-Math.pow(z/1.6,2))*(.4+.6*Math.sin(Math.PI*y/4))+.12*T.MathUtils.smoothstep(z,0,3);
  positions.push(options.backX??Math.min(-.99,x+thickness),y,z);uvs.push(uv.getX(i),uv.getY(i));colors.push(c.getX(i),c.getY(i),c.getZ(i));
  const key=[x,y,z].map(v=>Math.round(v*1e5)).join(',');if(!ids.has(key))ids.set(key,ids.size);keys.push(ids.get(key));
 }
 for(let i=0;i<g.index.count;i+=3){const a=g.index.getX(i),b=g.index.getX(i+1),d=g.index.getX(i+2);indices.push(d+n,b+n,a+n);for(const [u,v]of [[a,b],[b,d],[d,a]]){if(keys[u]===keys[v])continue;const key=[keys[u],keys[v]].sort((a,b)=>a-b).join(':');const e=edges.get(key);if(e)e.count++;else edges.set(key,{u,v,count:1});}}
 for(const {u,v,count}of edges.values())if(count===1){
  // Rounded fracture shoulder, not a planar extrusion end cap.
  const base=positions.length/3,segments=options.segments??8;
  for(let step=0;step<=segments;step++)for(const j of [u,v]){
   const t=step/segments,across=t,x=positions[j*3]+(positions[(j+n)*3]-positions[j*3])*across,y=positions[j*3+1],z=positions[j*3+2];
   const radial=new T.Vector2((y-2)*.6,z-.2).normalize(),shoulder=t<.3?t/.3:t>.7?(1-t)/.3:1,bulge=options.backX!==undefined?0:.64*shoulder+.035*Math.sin(Math.PI*t);
   const yy=Math.max(0,Math.min(4,y+radial.x*bulge)),zz=z+radial.y*bulge;
   const strata=.12*Math.sin(yy*5.3+t*3+Math.sin(yy*1.9))+.052*Math.sin(yy*16-t*7)+.019*Math.sin(yy*47+t*19);
   const texturedZ=Math.max(options.zMin??-3.14,Math.min(options.zMax??3.13,zz+(options.backX!==undefined?0:Math.sin(Math.PI*t)*strata)));
   positions.push(x,yy,texturedZ);colors.push(...colors.slice(j*3,j*3+3));if(options.backX!==undefined)uvs.push(x/1.4,(yy+zz*.35)/1.4);else {const topEdge=Math.abs((p.getY(u)+p.getY(v))*.5-2)>1.8&&Math.abs((p.getZ(u)+p.getZ(v))*.5-.2)<2.5;uvs.push(x/1.7,(topEdge?texturedZ:yy)/1.7);}
  }
  for(let step=0;step<segments;step++){const a=base+step*2;indices.push(a+1,a,a+2,a+3,a+1,a+2);}
 }
 const result=new T.BufferGeometry();result.setAttribute('position',new T.Float32BufferAttribute(positions,3));result.setAttribute('uv',new T.Float32BufferAttribute(uvs,2));result.setAttribute('color',new T.Float32BufferAttribute(colors,3));result.setIndex(indices);result.addGroup(0,g.index.count*2,0);result.addGroup(g.index.count*2,indices.length-g.index.count*2,1);const welded=mergeVertices(result,.00001);result.dispose();welded.computeVertexNormals();
 if(options.backX===undefined){const p=welded.attributes.position,normal=welded.attributes.normal,sums=new Map(),keys=[];for(let i=0;i<p.count;i++){const key=[p.getX(i),p.getY(i),p.getZ(i)].map(v=>Math.round(v*1e5)).join(',');keys.push(key);if(!sums.has(key))sums.set(key,new T.Vector3());sums.get(key).add(new T.Vector3().fromBufferAttribute(normal,i));}for(let i=0;i<p.count;i++){const v=sums.get(keys[i]).clone().normalize();normal.setXYZ(i,v.x,v.y,v.z);}}
 return welded;
}

// Relax the joined volume, including cap/scan UV seams, rather than rounding a separate strip.
export function relaxRock(g,iterations=8){
 const p=g.attributes.position,groups=new Map(),ids=[],neighbours=new Map();
 for(let i=0;i<p.count;i++){const k=[p.getX(i),p.getY(i),p.getZ(i)].map(v=>Math.round(v*1e5)).join(',');ids.push(k);if(!groups.has(k))groups.set(k,[]);groups.get(k).push(i);}
 for(let i=0;i<g.index.count;i+=3)for(let j=0;j<3;j++){const a=ids[g.index.getX(i+j)],b=ids[g.index.getX(i+(j+1)%3)];if(a===b)continue;if(!neighbours.has(a))neighbours.set(a,new Set());if(!neighbours.has(b))neighbours.set(b,new Set());neighbours.get(a).add(b);neighbours.get(b).add(a);}
 for(let k=0;k<iterations;k++){const updates=[];for(const [id,list]of groups){const v=new T.Vector3().fromBufferAttribute(p,list[0]),adj=neighbours.get(id);if(!adj?.size)continue;const mean=new T.Vector3();for(const next of adj)mean.add(new T.Vector3().fromBufferAttribute(p,groups.get(next)[0]));mean.multiplyScalar(1/adj.size);const oldY=v.y;v.lerp(mean,.24);if(oldY<.08)v.y=oldY;if(oldY>3.94)v.y=oldY;updates.push([list,v]);}for(const [list,v]of updates)for(const i of list)p.setXYZ(i,v.x,v.y,v.z);}
 g.computeVertexNormals();const n=g.attributes.normal;for(const list of groups.values()){const normal=new T.Vector3();for(const i of list)normal.add(new T.Vector3().fromBufferAttribute(n,i));normal.normalize();for(const i of list)n.setXYZ(i,normal.x,normal.y,normal.z);}return g;
}
