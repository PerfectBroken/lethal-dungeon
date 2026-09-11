import * as T from 'three';
import sharp from 'sharp';
import {createHash} from 'node:crypto';
import {readFile,writeFile} from 'node:fs/promises';

// Bake world-space triplanar PBR into a portable atlas. No preview-only shader.
export async function bakeRock(room){
 const names=['buttressScan','west','scannedWalls'];
 const meshes=names.map(n=>room.getObjectByName(n));
 const signature=await rockBakeSignature(meshes);let cached;try{cached=JSON.parse(await readFile('textures/unified-rock-cache.json','utf8'));}catch{}const reuse=cached?.signature===signature;
 const sources=[];
 if(!reuse)for(const name of ['Diffuse','nor_gl','arm'])sources.push(await sharp('textures/rock_boulder_dry_'+name+'.jpg').resize(1024,1024).removeAlpha().raw().toBuffer());
 const count=meshes.reduce((n,m)=>n+m.geometry.index.count/3,0),size=8192,columns=Math.ceil(Math.sqrt(count)),tile=Math.floor(size/columns),padding=2,span=tile-1-padding*2;
 const out=reuse?[]:[Buffer.alloc(size*size*3),Buffer.alloc(size*size*3),Buffer.alloc(size*size*3)];
 const sample=(src,u,v,c)=>{u=((u%1)+1)%1*1023;v=((v%1)+1)%1*1023;const x=Math.floor(u),y=Math.floor(v),fx=u-x,fy=v-y,at=(dx,dy)=>src[((y+dy)%1024*1024+(x+dx)%1024)*3+c];return (at(0,0)*(1-fx)+at(1,0)*fx)*(1-fy)+(at(0,1)*(1-fx)+at(1,1)*fx)*fy;};
 let face=0;
 for(const m of meshes){
  const old=m.geometry,g=old.toNonIndexed(),p=g.attributes.position,n=g.attributes.normal,uv=new Float32Array(p.count*2),colors=g.attributes.color;
  for(let i=0;i<p.count;i+=3,face++){
   const tx=(face%columns)*tile,ty=Math.floor(face/columns)*tile;
   const a=new T.Vector3().fromBufferAttribute(p,i),b=new T.Vector3().fromBufferAttribute(p,i+1),c=new T.Vector3().fromBufferAttribute(p,i+2),e1=b.clone().sub(a),e2=c.clone().sub(a);
   const ns=[0,1,2].map(j=>new T.Vector3().fromBufferAttribute(n,i+j));
   for(let j=0;j<3;j++){uv[(i+j)*2]=(tx+padding+(j===1?span:0)+.5)/size;uv[(i+j)*2+1]= (ty+padding+(j===2?span:0)+.5)/size;const x=p.getX(i+j),y=p.getY(i+j),z=p.getZ(i+j),layer=.88+.07*Math.sin(y*8+z*1.2+x*.9+.7*Math.sin(z*2+y));colors.setXYZ(i+j,layer,layer*.97,layer*.92);}
   if(!reuse)for(let py=0;py<tile;py++)for(let px=0;px<tile;px++){
    let u=Math.max(0,(px-padding)/span),v=Math.max(0,(py-padding)/span);if(u+v>1){const q=u+v;u/=q;v/=q;}
    const point=a.clone().addScaledVector(e1,u).addScaledVector(e2,v),normal=ns[0].clone().multiplyScalar(1-u-v).addScaledVector(ns[1],u).addScaledVector(ns[2],v).normalize();
    const weights=[Math.abs(normal.x)**4,Math.abs(normal.y)**4,Math.abs(normal.z)**4],sum=weights.reduce((a,b)=>a+b,0)||1;
    const warp=.09*Math.sin(point.y*2.4+point.z*.8)+.025*Math.sin(point.y*11+point.x*2),coords=[[point.z/1.25,(point.y+warp)/1.25],[point.x/1.25,point.z/1.25],[point.x/1.25,(point.y+warp)/1.25]];
    const stain=.90+.08*Math.sin(point.y*2.8+point.z*.6+point.x*.4),mud=.88+.12*Math.min(1,Math.max(0,point.y/.65));
    const k=((ty+py)*size+tx+px)*3;
    for(let ch=0;ch<3;ch++){
     let color=0,arm=0;for(let axis=0;axis<3;axis++){const w=weights[axis]/sum;color+=sample(sources[0],...coords[axis],ch)*w;arm+=sample(sources[2],...coords[axis],ch)*w;}
     out[0][k+ch]=Math.max(0,Math.min(255,color*[.65,.59,.50][ch]*stain*mud));out[2][k+ch]=ch===2?0:ch===1?Math.max(190,arm):255;
    }
    // Blend surface slopes in world space, then encode in this atlas triangle's TBN.
    const detail=new T.Vector3();for(let axis=0;axis<3;axis++){const dx=(sample(sources[1],...coords[axis],0)/127.5-1)*.55,dy=(sample(sources[1],...coords[axis],1)/127.5-1)*.55,w=weights[axis]/sum;detail.addScaledVector(axis===0?new T.Vector3(0,dy,dx):axis===1?new T.Vector3(dx,0,dy):new T.Vector3(dx,dy,0),w);}
    detail.add(normal).normalize();const tangent=e1.clone().addScaledVector(normal,-e1.dot(normal)).normalize(),bitangent=new T.Vector3().crossVectors(normal,tangent).normalize();if(bitangent.dot(e2)<0)bitangent.negate();
    out[1][k]=Math.round(127.5*(1+detail.dot(tangent)));out[1][k+1]=Math.round(127.5*(1+detail.dot(bitangent)));out[1][k+2]=Math.round(127.5*(1+detail.dot(normal)));
   }
  }
  g.setAttribute('uv',new T.BufferAttribute(uv,2));g.clearGroups();g.setIndex(Array.from({length:p.count},(_,i)=>i));m.geometry=g;old.dispose();m.material.color.set(0xffffff);
 }
 if(!reuse)for(let i=0;i<3;i++)await sharp(out[i],{raw:{width:size,height:size,channels:3}}).jpeg({quality:i===1?95:92}).toFile('textures/unified-rock-'+['diffuse','normal','arm'][i]+'.jpg');
 const result={size,triangles:count,tile,method:'world-space triplanar baked atlas',signature};await writeFile('textures/unified-rock-cache.json',JSON.stringify(result));return result;
}

export async function rockBakeSignature(meshes){
 const hash=createHash('sha256');hash.update(await readFile(new URL(import.meta.url)));
 for(const suffix of ['Diffuse','nor_gl','arm'])hash.update(await readFile('textures/rock_boulder_dry_'+suffix+'.jpg'));
 for(const m of meshes){hash.update(Buffer.from(m.geometry.attributes.position.array.buffer));hash.update(Buffer.from(m.geometry.attributes.normal.array.buffer));}return hash.digest('hex');
}
