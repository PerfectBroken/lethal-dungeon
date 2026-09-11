import {ConvexGeometry} from 'three/addons/geometries/ConvexGeometry.js';
import roomIdentity from '../identity.json' with {type:'json'};
import palette from './palette.json' with {type:'json'};
import boneScans from './bone-scans.json' with {type:'json'};
import skullScan from './goat-skull.json' with {type:'json'};
import * as T from 'three';
import {mergeGeometries,mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';
export const route=[[2,3.65],[1,2.65],[0,1.6],[0,0],[-.6,-1.65]];
export const colliders=[
 {name:'crag',minX:.62,maxX:4,minZ:-1.7,maxZ:1.55},
 {name:'left column',minX:-3.55,maxX:-2.75,minZ:.55,maxZ:1.35},
 {name:'back column',minX:-.85,maxX:-.15,minZ:-3.7,maxZ:-3},
 {name:'shelter',minX:-3.65,maxX:-1.15,minZ:-3.7,maxZ:-1.2},
 {name:'edge formations',minX:-3.8,maxX:-3.1,minZ:1.65,maxZ:2.5},
 {name:'east formations',minX:3.35,maxX:3.95,minZ:2.1,maxZ:2.8}
];
export function buildRoom(){
 let seed=9618;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296};
 const ceilingTips=[];const wallBlocks=[],mineralClearance=[];let wallSoilFaces=0;const bins={};const root=new T.Group();root.name='dead_end_cave_v1';root.userData={roomIdentity,palette,metresPerUnit:1,source:'approved cave concept',skullSource:skullScan.credit,door:'south offset example',colliders,route};
 const colors={stone:0x514d42,soil:0x51412d,wood:0x49311c,straw:0x857044,bone:0xb3a17c,hide:0x65503a,web:0x8d9189,water:0x354744};
 function put(g,cat,c=colors.stone){
  g.deleteAttribute('uv');g.deleteAttribute('normal');
  const westSurface=g.userData?.westSurface;const geological=g.userData?.geological;const hard=g.userData?.hardFaces;const old=g;if(!hard){g=mergeVertices(g,0.0001);old.dispose();}g.computeVertexNormals();
  if(g.index){const old=g;g=g.toNonIndexed();old.dispose();}
  const p=g.attributes.position,n=g.attributes.normal,cs=[],uv=[];
  const textured=['walls','front','ceiling','crag','columns','formations','floor','wallFoot','debris','hides'].includes(cat);
  const base=new T.Color(cat==='hides'?0xf5eee2:textured?((cat==='floor'||cat==='wallFoot')?0xc5b699:0xbab8ad):c);
  for(let i=0;i<p.count;i++){
   const x=p.getX(i),y=p.getY(i),z=p.getZ(i);const shade=.83+.09*Math.sin(x*9+y*6+z*7)+.04*Math.sin(x*43+z*37+y*31);
   const ao=cat==='floor'?.92:Math.min(1,.75+y*.13);
   if((hard||geological)&&cat==='walls'){
    const up=T.MathUtils.smoothstep(n.getY(i),.18,.85),patch=.55+.25*Math.sin(x*11+z*7+y*4)+.2*Math.sin(x*27-z*19);
    const seep=geological?.16*Math.pow(.5+.5*Math.sin(x*7+z*9+.12*Math.sin(y*3)),5):0;const amount=up*Math.max(.12,patch)*.65+seep;const tint=base.clone().lerp(new T.Color(0x786047),amount);
    if(westSurface){
     const axis=z-.2*y+.09*geologyNoise(z*.9,y*.6);
     const deposits=T.MathUtils.smoothstep(geologyNoise(axis*5.4,y*.45),-.12,.7);
     const damp=T.MathUtils.smoothstep(geologyNoise(axis*2.1+15,y*.6),.1,.8);
     tint.lerp(new T.Color(0xd4c8ae),deposits*.58).lerp(new T.Color(0x7e7667),damp*.4);
    }
    cs.push(tint.r*shade*ao,tint.g*shade*ao,tint.b*shade*ao);if(i%3===0&&amount>.16)wallSoilFaces++;
   }else cs.push(base.r*shade*ao,base.g*shade*ao,base.b*shade*ao);
  }
  for(let i=0;i<p.count;i+=3){const nx=Math.abs(n.getX(i)+n.getX(i+1)+n.getX(i+2)),ny=Math.abs(n.getY(i)+n.getY(i+1)+n.getY(i+2)),nz=Math.abs(n.getZ(i)+n.getZ(i+1)+n.getZ(i+2));
   for(let j=0;j<3;j++){const k=i+j,x=p.getX(k),y=p.getY(k),z=p.getZ(k);if(hard&&cat==='walls'&&ny>nx&&ny>nz)uv.push(x*.65,z*.65);else if(cat==='hides')uv.push((x+3)/1.05,(z+3.4)/1.05);else if(['walls','front','crag'].includes(cat))uv.push((nx>nz?z:x)*.65,1-y/4);else if(ny>nx&&ny>nz)uv.push(x*.65,z*.65);else if(nx>nz)uv.push(z*.65,y*.65);else uv.push(x*.65,y*.65);}
  }
  g.setAttribute('color',new T.Float32BufferAttribute(cs,3));g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));(bins[cat]??=[]).push(g);
 }
 function fracturedStone(salt){
  const points=[];
  for(const x of [-1,1])for(const y of [-1,1])for(const z of [-1,1])for(let axis=0;axis<3;axis++){
   const v=[x,y,z],cut=.23+.16*Math.sin(salt+x*3+y*5+z*7+axis*2);v[axis]*=1-cut;
   points.push(new T.Vector3(v[0]+.17*v[1],v[1]*(.84+.1*Math.sin(salt+v[0]*2)),v[2]+.13*v[0]));
  }
  const raw=new ConvexGeometry(points),p=raw.attributes.position,a=[];
  // Midpoint subdivision gives soil gradients room without rounding fracture planes.
  for(let i=0;i<p.count;i+=3){const v=[0,1,2].map(j=>new T.Vector3().fromBufferAttribute(p,i+j));const ab=v[0].clone().lerp(v[1],.5),bc=v[1].clone().lerp(v[2],.5),ca=v[2].clone().lerp(v[0],.5);for(const q of [v[0],ab,ca,ab,v[1],bc,ca,bc,v[2],ab,bc,ca])a.push(...q.toArray());}
  raw.dispose();const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(a,3));return g;
 }
 function rock(pos,scale,cat='walls',c=colors.stone,detail=2){
  const angular=cat==='walls';const salt=rand()*9;const g=angular?fracturedStone(salt):new T.IcosahedronGeometry(1,detail),p=g.attributes.position;
  for(let i=0;i<p.count;i++){const x=p.getX(i),y=p.getY(i),z=p.getZ(i);const k=1+.095*Math.sin(x*8+y*4+salt)*Math.cos(z*9-salt);p.setXYZ(i,angular?x*(.86+.18*Math.sin(y*5+salt))+.18*y:x*k,angular?y*(1.05+.19*Math.cos(z*4+salt)):y*k,angular?z*(.85+.22*Math.sin(x*6+salt))+.16*x:z*k);}
  g.scale(...scale);g.rotateY(rand()*6.28);g.translate(...pos);if(cat==='walls'){for(let i=0;i<p.count;i++)p.setXYZ(i,Math.max(-4,Math.min(4,p.getX(i))),Math.max(0,Math.min(4,p.getY(i))),Math.max(-4,Math.min(4,p.getZ(i))));}if(angular){g.userData={hardFaces:true};g.computeBoundingBox();wallBlocks.push({min:g.boundingBox.min.toArray(),max:g.boundingBox.max.toArray()});}put(g,cat,c);
 }
 function rod(a,b,r,cat,c=colors.wood,r2=r*.84,sides=7){const start=new T.Vector3(...a),end=new T.Vector3(...b),d=end.clone().sub(start);const g=new T.CylinderGeometry(r2,r,d.length(),sides,1);g.applyQuaternion(new T.Quaternion().setFromUnitVectors(new T.Vector3(0,1,0),d.normalize()));g.translate(...start.add(end).multiplyScalar(.5).toArray());put(g,cat,c);}
 function triSurface(n,m,fn,cat,c){const a=[];for(let i=0;i<n;i++)for(let j=0;j<m;j++){const p=fn(i/n,j/m),q=fn((i+1)/n,j/m),r=fn((i+1)/n,(j+1)/m),s=fn(i/n,(j+1)/m);a.push(...p,...q,...s,...q,...r,...s);}const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(a,3));put(g,cat,c);}
 const rough=(x,y)=>.1*Math.sin(x*2.4+y*1.7)+.055*Math.cos(x*6.7-y*3.1)+.023*Math.sin(x*19+y*13);
 triSurface(85,85,(u,v)=>{const x=u*8-4,z=v*8-4;const edge=Math.max(0,1-Math.min(4-Math.abs(x),z+4)/.9);return[x,.01+rough(x,z)*.22+edge*edge*(.13+.06*Math.sin(x*3+z*4)),z]},'floor',colors.soil);
 // Large fracture domains share a continuous mother-rock surface.
 root.userData.wallStructure='continuous fractured bedrock';root.userData.scatteredWallRocks=0;root.userData.geologicalWalls=[];
 const geologyNoise=(x,y)=>{
  const ix=Math.floor(x),iy=Math.floor(y),fx=x-ix,fy=y-iy,sx=fx*fx*(3-2*fx),sy=fy*fy*(3-2*fy);
  const hash=(a,b)=>{let h=Math.imul(a,374761393)^Math.imul(b,668265263);h=Math.imul(h^(h>>>13),1274126177);return((h^(h>>>16))>>>0)/4294967295*2-1;};
  return T.MathUtils.lerp(T.MathUtils.lerp(hash(ix,iy),hash(ix+1,iy),sx),T.MathUtils.lerp(hash(ix,iy+1),hash(ix+1,iy+1),sx),sy);
 };
 const domains=[[-3.25,.55],[-.8,.6],[2.3,.4],[-3,2.8],[-.75,3.3],[1.45,2.45],[3.6,3.2]];
 for(const [sideIndex,side]of ['west','east','north'].entries()){
  const nx=side==='west'?160:112,ny=64,vertices=[],reliefs=[];let maxNeighbourStep=0;
  const surface=(u,v)=>{
   const a=u*8-4,y=v*4,wx=a+.22*y+.085*geologyNoise(a*2,y*2),wy=y+.12*geologyNoise(a*1.4,sideIndex+2);
   const distances=domains.map(([cx,cy],i)=>({d:Math.hypot((wx-cx)*.86,wy-cy),i})).sort((a,b)=>a.d-b.d);
   const id=distances[0].i,[cx,cy]=domains[id],gap=distances[1].d-distances[0].d;
   const plane=.38+.07*Math.sin(id*2.3+sideIndex)+.075*(wx-cx)-.045*(wy-cy);
   const fissure=.19*Math.exp(-Math.pow(gap/.085,2));
   const fluting=.025*geologyNoise(a*12+.16*y,y*.8+sideIndex);
   const weathering=.055*geologyNoise(a*3,y*3+sideIndex)+.023*geologyNoise(a*9,y*9)+.008*geologyNoise(a*23,y*21);
   const layerAxis=a-.20*y+.075*geologyNoise(a*.8,y*.7);
   const westRelief=.39+.135*geologyNoise(layerAxis*.85,3+y*.18)
    +.082*geologyNoise(layerAxis*3.5,8+y*.28)
    +.026*geologyNoise(layerAxis*9,13+y*.43)
    +.009*geologyNoise(a*19,y*12);
   let flakes=0;
   const ridgeCenters=[-3.72,-2.69,-1.91,-.85,.12,1.34,2.31,3.48];
   for(const [j,center]of ridgeCenters.entries()){
    const axis=a-center-y*[.34,-.16,.22,.44,.10,-.22,.28,.40][j]-.12*geologyNoise(y*1.3,j+4),width=.095+.026*(j%3);
    const envelope=(.15+.85*Math.exp(-Math.pow((y-(.6+(j*1.13)%2.9))/(.65+.27*(j%3)),2)))*(.65+.35*T.MathUtils.smoothstep(geologyNoise(j*2,y*.65),-.8,.65));
    flakes+=(.12+.024*(j%3))*Math.exp(-Math.pow(axis/width,2))*envelope;
    flakes-=.048*Math.exp(-Math.pow((axis-width*1.6)/(width*1.3),2))*envelope;
   }
   const tilt=.70*T.MathUtils.smoothstep(y,0,4);
   const r=side==='west'?Math.max(.15,Math.min(1.08,westRelief+flakes+tilt)):
    Math.max(.13,Math.min(.72,plane-fissure+fluting+weathering+y*[.04,.03,.025][sideIndex]));
   return {point:side==='west'?[-4+r,y,a]:side==='east'?[4-r,y,a]:[a,y,-4+r],r};
  };
  for(let i=0;i<nx;i++)for(let j=0;j<ny;j++){
   const q=[surface(i/nx,j/ny),surface((i+1)/nx,j/ny),surface((i+1)/nx,(j+1)/ny),surface(i/nx,(j+1)/ny)];
   maxNeighbourStep=Math.max(maxNeighbourStep,Math.abs(q[0].r-q[1].r),Math.abs(q[0].r-q[3].r));
   for(const k of [0,1,3,1,2,3])vertices.push(...q[k].point);for(const v of q)reliefs.push(v.r);
   // Spatially local wall envelopes, rather than an AABB enclosing the room.
   if(i%8===0&&j%8===0){const b=new T.Box3();for(let ii=i;ii<=Math.min(i+8,nx);ii++)for(let jj=j;jj<=Math.min(j+8,ny);jj++)b.expandByPoint(new T.Vector3(...surface(ii/nx,jj/ny).point));wallBlocks.push({min:b.min.toArray(),max:b.max.toArray()});}
  }
  const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(vertices,3));g.userData={geological:true,westSurface:side==='west'};put(g,'walls');
  root.userData.geologicalWalls.push({side,erosionRidges:side==='west'?8:0,bottomInset:surface(.5,0).r,topInset:surface(.5,1).r,surfaceStyle:side==='west'?'continuous inclined erosion':'fractured bedrock',maxNeighbourStep,fractureDomains:domains.length,minRelief:Math.min(...reliefs),maxRelief:Math.max(...reliefs)});
 }
 // Front wall is split around a genuine 2m x3m opening, x=1..3.
 for(const [left,right,bottom,top] of [[-4,1,0,4],[3,4,0,4],[1,3,3,4]])triSurface(Math.ceil((right-left)*6),12,(u,v)=>[left+(right-left)*u,bottom+(top-bottom)*v,3.86-(bottom+(top-bottom)*v)*.025+rough(u*3,v*4)*.35],'front',colors.stone);
 triSurface(65,65,(u,v)=>{const x=u*8-4,z=v*8-4;return[x,3.84+rough(x,z)*.7,z]},'ceiling',0x49473d);
 // Broken strata and loose perimeter rubble sit within the module envelope.
 // Preserve the decoration random stream used by the already-approved assets.
 for(let i=0;i<195*7;i++)rand();
 for(let i=0;i<180;i++){let x=rand()*7.2-3.6,z=rand()*7.2-3.6;if(Math.abs(x)<2.7&&Math.abs(z)<2.7&&rand()<.85)continue;if(z>3.1&&x>.6&&x<3.3)continue;const s=.035+rand()*.12;rock([x,s*.48,z],[s,s*.5,s*.85],'floor',rand()>.4?0x595245:0x3d3b34,0);}
 // Irregular soil aprons climb the wall and feather into the floor.
 for(const side of ['west','east','north'])triSurface(75,14,(u,v)=>{const a=u*7.7-3.85,width=.55+.19*Math.sin(a*3.3)+.12*Math.sin(a*7.5),d=v*width,crest=.38+.12*Math.sin(a*2.9)+.06*Math.sin(a*8.2);const y=.025+crest*Math.pow(1-v,2.3)+.016*Math.sin(a*23+d*13)*Math.sin(Math.PI*v);return side==='west'?[-3.8+d,y,a]:side==='east'?[3.8-d,y,a]:[a,y,-3.8+d]},'wallFoot',colors.soil);
 for(let i=0;i<120;i++){let x=rand()*7-3.5,z=rand()*7-3.5;if(z>2.7&&x>.6&&x<3.35)continue;const edge=Math.min(4-Math.abs(x),z+4),sz=edge<1.3?.07+rand()*.17:.025+rand()*.085;rock([x,.045+sz*.22,z],[sz,sz*.46,sz*.82],'debris',colors.stone,1);}
 // Asymmetric layered rock buttress, continuous ground-to-roof loft.
 const crag=[];const n=70,layers=40;
 const point=(j,i)=>{const y=j/layers*4,t=i/n*Math.PI*2;
  const swell=T.MathUtils.smoothstep(y,.8,4);
  const rx=.46+1.3*swell,rz=.68+.69*swell,cx=3.27-.66*swell;
  const fault=.055*Math.sin(t*9+y*2.1)+.032*Math.sin(t*19-y*4.8)+.013*Math.sin(t*39+y*14);
  const strata=.023*Math.sin(y*18+t*2.4);
  return[Math.max(.64,Math.min(3.97,cx+Math.cos(t)*rx*(1+fault+strata))),y,-.1+Math.sin(t)*rz*(1+fault)];};
 for(let j=0;j<layers;j++)for(let i=0;i<n;i++){const a=point(j,i),b=point(j,i+1),c=point(j+1,i),d=point(j+1,i+1);crag.push(...a,...b,...c,...b,...d,...c)}
 for(const j of [0,layers])for(let i=0;i<n;i++)crag.push(3,j/layers*4,-.1,...point(j,i),...point(j,i+1));
 const cg=new T.BufferGeometry();cg.setAttribute('position',new T.Float32BufferAttribute(crag,3));put(cg,'crag',0x514f46);
 // Thin waisted mineral formations; tip shapes are modeled rather than tubes.
 function mineral(x,z,y0,y1,radii,cat){
  const lean=x<-2.5&&cat!=='ceiling'?(z>1?[.10,-.18]:z>0?[.18,.13]:[-.09,.21]):[0,0];
  if(cat==='columns'||cat==='formations'||(cat==='ceiling'&&y1<3.86)){
   const radius=Math.max(...radii,cat==='formations'?.35:0)*1.38+.035+Math.max(Math.abs(lean[0]),Math.abs(lean[1]));
   let envelope;
   for(let step=0;step<35;step++){
    envelope=new T.Box3(new T.Vector3(x-radius,cat==='ceiling'?y0:0,z-radius),new T.Vector3(x+radius,cat==='ceiling'?y1:4,z+radius));
    if(!wallBlocks.some(b=>envelope.intersectsBox(new T.Box3(new T.Vector3(...b.min),new T.Vector3(...b.max)).expandByScalar(.065))))break;
    if(Math.abs(x)>Math.abs(z))x-=Math.sign(x)*.04;else z-=Math.sign(z)*.04;
   }
   mineralClearance.push({min:envelope.min.toArray(),max:envelope.max.toArray()});
  }
  const large=cat==='columns'||cat==='formations',maxR=Math.max(...radii);
  const profile=large?Array.from({length:37},(_,i)=>{const u=i/36*(radii.length-1),j=Math.min(radii.length-2,Math.floor(u)),t=u-j;return T.MathUtils.lerp(radii[j],radii[j+1],t);}):radii;
  const points=profile.map((r,i)=>new T.Vector2(r,(y1-y0)*i/(profile.length-1))),g=new T.LatheGeometry(points,large?24:36),p=g.attributes.position;
  for(let i=0;i<p.count;i++){
   const yy=p.getY(i),xx=p.getX(i),zz=p.getZ(i),angle=Math.atan2(zz,xx),height=yy+y0;
   const strata=large?.10*geologyNoise(angle*3,height*17)+.12*geologyNoise(angle*5,height*5):0;
   const f=1+.065*Math.sin(angle*13+yy*.45)+.025*Math.cos(angle*27-yy*.8)+strata;
   const bend=large?.023*Math.sin(height*3+x):0;
   p.setXYZ(i,xx*f+x+lean[0]*height/4+bend,height,zz*f+z+lean[1]*height/4+(large?.025*Math.sin(height*2+z):0));
  }
  put(g,cat,0x756c55);
  if(large){
   // Satellite cones are rooted in the same broad foot/crown, with varying lengths.
   const crown=y1>3.9,foot=y0===0;
   for(const top of [false,true]){
    if(top&&!crown||!top&&!foot)continue;
    for(let k=0;k<3;k++){
     const angle=k*2.399+x*.7+z,offset=maxR*(.82+.08*k),r=maxR*(.29+.045*k),h=(top?.42:.37)+(.21+.08*k)*(1+.13*Math.sin(x+z+k));
     const cx=x+Math.cos(angle)*offset+(top?lean[0]:0),cz=z+Math.sin(angle)*offset+(top?lean[1]:0);
     const pts=Array.from({length:9},(_,j)=>{const t=j/8;return new T.Vector2(r*(.015+Math.pow(top?t:1-t,1.4)),h*t);});
     const sg=new T.LatheGeometry(pts,12),sp=sg.attributes.position;
     for(let j=0;j<sp.count;j++){const sy=sp.getY(j),sx=sp.getX(j),sz=sp.getZ(j),t=sy/h,wrinkle=1+.15*geologyNoise(sy*16,k*2)+.08*Math.sin(Math.atan2(sz,sx)*7+sy*9);sp.setXYZ(j,cx+sx*wrinkle+.035*Math.sin(angle)*(top?1-t:t),sy+(top?4-h:0),cz+sz*wrinkle+.045*Math.cos(angle)*(top?1-t:t));}
     put(sg,cat,0x756c55);root.userData.mineralSatelliteCount=(root.userData.mineralSatelliteCount??0)+1;
    }
   }
  }
  if(cat==='ceiling')ceilingTips.push({id:ceilingTips.length,start:[x,y0,z]});
 }
 root.userData.largeMineralGroups=[];
 let mineralSeed=8351;const mineralRandom=()=>{mineralSeed=(Math.imul(mineralSeed,1664525)+1013904223)>>>0;return mineralSeed/4294967296;};
 for(const [id,x,z]of [['west',-3.15,.65],['north',.65,-3.35]]){
  const radiusScale=(id==='west'?.65:1.15)+mineralRandom()*.15;
  root.userData.largeMineralGroups.push({id,radiusScale});mineral(x,z,0,4,[.34,.21,.12,.065,.043,.038,.055,.11,.22,.38].map(r=>r*radiusScale),'columns');
 }
 const radiusScale=.88+mineralRandom()*.15;root.userData.largeMineralGroups.push({id:'east',radiusScale});
 mineral(3.6,2.4,0,1.5,[.27,.2,.115,.05,.007].map(r=>r*radiusScale),'formations');mineral(3.6,2.4,1.66,3.98,[.008,.055,.13,.21,.33].map(r=>r*radiusScale),'formations');
 for(let i=0;i<48;i++){const x=rand()*5.7-2.85,z=rand()*5.7-2.85,h=.17+rand()*.55;mineral(x,z,3.82-h,3.85,[.003,.03,.05,.11],'ceiling');}
 root.userData.wallBlocks=wallBlocks;root.userData.mineralClearance=mineralClearance;root.userData.wallSoilFaces=wallSoilFaces;
 for(const [index,name]of [[0,'left column'],[1,'back column']]){const b=mineralClearance[index],c=colliders.find(c=>c.name===name);Object.assign(c,{minX:b.min[0],maxX:b.max[0],minZ:b.min[2],maxZ:b.max[2]});}
 const oldEdge=colliders.findIndex(c=>c.name==='edge formations');if(oldEdge>=0)colliders.splice(oldEdge,1);const edge=colliders.find(c=>c.name==='east formations'),e=mineralClearance[2];Object.assign(edge,{minX:e.min[0],maxX:e.max[0],minZ:e.min[2],maxZ:e.max[2]});
 // Bent timber with bark grooves, lashings, layered bundled thatch.
 function tube(points,r,cat,c,sides=7,steps=12){const curve=new T.CatmullRomCurve3(points.map(p=>new T.Vector3(...p)));put(new T.TubeGeometry(curve,steps,r,sides,false),cat,c);}
 function timber(a,b,r){
  const mid=a.map((v,i)=>(v+b[i])/2+(i===0?.025:i===2?.018:0));const curve=new T.CatmullRomCurve3([a,mid,b].map(p=>new T.Vector3(...p)));let g=new T.TubeGeometry(curve,16,r,16,false);const uv=g.attributes.uv,p=g.attributes.position,cs=[];
  for(let i=0;i<p.count;i++){const u=uv.getX(i),v=uv.getY(i);uv.setXY(i,v*1.3,u*curve.getLength()/.65);const shade=.48+.10*Math.sin(u*3.14);cs.push(shade,shade,shade);}
  g.setAttribute('color',new T.Float32BufferAttribute(cs,3));g=g.toNonIndexed();(bins.timber??=[]).push(g);
 }
 function tie(x,y,z){
  // A single uneven cord crosses itself around the joint, with loose knot loops.
  const pts=[],turns=5.3+rand()*1.6,phase=rand()*6.28;
  for(let j=0;j<=100;j++){const t=j/100,a=phase+t*turns*6.283,r=.083+.015*Math.sin(a*1.7)+.007*Math.cos(a*3);pts.push([x+Math.cos(a)*r,y+(t-.5)*.17+.043*Math.sin(a*.73),z+Math.sin(a)*r]);}
  tube(pts,.010,'lashings',0x927447,5,100);
  for(let k=0;k<2;k++){const d=k?1:-1;tube([[x+.07,y+.04,z+.045],[x+.15,y+.025*d,z+.1],[x+.10,y-.045,z+.14],[x+.055,y+.005,z+.09],[x+.11,y-.09,z+.13],[x+.08+d*.035,y-.21-rand()*.08,z+.1]],.012,'lashings',k?0x735637:0xa58a58,5,22);}
 }
 const x0=-3.35,x1=-1.3,back=-3.45,front=-2.0;
 root.userData.posts=[];let postIndex=0;
 for(const x of[x0,x1])for(const z of[back,front]){const h=z===back?2.65:1.35,offsets=[[-.16,.12],[.15,-.10],[.19,.08],[-.11,-.16]],off=offsets[postIndex++],foot=[x+off[0],.035,z+off[1]],top=[x+.035,h,z];if(z===front){const bearingHeight=x===x0?.6975:1.3175;foot[0]=top[0];foot[2]=z+(bearingHeight-.02)/.72/Math.tan(70*Math.PI/180);}const extra=z===back?(x===x0?.70:.77):0,tip=[top[0]+(top[0]-foot[0])*extra/h,h+extra,top[2]+(top[2]-foot[2])*extra/h];root.userData.posts.push({foot,top,tip});timber(foot,tip,.063);if(extra){const cap=new T.CircleGeometry(.062,12);cap.applyQuaternion(new T.Quaternion().setFromUnitVectors(new T.Vector3(0,0,1),new T.Vector3(...tip).sub(new T.Vector3(...foot)).normalize()));cap.translate(...tip);put(cap,'timber',0xb29b76);}tie(x+.035,h-.06,z);}
 for(const z of[back,front])timber([x0-.12,z===back?2.65:1.35,z],[x1+.12,z===back?2.65:1.35,z],.065);
 for(let i=0;i<9;i++){const x=x0+i*(x1-x0)/8;timber([x,2.65,back-.08],[x,1.30,front+.13],.025);}
 // Visible triangular side framing and rough boards, as in the concept.
 timber([x0,1.35,back],[x0,1.35,front],.055);
 timber([x0,1.35,back],[x0,2.65,back],.054);
 timber([x0,1.35,back+.15],[x0,1.98,back+.72],.041);
 tie(x0,1.35,back);tie(x0,1.35,front);
 root.userData.triangularBrace=true;root.userData.scrapPlanks=5;
 for(let i=0;i<5;i++){
  const h=[1.24,.92,1.42,1.05,1.32][i],width=[.14,.12,.16,.11,.15][i],z=back+.10+i*.15;
  const shape=new T.Shape();shape.moveTo(-width/2,0);shape.lineTo(-width*.57,h*.36);shape.lineTo(-width*.46,h);shape.lineTo(-width*.14,h*.94);shape.lineTo(width*.03,h*.74);shape.lineTo(width*.15,h*.98);shape.lineTo(width*.52,h*.89);shape.lineTo(width*.47,h*.44);shape.lineTo(width*.55,0);shape.closePath();
  let g=new T.ExtrudeGeometry(shape,{depth:.045,bevelEnabled:false,steps:1});const p=g.attributes.position,uv=g.attributes.uv,cs=[];
  for(let j=0;j<p.count;j++){uv.setXY(j,(p.getX(j)+.1)*5,p.getY(j)*1.6);cs.push(.44,.42,.38);}
  g.setAttribute('color',new T.Float32BufferAttribute(cs,3));g.rotateY(Math.PI/2);g.rotateX([-.08,.12,-.045,.08,-.11][i]);g.translate(x0-.08,0,z);if(g.index)g=g.toNonIndexed();(bins.timber??=[]).push(g);
 }
 // Overlapping local tufts, following the roof broadly without a shared focal point.
 for(let bundle=0;bundle<360;bundle++){
  const under=bundle>=280;
  const bx=x0-.12+rand()*(x1-x0+.24),bz=back-.12+rand()*1.62;
  const cross=rand()<.22,angle=-.25+.35*Math.sin(bx*4+bz*3)+(rand()-.5)*(cross?2.6:1.0),clump=.015+rand()*.10,bend=(rand()-.5)*.14;
  for(let k=0;k<8;k++){
   const x=bx+(k-3.5)*.014+(rand()-.5)*.02,z=bz+(rand()-.5)*.08,len=Math.min(.25+rand()*.34,(front+.19-z)/Math.max(.2,Math.cos(angle)));
   const dx=T.MathUtils.clamp(x+Math.sin(angle+(rand()-.5)*.25)*len,x0-.14,x1+.14)-x,dz=Math.cos(angle)*len;
   const y=(under?2.75:2.95)-(z-back)*.77+clump+rand()*.025,w=.009+rand()*.009,verts=[];
   const point=(t,side)=>{const zz=z+dz*t,hang=Math.max(0,zz-(front-.06));return[x+dx*t+bend*Math.sin(t*Math.PI)+side*w*(1-.87*t),y-.77*dz*t+.045*Math.sin(t*Math.PI)-.075*t*t-hang*1.05,zz];};
   for(let j=0;j<4;j++){const t=j/4,u=(j+1)/4;verts.push(...point(t,-1),...point(t,1),...point(u,-1),...point(t,1),...point(u,1),...point(u,-1));}
   const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(verts,3));put(g,'shelter',[0x4b371f,0x624a27,0x3b2f20,0x796035][Math.floor(rand()*4)]);
  }
 }
 // Southern eave: fibers bend across the side edge as well as the east edge.
 root.userData.frontEave=[];
 for(let bundle=0;bundle<48;bundle++){
  const bx=x0+.23+rand()*.12,bz=back-.05+(front-back+.12)*(bundle+rand()*.65)/48,drop=.065+rand()*.035;
  for(let k=0;k<8;k++){
   const x=bx+(rand()-.5)*.025,z=bz+(rand()-.5)*.11,y=2.89-(z-back)*.77+rand()*.08,len=.28+rand()*.10,w=.009+rand()*.008,sideways=(rand()-.5)*.34,curl=(rand()-.5)*.13,sag=drop+.005+rand()*.025;
   const pt=(t,side)=>[x-len*t,y+(.08+curl*.25)*Math.sin(t*Math.PI)-sag*Math.pow(Math.max(0,(t-.25)/.75),2),z+sideways*t+curl*Math.sin(t*Math.PI)+side*w*(1-.72*t)];
   const v=[];for(let j=0;j<4;j++){const t=j/4,u=(j+1)/4;v.push(...pt(t,-1),...pt(t,1),...pt(u,-1),...pt(t,1),...pt(u,1),...pt(u,-1));}
   const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(v,3));put(g,'shelter',[0x4b371f,0x624a27,0x796035][Math.floor(rand()*3)]);
   if(k===3)root.userData.frontEave.push({root:pt(0,0),tip:pt(1,0)});
  }
 }
 // Dense east eave tufts overlap the front fringe at the low corner.
 for(let b=0;b<36;b++){
  const bx=x0-.06+(x1-x0+.12)*(b+rand()*.7)/36;
  for(let k=0;k<8;k++){
   const x=bx+(rand()-.5)*.08,z=front-.33+rand()*.12,y=2.90-(z-back)*.77+rand()*.07,len=.46+rand()*.12,sag=.085+rand()*.035,drift=(rand()-.5)*.22,w=.011+rand()*.008;
   const pt=(t,side)=>[x+drift*t+.025*Math.sin(t*4)+side*w*(1-.72*t),y-.77*len*t+.075*Math.sin(t*Math.PI)-sag*Math.pow(Math.max(0,(t-.25)/.75),2),z+len*t];
   const v=[];for(let j=0;j<4;j++){const t=j/4,u=(j+1)/4;v.push(...pt(t,-1),...pt(t,1),...pt(u,-1),...pt(t,1),...pt(u,1),...pt(u,-1));}
   const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(v,3));put(g,'shelter',[0x4b371f,0x624a27,0x796035][Math.floor(rand()*3)]);
  }
 }
 // Rotate the shelter only: its opening now faces east, away from the west wall.
 const hutTransform=new T.Matrix4().makeTranslation(-2.325,0,-2.525).multiply(new T.Matrix4().makeRotationY(Math.PI/2)).multiply(new T.Matrix4().makeTranslation(2.325,0,2.725));
 const goblinScale=new T.Matrix4().makeTranslation(-2.325,0,-2.525).multiply(new T.Matrix4().makeScale(.72,.72,.72)).multiply(new T.Matrix4().makeTranslation(2.325,0,2.525));
 const finalHutTransform=goblinScale.clone().multiply(hutTransform);
 // Four independently specified corners replace the former uniform roof plane.
 const warpHeight=(x,y,z)=>{const u=(x-x0-.035)/(x1-x0),v=(z-back)/(front-back),old=2.65-1.30*v,newHeight=(1.55/.72)*(.9+.1*u+v*(-.45+.30*u))-.04*Math.sin(Math.PI*u)*Math.sin(Math.PI*v);return y<=old?y/old*newHeight:y-old+newHeight;};
 for(const category of ['shelter','lashings','timber'])for(const g of bins[category]??[]){const p=g.attributes.position;for(let i=0;i<p.count;i++)p.setY(i,warpHeight(p.getX(i),p.getY(i),p.getZ(i)));g.computeVertexNormals();}
 for(const p of root.userData.posts)for(const key of ['foot','top','tip'])p[key][1]=warpHeight(...p[key]);
 for(const p of root.userData.frontEave)for(const key of ['root','tip']){p[key][1]=warpHeight(...p[key]);p[key]=new T.Vector3(...p[key]).applyMatrix4(finalHutTransform).toArray();}

 for(const category of ['shelter','lashings','timber'])for(const g of bins[category]??[])g.applyMatrix4(finalHutTransform);
 for(const p of root.userData.posts)for(const key of ['foot','top','tip'])p[key]=new T.Vector3(...p[key]).applyMatrix4(finalHutTransform).toArray();
 root.userData.shelterScale=.72;
 root.userData.shelterFacing=[1,0,0];root.userData.shelterRoofForm='four unequal corners';
 root.userData.drips=[
  {start:[-2.75,3.43,-2.6],end:[-2.75,2.68,-2.6],surface:'roof'},
  {start:[-2.30,3.51,-3.05],end:[-2.30,2.35,-3.05],surface:'roof'},
  {start:[-1.702,1.1376,-2.363],end:[-1.702,.065,-2.363],surface:'ground'},
  {start:[2.75,3.40,2.6],end:[2.75,.07,2.6],surface:'ground'}
 ];
 for(const d of root.userData.drips.filter(d=>d.surface==='roof'))mineral(d.start[0],d.start[2],d.start[1],3.86,[.005,.032,.065,.12],'ceiling');
 // Damp ground below the eave, outside the sleeping area.
 for(const z of [-2.363,-2.69]){const g=new T.CircleGeometry(.19,20);g.rotateX(-Math.PI/2);g.scale(.55,1,1);g.translate(-1.70,.055,z);put(g,'water',0x30352a);}
 // Rumpled thick fur bedding, with asymmetric folded shoulders and long tufts.
 const hidePoint=(t,a)=>{
  const limb=.15*Math.exp(-Math.pow(Math.sin(a*2+.3)/.27,2)),r=1+limb+.045*Math.sin(a*9),xx=Math.cos(a)*.53*r*t,zz=Math.sin(a)*.70*r*t;
  const fold=.23*Math.exp(-Math.pow((xx+.19+zz*.35)/.105,2))*(.4+.6*Math.pow(Math.cos(zz*2),2));
  const edge=.11*Math.pow(t,5)*(1+.65*Math.sin(a*3+.7));
  const wrinkle=.032*Math.sin(xx*23+zz*8)*Math.sin(t*Math.PI);
  return[-2.32+xx,.09+fold+edge+wrinkle,-2.53+zz];
 };
 const hideVerts=[];for(let j=0;j<11;j++)for(let i=0;i<64;i++){const a=i/64*6.283,b=(i+1)/64*6.283,t=j/11,u=(j+1)/11;hideVerts.push(...hidePoint(t,a),...hidePoint(u,a),...hidePoint(u,b),...hidePoint(t,a),...hidePoint(u,b),...hidePoint(t,b));}
 // A rolled leather edge gives the pelt real thickness.
 for(let i=0;i<64;i++){const a=hidePoint(1,i/64*6.283),b=hidePoint(1,(i+1)/64*6.283),c=[a[0],a[1]-.035,a[2]],d=[b[0],b[1]-.035,b[2]];hideVerts.push(...a,...b,...c,...b,...d,...c);}
 const hg=new T.BufferGeometry();hg.setAttribute('position',new T.Float32BufferAttribute(hideVerts,3));put(hg,'hides',0x63513b);
 for(let i=0;i<180;i++){
  const a=rand()*6.283,t=Math.sqrt(rand())*.98,p=hidePoint(t,a),len=.045+rand()*.065,w=.0015+rand()*.0025,drift=(rand()-.5)*.05;
  const tip=[p[0]+drift,p[1]+.008,p[2]+len],mid=[p[0]+drift*.4,p[1]+.009+rand()*.009,p[2]+len*.5];
  const v=[p[0]-w,p[1]+.003,p[2],p[0]+w,p[1]+.003,p[2],...mid,p[0]-w,p[1]+.003,p[2],...mid,...tip];
  const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(v,3));put(g,'hides',[0x4f3b28,0x7d684b,0x9b8460,0x67533b][Math.floor(rand()*4)]);
 }
 // A loosely folded second pelt overlaps the rear of the sleeping nest.
 triSurface(14,16,(u,v)=>{const x=-2.83+u*.52,z=-2.99+v*.56+.025*Math.sin(u*25);return[x,.13+.15*Math.sin(u*Math.PI)+.095*Math.exp(-Math.pow((v-.45)/.15,2))+.022*Math.sin(u*19+v*8),z]},'hides',0x8c7d68);
 // Actual Artec 3D goat scan, decimated with source vertex colors preserved.
 function skull(x,y,z,size,stake=false){
  let g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(skullScan.positions,3));g.setIndex(skullScan.indices);g.computeVertexNormals();
  const colors=[];for(let i=0;i<skullScan.colors.length;i+=3){const luminance=(.2126*skullScan.colors[i]+.7152*skullScan.colors[i+1]+.0722*skullScan.colors[i+2])/255;const c=new T.Color(palette.agedBone).multiplyScalar(.16+1.5*Math.pow(luminance,2));colors.push(c.r,c.g,c.b);}
  g.setAttribute('color',new T.Float32BufferAttribute(colors,3));g.setAttribute('uv',new T.Float32BufferAttribute(new Float32Array(skullScan.positions.length/3*2),2));
  g.scale(size,size,size);if(!stake)g.rotateX(-Math.PI/2);else g.rotateZ(-.12);g.translate(x,y,z);g=g.toNonIndexed();(bins.skull??=[]).push(g);
  if(stake){timber([x,0,z-.06],[x,y+.09,z-.06],.048);tie(x,y-.55,z-.06);}
 }
 skull(-.88,1.22,-2.64,.86,true);skull(-1.65,.18,-1.63,.46);
 // Real scan fragments, each settled on the floor after independent rotation.
 root.userData.boneSource=boneScans.credit;root.userData.bonePlacements=[];
 const fragments=[
 ['ribs',-2.55,-.7,.36,.8,.28],['pelvis',-1.38,-1.98,.25,-.9,.2],
 ['femur',-2.2,-1.12,.38,1.3,.15],['humerus',-2.46,-1.19,.29,-.6,-.18],
 ['femur',-1.89,-1.42,.31,-1.1,-.2],['humerus',-1.18,-1.29,.27,2.1,.35],
 ['femur',-2.93,-1.05,.26,.45,.4],['humerus',-1.53,-.94,.25,-2.2,-.25],
 ['pelvis',-2.86,-2.1,.19,2.5,.3],['femur',-1.32,-1.58,.22,.2,-.45]
 ];
 for(const [kind,x,z,size,yaw,roll] of fragments){
  const scan=boneScans[kind];let g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(scan.positions,3));g.setIndex(scan.indices);g.computeVertexNormals();
  g.scale(size,size,size);g.rotateZ(roll);g.rotateY(yaw);g.computeBoundingBox();
  const bottom=.047,height=g.boundingBox.max.y-g.boundingBox.min.y;
  g.translate(x,bottom-g.boundingBox.min.y,z);
  const c=[],p=g.attributes.position;
  for(let i=0;i<p.count;i++){
   const px=p.getX(i),py=p.getY(i),pz=p.getZ(i);
   const dirt=Math.min(1,(py-bottom)/.085),mottling=.5+.5*Math.sin(px*65+pz*41)*Math.sin(pz*87-py*73);
   const shade=.40+.14*dirt+.06*mottling;
   const pigment=new T.Color(palette.agedBone).convertLinearToSRGB();const color=new T.Color().setRGB(shade,pigment.g/pigment.r*shade,pigment.b/pigment.r*shade,T.SRGBColorSpace);c.push(color.r,color.g,color.b);
  }
  g.setAttribute('color',new T.Float32BufferAttribute(c,3));g.setAttribute('uv',new T.Float32BufferAttribute(new Float32Array(p.count*2),2));
  g=g.toNonIndexed();(bins.bones??=[]).push(g);root.userData.bonePlacements.push({kind,x,z,size,yaw,bottom,height});
 }
 // Two independently seeded fallen rocks, clear of the approach route.
 const savedRockSeed=seed;root.userData.fallenBoulders=[];
 for(const [x,z,sx,sy,sz]of [[-3.0,2.72,.61,.44,.46],[3.04,-2.5,.53,.37,.48]]){
  const start=bins.debris.length;rock([x,sy*.72,z],[sx,sy,sz],'debris',colors.stone,2);
  const b=new T.Box3().setFromBufferAttribute(bins.debris[start].attributes.position),c={name:'fallen rock '+root.userData.fallenBoulders.length,minX:b.min.x,maxX:b.max.x,minZ:b.min.z,maxZ:b.max.z};
  root.userData.fallenBoulders.push(c);
 }
 seed=savedRockSeed;
 // Rebuild mutable extra envelopes on every deterministic generation.
 for(let i=colliders.length-1;i>=0;i--)if(colliders[i].name.startsWith('fallen rock '))colliders.splice(i,1);
 colliders.push(...root.userData.fallenBoulders);
 root.userData.ceilingTips=ceilingTips;
 const selected=[...ceilingTips.slice(48),...ceilingTips.slice(0,48).sort((a,b)=>((Math.imul(a.id+7,1664525)^9618)>>>0)%997-((Math.imul(b.id+7,1664525)^9618)>>>0)%997).slice(0,Math.round(ceilingTips.length*.2)-2)];
 root.userData.drips=root.userData.drips.filter(d=>d.surface==='ground').slice(0,1);
 for(const [i,tip]of selected.entries())root.userData.drips.push({ceilingTipId:tip.id,start:tip.start,end:[tip.start[0],.04,tip.start[2]],surface:tip.id>=48?'roof':'ground',period:1.05+(i*137%290)/100,phase:(i*.618)%1});
 // Fine web strands as actual slender mesh geometry for GLB portability.
 for(const [cx,cz,dir]of[[-3.62,-3.55,1],[3.6,-3.54,-1]]){const center=[cx,3.08,cz];for(let i=0;i<9;i++){const a=i/8*Math.PI/2;rod(center,[cx+dir*Math.sin(a)*1.05,3.78,cz+Math.cos(a)*.28],.0022,'webs',colors.web,.0022,3);}for(let k=1;k<=5;k++){let prev;for(let i=0;i<=12;i++){const a=i/12*Math.PI/2,pt=[cx+dir*Math.sin(a)*1.05*k/5,3.08+.7*k/5,cz+Math.cos(a)*.28*k/5];if(prev)rod(prev,pt,.0015,'webs',colors.web,.0015,3);prev=pt;}}}
 const puddle=new T.CircleGeometry(.43,32);puddle.rotateX(-Math.PI/2);puddle.scale(1,.7,1);puddle.translate(2.75,.047,2.6);put(puddle,'water',colors.water);
 for(const [cat,geos]of Object.entries(bins)){const merged=mergeGeometries(geos,false);const g=mergeVertices(merged,0.0001);merged.dispose();const mat=new T.MeshStandardMaterial({vertexColors:true,roughness:cat==='water'?.22:.93,metalness:0,side:T.DoubleSide});if(cat==='timber')mat.color.set(palette.darkWood);mat.name=cat+'_material';const mesh=new T.Mesh(g,mat);mesh.name=cat;mesh.castShadow=cat!=='webs'&&cat!=='water';mesh.receiveShadow=true;root.add(mesh);for(const a of geos)a.dispose();}
 root.updateMatrixWorld(true);for(const d of root.userData.drips){if(d.ceilingTipId===undefined)continue;const ray=new T.Raycaster(new T.Vector3(...d.start).add(new T.Vector3(0,-.015,0)),new T.Vector3(0,-1,0));const targets=root.children.filter(o=>o.isMesh&&!['ceiling','water','webs'].includes(o.name));const hit=ray.intersectObjects(targets,false)[0];if(!hit)throw new Error('Ceiling drip must hit a solid surface');d.end=hit.point.toArray();d.surface=hit.object.name==='shelter'?'roof':hit.object.name;}
 root.userData.dripPools=[];
 const water=root.getObjectByName('water'),poolParts=[water.geometry.toNonIndexed()];
 const poolTargets=root.children.filter(o=>o.isMesh&&!['water','ceiling','webs'].includes(o.name));
 for(const [dripIndex,d]of root.userData.drips.entries()){
  if(!['floor','wallFoot'].includes(d.surface))continue;
  const [x,y,z]=d.end,radius=.20+.09*((dripIndex*17)%7)/6,points=[],uv=[],color=[];
  const sample=(px,pz)=>{const ray=new T.Raycaster(new T.Vector3(px,y+.65,pz),new T.Vector3(0,-1,0));const hit=ray.intersectObjects(poolTargets,false)[0];return hit&&['floor','wallFoot'].includes(hit.object.name)?hit.point.add(new T.Vector3(0,.009,0)):null;};
  const center=sample(x,z);if(!center)continue;
  const edge=t=>{const a=t*Math.PI*2,r=radius*(1+.16*Math.sin(a*3+dripIndex)+.09*Math.sin(a*7));return sample(x+Math.cos(a)*r,z+Math.sin(a)*r*.76);};
  for(let j=0;j<32;j++){const a=edge(j/32),b=edge((j+1)/32);if(!a||!b)continue;for(const q of [center,b,a]){points.push(...q.toArray());uv.push(q.x,q.z);const c=new T.Color(0x292922);color.push(c.r,c.g,c.b);}}
  if(!points.length)continue;
  const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(points,3));g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));g.setAttribute('color',new T.Float32BufferAttribute(color,3));g.computeVertexNormals();poolParts.push(g);root.userData.dripPools.push({dripIndex,center:center.toArray(),radius});
 }
 water.material.color.set(0x89735e);water.material.transparent=true;water.material.opacity=.68;water.material.depthWrite=false;water.material.roughness=.16;
 const mergedPools=mergeGeometries(poolParts,false);water.geometry.dispose();water.geometry=mergeVertices(mergedPools,.0001);mergedPools.dispose();for(const g of poolParts)g.dispose();
 return root;
}
