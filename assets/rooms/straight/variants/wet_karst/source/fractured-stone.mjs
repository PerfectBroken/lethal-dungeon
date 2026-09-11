import * as T from 'three';
import {mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';
// Refine each independently fractured hull; mild erosion rounds edges while
// retaining the original anisotropic silhouette and adding small surface pits.
export function refineStone(source,salt,passes=2){
 const base=source.clone();base.deleteAttribute('normal');base.deleteAttribute('uv');let g=mergeVertices(base,1e-5);base.dispose();g.computeBoundingBox();const bounds=g.boundingBox.clone();
 for(let pass=0;pass<passes;pass++){
  const p=[...g.attributes.position.array],idx=[],edges=new Map();const middle=(a,b)=>{const k=a<b?`${a}:${b}`:`${b}:${a}`;if(edges.has(k))return edges.get(k);const n=p.length/3;for(let j=0;j<3;j++)p.push((p[a*3+j]+p[b*3+j])*.5);edges.set(k,n);return n;};
  for(let i=0;i<g.index.count;i+=3){const a=g.index.getX(i),b=g.index.getX(i+1),c=g.index.getX(i+2),ab=middle(a,b),bc=middle(b,c),ca=middle(c,a);idx.push(a,ab,ca,ab,b,bc,ca,bc,c,ab,bc,ca);}
  g.dispose();g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(p,3));g.setIndex(idx);
 }
 const p=g.attributes.position,adj=Array.from({length:p.count},()=>new Set());for(let i=0;i<g.index.count;i+=3){const ids=[0,1,2].map(k=>g.index.getX(i+k));for(const a of ids)for(const b of ids)if(a!==b)adj[a].add(b);}
 for(let pass=0;pass<3;pass++){const copy=p.array.slice();for(let i=0;i<p.count;i++)for(let k=0;k<3;k++){let mean=0;for(const j of adj[i])mean+=copy[j*3+k];p.array[i*3+k]=copy[i*3+k]*.82+mean/adj[i].size*.18;}}
 g.computeVertexNormals();const normals=g.attributes.normal,extent=bounds.getSize(new T.Vector3()).length();for(let i=0;i<p.count;i++){const x=p.getX(i),y=p.getY(i),z=p.getZ(i),grain=(Math.sin(x*27+y*19+salt)*Math.sin(z*23-x*9)+.4*Math.sin(x*61-z*43+y*31+salt))*extent*.008;p.setXYZ(i,x+normals.getX(i)*grain,y+normals.getY(i)*grain,z+normals.getZ(i)*grain);}
 g.computeVertexNormals();g.computeBoundingBox();const center=g.boundingBox.getCenter(new T.Vector3()),size=g.boundingBox.getSize(new T.Vector3());const textured=g.toNonIndexed(),tp=textured.attributes.position,uv=[];
 for(let i=0;i<tp.count;i+=3){const tri=[];for(let k=0;k<3;k++){const v=new T.Vector3().fromBufferAttribute(tp,i+k).sub(center).divide(size);tri.push([Math.atan2(v.z,v.x)/(Math.PI*2)+.5,Math.asin(v.y/v.length())/Math.PI+.5]);}if(Math.max(...tri.map(t=>t[0]))-Math.min(...tri.map(t=>t[0]))>.5)for(const t of tri)if(t[0]<.5)t[0]++;for(const t of tri)uv.push(...t);}
 textured.setAttribute('uv',new T.Float32BufferAttribute(uv,2));g.dispose();source.dispose();return textured;
}
