import * as T from 'three';
import {refineStone} from './fractured-stone.mjs';
import {solidifyRock,relaxRock} from './rock-volume.mjs';
import {ConvexGeometry} from 'three/addons/geometries/ConvexGeometry.js';
import {mergeGeometries,mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';
import identity from '../identity.json' with {type:'json'};
import palette from './palette.json' with {type:'json'};
import rockScan from './rock-scan.json' with {type:'json'};
import bones from './bone-scans.json' with {type:'json'};
const {sin,cos,PI,abs,pow,max,min,exp}=Math,lerp=T.MathUtils.lerp,smooth=T.MathUtils.smoothstep;
function random(seed){return()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296;};}
function noise(x,y){const i=Math.floor(x),j=Math.floor(y),a=x-i,b=y-j,u=a*a*(3-2*a),v=b*b*(3-2*b);const h=(x,y)=>{let k=Math.imul(x,374761393)^Math.imul(y,668265263);k=Math.imul(k^(k>>>13),1274126177);return ((k^(k>>>16))>>>0)/4294967295*2-1;};return lerp(lerp(h(i,j),h(i+1,j),u),lerp(h(i,j+1),h(i+1,j+1),u),v);}
export {waterState} from './water.mjs';
export function buildRoom({closedWalls=false}={}){
 const root=new T.Group();root.name='straight_wet_karst';root.userData={roomIdentity:identity,metresPerUnit:1,north:'+Z',tips:[],drips:[],pools:[],source:'concept-v02',boneSource:bones.credit};root.userData.previewWallMode=closedWalls?'closed':'example-doors';const bins={};
 const pools=[{x:2.55,z:-2.45,rx:.91,rz:.77,y:.125},{x:-2.65,z:-1.7,rx:.45,rz:.31,y:.143},{x:2.55,z:2.5,rx:.4,rz:.35,y:.14}];
 const poolR=(x,z,p)=>{const dx=(x-p.x)/p.rx,dz=(z-p.z)/p.rz,a=Math.atan2(dz,dx);return Math.hypot(dx,dz)/(1+.065*sin(a*3+.6)+.038*sin(a*7));};
 function ground(x,z){let y=.17+.018*noise(x*3,z*3)+.01*noise(x*14,z*14);y+=.25*exp(-pow((z+1.82)/.34,2))*smooth(x,-2.5,-2.25)*(1-smooth(x,-1.12,-.98));for(const p of pools){const r=poolR(x,z,p);if(r<1.28)y=lerp(.025+.01*noise(x*5,z*5),y,smooth(r,.68,1.25));}return y*smooth(4-abs(z),0,.5);}
 function meshGeo(points){const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(points,3));return g;}
 function put(g,cat,tint=0xffffff){
  if(g.index)g=g.toNonIndexed();const smoothStone=cat==='gravel'&&g.hasAttribute('uv');if(!smoothStone)g.deleteAttribute('normal');const src=g;if(cat!=='gravel'){g=mergeVertices(g,.00002);src.dispose();}if(!smoothStone)g.computeVertexNormals();if(g.index)g=g.toNonIndexed();const p=g.attributes.position,n=g.attributes.normal,originalUV=g.attributes.uv,colors=[],uv=[],base=new T.Color(tint);if(['ceiling','roofRim'].includes(cat))for(let i=0;i<p.count;i++){const x=p.getX(i),z=p.getZ(i),d=.002,v=new T.Vector3((vault(x+d,z)-vault(x-d,z))/(2*d),-1,(vault(x,z+d)-vault(x,z-d))/(2*d)).normalize();n.setXYZ(i,v.x,v.y,v.z);}
  for(let i=0;i<p.count;i++){const x=p.getX(i),y=p.getY(i),z=p.getZ(i),ny=n.getY(i);let c=base.clone();
   if(!['bones','webs','water'].includes(cat)){const mineral=['columns','ceilingTips','deposits'].includes(cat),phase=noise(x*1.3+z*1.8,y*.4),grain=noise(x*11+z*7,y*9);c.set(mineral?0xc0ad87:cat==='floor'?0xc8b9a0:cat==='gravel'?0x9e9584:0xc1b7a2);if(cat!=='floor')c.lerp(new T.Color(0x786347),.14+.12*phase+.17*pow(max(0,ny),2)+.2*(1-smooth(y,0,.65)));c.multiplyScalar(.86+.08*phase+.035*grain);}
   colors.push(c.r,c.g,c.b);const k=Math.floor(i/3)*3,nx=abs(n.getX(k)+n.getX(k+1)+n.getX(k+2)),faceNY=abs(n.getY(k)+n.getY(k+1)+n.getY(k+2)),nz=abs(n.getZ(k)+n.getZ(k+1)+n.getZ(k+2));const orient=['west','east'].includes(cat)?'x':['north','south'].includes(cat)?'z':faceNY>max(nx,nz)?'y':nx>nz?'x':'z';uv.push(...(originalUV?[originalUV.getX(i),originalUV.getY(i)]:orient==='x'?[z/1.8,y/1.8]:orient==='z'?[x/1.8,y/1.8]:[x/1.5,z/1.5]));
  }g.setAttribute('color',new T.Float32BufferAttribute(colors,3));g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));(bins[cat]??=[]).push(g);
 }
 function surface(nx,ny,fn,cat,uvfn){const v=[],uv=[];for(let i=0;i<nx;i++)for(let j=0;j<ny;j++){const a=fn(i/nx,j/ny),b=fn((i+1)/nx,j/ny),c=fn((i+1)/nx,(j+1)/ny),d=fn(i/nx,(j+1)/ny);v.push(...a,...b,...d,...b,...c,...d);if(uvfn)for(const [u,v]of[[i/nx,j/ny],[(i+1)/nx,j/ny],[i/nx,(j+1)/ny],[(i+1)/nx,j/ny],[(i+1)/nx,(j+1)/ny],[i/nx,(j+1)/ny]])uv.push(...uvfn(u,v));}const g=meshGeo(v);if(uvfn)g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));put(g,cat);}

 function relief(a,y,side){const axis=a-.18*y;let r=.26+.4*smooth(y,.1,4)+.17*noise(axis*.8,y*.7+side)+.11*noise(axis*3,y*2)+.04*noise(axis*12,y*8);for(let i=0;i<10;i++){const center=-3.7+i*.81+.18*sin(i*7+side),q=a-center-y*(.08+.10*sin(i*3+side));r+=(.08+.08*(.5+.5*sin(i*7.3)))*exp(-pow(q/(.08+.04*(i%3)),2))*(.35+.65*smooth(y,.1,3.8));}return r;}
 for(const side of ['west','east'])surface(side==='west'?30:100,side==='west'?16:48,(u,v)=>{const a=u*8-4,y=v*4,r=relief(a,y,side==='west'?3:7);return[side==='west'?-4+r:4-r,y,a]},side);
 for(const side of ['north','south'])for(const [l,r,b,t]of(closedWalls?[[-4,4,0,4]]:[[-4,-1,0,4],[1,4,0,4],[-1,1,3,4]]))surface(Math.ceil((r-l)*(closedWalls?17:19)),Math.ceil((t-b)*(closedWalls?18:20)),(u,v)=>{const x=lerp(l,r,u),y=lerp(b===3?3+.25*pow(cos(x*PI/2),2):b,t,v),d=relief(x,y,9)*(closedWalls?1:smooth(abs(x),1,1.3));return[x,y,side==='north'?4-d:-4+d]},side);
 // A continuous cave vault, split only for the cutaway visibility control.
 const vault=(x,z)=>3.89-.70*pow(smooth(abs(x),1.55,4),1.45)-.26*smooth(abs(z),2.35,4)+.045*noise(x*1.4+z*.18,z*1.3)+.019*noise(x*5,z*4);
 for(const [l,r,cat]of[[-4,-2.4,'roofRim'],[-2.4,2.4,'ceiling'],[2.4,4,'roofRim']])surface(Math.ceil((r-l)*20),96,(u,v)=>{const x=lerp(l,r,u),z=v*8-4;return[x,vault(x,z),z]},cat,(u,v)=>[lerp(l,r,u)/1.5,(v*8-4)/1.5]);
 // Curving mother rock: the northern tail returns into the western wall.
 const scanGeo=new T.BufferGeometry();scanGeo.setAttribute('position',new T.Float32BufferAttribute(rockScan.positions,3));scanGeo.setAttribute('uv',new T.Float32BufferAttribute(rockScan.uv,2));scanGeo.setIndex(rockScan.indices);const sp=scanGeo.attributes.position,sc=[];
 for(let i=0;i<sp.count;i++){const ox=sp.getX(i),oy=sp.getY(i),oz=sp.getZ(i),y=(oy+.034359213)/3.564049*4,z=.2+(ox-.343154)/4.953511*5.7;
 // Plan curvature closes toward the north rear; vertical curvature rounds the shoulder.
 const depth=(oz+3.466048)/3.827303;
 const rearCurl=(1.70+.24*smooth(y,.8,1.8))*pow(smooth(z,-2.4,2.7),2.15);
 const x=max(-3.96,min(-1.83,-1.92-1.43*pow(y/4,1.65)-rearCurl*(1-.58*pow(y/4,2))+(depth-.5)*.65+.12*sin(y*1.6+z*.55)*sin(PI*y/4)));
 const crown=2.12+1.88*smooth(z,-2.7,2.2);
 sp.setXYZ(i,max(-3.98,x-.28*(1-smooth(z,1,2.6))),max(0,min(4,y*crown/4+1.0*smooth(z,1.7,2.5)*smooth(y,2.5,3.4))),z+.24*(1-smooth(z,-2.7,-.8)));const c=new T.Color(0xb8aa8b).multiplyScalar(.8+.13*smooth(y,0,2));sc.push(c.r,c.g,c.b);}scanGeo.setAttribute('color',new T.Float32BufferAttribute(sc,3));scanGeo.computeVertexNormals();const solid=relaxRock(solidifyRock(scanGeo));solid.applyMatrix4(new T.Matrix4().makeScale(1,1,4.75/5.86));solid.translate(0,0,3.13*(1-4.75/5.86));const foot=solid.attributes.position;for(let i=0;i<foot.count;i++){const y=foot.getY(i);if(y<.4)foot.setY(i,max(0,y-.12*(1-smooth(y,0,.4))));}bins.buttressScan=[solid];scanGeo.dispose();
 for(const side of [-1,1]){const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(rockScan.positions,3));g.setAttribute('uv',new T.Float32BufferAttribute(rockScan.uv,2));g.setIndex(rockScan.indices);const p=g.attributes.position,c=[];for(let i=0;i<p.count;i++){const ox=p.getX(i),oy=p.getY(i),oz=p.getZ(i),y=(oy+.034359213)/3.564049*4;
 const edge=smooth(abs((ox-.343154)/4.953511*7.7),3.25,3.85);p.setXYZ(i,side*(3.99-(oz+3.466048)/3.827303*.87*(1-edge)-.16*smooth(y,0,4)*(1-edge)),max(0,min(4,y)),(ox-.343154)/4.953511*7.7);const tint=new T.Color(0xb9ac94).multiplyScalar(.85+.13*smooth(y,0,2));c.push(tint.r,tint.g,tint.b);}g.setAttribute('color',new T.Float32BufferAttribute(c,3));g.computeVertexNormals();(bins.scannedWalls??=[]).push(side===-1?solidifyRock(g,{backX:-4,zMin:-4,zMax:4,segments:1}):g);}
 function mineral(x,z,y0,y1,profile,cat,salt,lean=[0,0]){surface(cat==='columns'?28:16,cat==='columns'?40:16,(u,v)=>{const a=u*PI*2,h=lerp(y0,y1,v),q=v*(profile.length-1),j=min(profile.length-2,Math.floor(q)),r=lerp(profile[j],profile[j+1],q-j),rib=1+.11*sin(a*9+salt+h*.8)+.07*noise(a*5+salt,h*14)+.055*noise(a*11,h*4);return[x+cos(a)*r*rib+lean[0]*v+.012*sin(h*5+salt)*sin(PI*v),h,z+sin(a)*r*rib+lean[1]*v+.018*sin(h*3)*sin(PI*v)];},cat,(u,v)=>[u*max(...profile)*2*PI/1.8,lerp(y0,y1,v)/1.8]);}
 bins.west=bins.west.map(g=>solidifyRock(g,{backX:-4,zMin:-4,zMax:4,segments:1}));
 const rng=random(31003);
 // Keep the full mineral envelope clear of the shortened front face.
 root.userData.footStalagmites=[];
 for(const [k,[x,z]]of [[-2.02,-2.23],[-1.74,-2.64],[-1.34,-2.13]].entries()){
  const base=ground(x,z),h=[.32,.17,.44][k];mineral(x,z,base,base+h,[.105,.078,.041,.016,.002],'deposits',k+90,[.025*sin(k*3),.013]);mineral(x,z,max(0,base-.035),base+.075,[.16,.13,.08,.025],'deposits',k+131,[.009,-.008]);root.userData.footStalagmites.push({x,z,height:h});
 }


 for(const [x,z,r,s]of[[-2.98,-1.12,.95,2],[3.06,-.86,1.16,5]]){if(s!==2)mineral(x,z,.08,3.73,[.36,.23,.14,.08,.041,.034,.065,.14,.27,.38].map(n=>n*r),'columns',s,[.09*(s-3),-.1]);for(let k=0;k<6;k++){const a=k*2.399,dist=.27+.08*(k%3),cx=x+cos(a)*dist,cz=z+sin(a)*dist,h=.28+rng()*.68;if(s!==2)mineral(cx,cz,ground(cx,cz),h,[.11+.02*(k%2),.08,.027,.002],'deposits',k);if(s!==2)mineral(cx,cz,3.73-h,3.73,[.002,.027,.07,.14],'deposits',k+8);}}
 for(let i=0;i<48;i++){const side=i%2?-1:1,z=-3.55+rng()*7.1,x=side*(3.03+rng()*.34);if(pools.some(p=>poolR(x,z,p)<1.35))continue;const h=.25+rng()*.61;if(x<0&&z> -3.1&&z<3.2)continue;mineral(x,z,ground(x,z),h,[.13,.08,.04,.002],'deposits',i);}
 const tips=root.userData.tips;
 for(let i=0;i<50;i++){let x,z,h;if(i<2){x=pools[0].x+(i?-.23:.23);z=pools[0].z+(i?.17:-.13);h=i?.95:1.18;}else if(i<10){const q=[[2.5,2.8],[-2.45,-3.1],[2.5,1.8],[-2.7,-3.3],[2.65,.7],[-2.4,3.25],[2.4,-.8],[2.5,-1.35]][i-2];[x,z]=q;h=.4+.15*(i%3);}else{x=(i%2?-1:1)*(2.86+rng()*.34);z=-3.45+rng()*6.9;h=.18+rng()*.64;}const tip=[x,3.73-h,z],top=max(3.73,vault(x,z)+.065);mineral(x,z,tip[1],top,[.002,.024,.065,.11+.055*rng()],'ceilingTips',i);tips.push({id:i,point:tip});}
 const selected=tips.slice(0,10);
 for(const [i,t]of selected.entries()){const [x,,z]=t.point;let p=pools.find(p=>poolR(x,z,p)<.82);if(!p){p={x,z,rx:.2+.025*(i%3),rz:.18,y:ground(x,z)+.012};pools.push(p);}root.userData.drips.push({tipId:t.id,start:t.point,end:[x,p.y,z],period:1.05+(i*1.37)%2.7,phase:(i*.618)%1});}
 for(const p of pools){const v=[],point=a=>{const r=1+.065*sin(a*3+.6)+.038*sin(a*7);return[p.x+cos(a)*p.rx*r,p.y,p.z+sin(a)*p.rz*r];};for(let i=0;i<96;i++)v.push(p.x,p.y,p.z,...point((i+1)/96*PI*2),...point(i/96*PI*2));put(meshGeo(v),'water',0x1c231f);}root.userData.pools=pools;surface(100,100,(u,v)=>{const x=u*8-4,z=v*8-4;return[x,ground(x,z),z]},'floor');
 const rubble=random(88231);
 function stone(x,z,sx,sy,sz,salt){const g=new T.IcosahedronGeometry(1,0),p=g.attributes.position;for(let i=0;i<p.count;i++){const x=p.getX(i),y=p.getY(i),z=p.getZ(i),k=1+.14*noise(x*3+salt,z*4+y);p.setXYZ(i,(x+.21*y)*k,min(.62,y)*k,z*k);}g.scale(sx,sy,sz);g.rotateY(salt);g.translate(x,ground(x,z)+sy*.50,z);for(let i=0;i<p.count;i++)p.setY(i,max(0,p.getY(i)));if(x< -1.3&&z< -2.8&&sx>.10){const fine=refineStone(g,salt,2),fp=fine.attributes.position;for(let k=0;k<fp.count;k++)fp.setY(k,max(0,fp.getY(k)));put(fine,'gravel');}else put(g,'gravel');}
 for(let i=0;i<650;i++){const x=rubble()*7.45-3.725,z=rubble()*7.45-3.725,edge=abs(x)>2.8||abs(z)>3.4;if(!edge&&rubble()<.84)continue;if(abs(x)<1.15&&abs(z)>3)continue;if(pools.some(p=>poolR(x,z,p)<1.15))continue;const s=(edge?.06:.018)+rubble()*(edge?.16:.045);const salt=rubble()*9;if(!(x<-1.35&&z>-2.8&&z<3.1))stone(x,z,s,s*.55,s*.8,salt);}
 // Local talus follows separate collapse pockets; no perimeter necklace.
 root.userData.buttressTalus=[];const talus=random(461293);
 const clusters=[[-2.18,-2.16,.34,.4,11],[-3.06,-.55,.24,.6,8],[-1.65,1.05,.3,.45,9],[-2.08,2.55,.36,.3,12]];
 for(const [cluster,cfg]of clusters.entries()){const [cx,cz,wx,wz,count]=cfg;
  for(let i=0;i<count;i++){const family=(i+cluster)%4,size=.035+pow(talus(),2)*.23,x=cx+(talus()+talus()-1)*wx,z=cz+(talus()+talus()-1)*wz;
   const ratios=[[1.8,.3,1.05],[1.35,.28,.72],[1.6,.42,.48],[.95,.32,1.35]][family],points=[],sides=4+Math.floor(talus()*4),twist=talus()*6.28;
   for(const layer of [-1,1])for(let k=0;k<sides;k++){const a=(k+(talus()-.5)*.35)/sides*PI*2+twist,r=.66+talus()*.38;points.push(new T.Vector3(cos(a)*r*ratios[0]+layer*.18,layer*(.55+talus()*.35)*ratios[1],sin(a)*r*ratios[2]));}
   const g=refineStone(new ConvexGeometry(points),cluster*17+i*3.7);g.scale(size,size,size);g.rotateX((talus()-.5)*.5);g.rotateY(talus()*6.28);g.rotateZ((talus()-.5)*.35);g.computeBoundingBox();const burial=(g.boundingBox.max.y-g.boundingBox.min.y)*(.07+talus()*.15);g.translate(x,ground(x,z)-g.boundingBox.min.y-burial,z);const gp=g.attributes.position;for(let k=0;k<gp.count;k++)gp.setY(k,max(0,gp.getY(k)));g.computeBoundingBox();const gb=g.boundingBox;if(root.userData.footStalagmites.some(s=>gb.max.x>s.x-.18&&gb.min.x<s.x+.18&&gb.max.z>s.z-.18&&gb.min.z<s.z+.18)){g.dispose();continue;}put(g,'gravel');root.userData.buttressTalus.push({cluster,family,size,x,z,triangles:(g.index?.count??g.attributes.position.count)/3});
  }
 }
 // A separate collapse pocket behind the skeleton; preserve existing random streams.
 root.userData.alcoveTalus=[];const pileRandom=random(937241);
 const pile=[[-3.22,.22,.18],[-2.98,.30,.16],[-3.38,.43,.20],[-3.13,.51,.22],[-2.88,.58,.15],[-3.34,.72,.16],[-3.06,.82,.19],[-3.47,.20,.10],[-2.83,.15,.09],[-3.38,.58,.11],[-2.95,.44,.085],[-3.18,.93,.075]];
 const pileMeshes=[];
 for(const [i,[x,z,size]]of pile.entries()){
  const points=[],sides=5+i%3;
  for(const layer of [-1,1])for(let k=0;k<sides;k++){const a=(k+(pileRandom()-.5)*.3)/sides*PI*2,r=.7+pileRandom()*.35;points.push(new T.Vector3(cos(a)*r*(1.1+(i%3)*.2)+layer*.12,layer*(.35+pileRandom()*.25),sin(a)*r));}
  const g=refineStone(new ConvexGeometry(points),100+i*4.17);g.scale(size,size,size);g.rotateY(pileRandom()*6.28);g.rotateZ((pileRandom()-.5)*1.1);g.rotateX((pileRandom()-.5)*.7);g.computeBoundingBox();
  let support=ground(x,z);if(i===9){const hit=new T.Raycaster(new T.Vector3(x,1,z),new T.Vector3(0,-1,0)).intersectObjects(pileMeshes)[0];if(hit)support=hit.point.y;}
  g.translate(x,support-g.boundingBox.min.y-.018,z);g.computeBoundingBox();const b=g.boundingBox;root.userData.alcoveTalus.push({x,z,min:b.min.toArray(),max:b.max.toArray(),triangles:(g.index?.count??g.attributes.position.count)/3});const mesh=new T.Mesh(g,new T.MeshBasicMaterial({side:T.DoubleSide}));mesh.updateMatrixWorld();pileMeshes.push(mesh);put(g,'gravel',0xb0a18b);
 }
 for(let i=0;i<18;i++){const a=i*.43,p=pools[0],x=p.x+cos(a)*p.rx*1.17,z=p.z+sin(a)*p.rz*1.17;if(x>3.55)continue;const s=.04+rubble()*.09;stone(x,z,s,s*.7,s*1.3,i);}
 for(const [kind,x,z,size,yaw]of[['ribs',-2.97,.83,.36,1.8],['pelvis',-3.04,1.16,.2,.2],['femur',-2.91,.45,.33,-.5],['humerus',-3.13,.61,.3,1.3],['femur',-2.84,.78,.28,2.5]]){const scan=bones[kind],g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(scan.positions,3));g.setIndex(scan.indices);g.scale(size,size,size);g.rotateY(yaw);g.rotateZ(.17);g.computeBoundingBox();g.translate(x,ground(x,z)+.012-g.boundingBox.min.y,z);put(g,'bones',palette.agedBone);}
 // Fine slack silk: an independent, damaged fan rather than equal radial spokes.
 function silk(a,b,r){const p=new T.Vector3(...a),q=new T.Vector3(...b),d=q.clone().sub(p),g=new T.CylinderGeometry(r*.72,r,d.length(),3,1,true);g.applyQuaternion(new T.Quaternion().setFromUnitVectors(new T.Vector3(0,1,0),d.normalize()));g.translate(...p.add(q).multiplyScalar(.5).toArray());put(g,'webs',0xc9c4b5);}
 const wr=random(713491),wallMeshes=['east','north','scannedWalls'].flatMap(cat=>(bins[cat]??[]).map(g=>{const m=new T.Mesh(g,new T.MeshBasicMaterial({side:T.DoubleSide}));m.name=cat;m.updateMatrixWorld();return m;}));
 function anchor(wall,a,y){const o=new T.Vector3(wall==='east'?0:a,y,wall==='east'?a:0),dir=new T.Vector3(wall==='east'?1:0,0,wall==='north'?1:0),hit=new T.Raycaster(o,dir,0,4.1).intersectObjects(wallMeshes)[0];if(!hit)throw Error('missing web anchor');const p=hit.point.clone().addScaledVector(dir,-.012);return {wall,point:p.toArray()};}
 root.userData.webAnchors=[anchor('east',2.35,2.92),anchor('north',2.30,2.84),anchor('east',3.05,2.35)];
 const [A,B,C]=root.userData.webAnchors.map(a=>new T.Vector3(...a.point)),center=A.clone().add(B).multiplyScalar(.5);center.y=2.57;center.x-=.14;center.z-=.14;
 const u=B.clone().sub(A).normalize(),v=new T.Vector3(0,1,0).addScaledVector(u,-u.y).normalize(),normal=u.clone().cross(v).normalize(),radius=.36,turns=12;
 root.userData.orbWeb={center:center.toArray(),normal:normal.toArray(),radius,turns};
 function orb(a,r){const uneven=1+.026*sin(a*3+.4)+.018*sin(a*7);return center.clone().addScaledVector(u,cos(a)*r*uneven).addScaledVector(v,sin(a)*r).toArray();}
 const spokes=24;
 for(let i=0;i<spokes;i++){const a=i/spokes*PI*2+.022*sin(i*3.1);silk(orb(a,.017),orb(a,radius),.0017);}
 for(let i=0;i<96;i++)silk(orb(i/96*PI*2,radius),orb((i+1)/96*PI*2,radius),.0017);
 const steps=turns*48;
 for(let i=0;i<steps;i++){const t=i/steps,t2=(i+1)/steps;silk(orb(t*turns*PI*2,.034+t*(radius-.046)),orb(t2*turns*PI*2,.034+t2*(radius-.046)),.00115);}
 const candidates=[];
 for(let i=0;i<spokes;i++){const a=i/spokes*PI*2+.022*sin(i*3.1),rim=new T.Vector3(...orb(a,radius)),dir=rim.clone().sub(center).normalize(),hit=new T.Raycaster(rim,dir,.005,2.5).intersectObjects(wallMeshes)[0];if(hit&&hit.point.y>.8&&hit.point.x>1.82&&hit.point.z>1.82&&hit.distance>.06)candidates.push({quadrant:(cos(a)>=0?0:1)+(sin(a)>=0?0:2),spoke:i,rim:rim.toArray(),end:hit.point.toArray(),wall:hit.object.name==='north'?'north':'east'});}
 if(candidates.length<6)throw Error('not enough radial wall anchors: '+candidates.length);
 root.userData.webTethers=[];
 for(let q=0;q<4;q++){const options=candidates.filter(t=>t.quadrant===q).sort((a,b)=>{const score=t=>Math.abs(Math.abs(sin(t.spoke/spokes*PI*2))-.707);return score(a)-score(b);});if(options.length<2)throw Error('missing quadrant '+q+' '+options.length);root.userData.webTethers.push(...options.slice(0,2));}
 root.userData.webTethers.sort((a,b)=>a.spoke-b.spoke);
 root.userData.webAnchors=root.userData.webTethers.map(t=>({wall:t.wall,point:t.end}));
 for(const t of root.userData.webTethers)silk(t.rim,t.end,.0017);
 root.userData.webBraces=[];
 for(const fraction of [.12,.27])for(let i=0;i<root.userData.webTethers.length;i++){const a=root.userData.webTethers[i],b=root.userData.webTethers[(i+1)%root.userData.webTethers.length],p=new T.Vector3(...a.rim).lerp(new T.Vector3(...a.end),fraction),q=new T.Vector3(...b.rim).lerp(new T.Vector3(...b.end),fraction),mid=p.clone().add(q).multiplyScalar(.5).lerp(center,.018);if(mid.distanceTo(center)<radius+.035)mid.copy(center.clone().add(mid.clone().sub(center).normalize().multiplyScalar(radius+.035)));silk(p.toArray(),mid.toArray(),.00115);silk(mid.toArray(),q.toArray(),.00115);root.userData.webBraces.push({from:a.spoke,to:b.spoke,fraction,start:p.toArray(),end:q.toArray()});}

 for(const [cat,geos]of Object.entries(bins)){const raw=mergeGeometries(geos,false);if(cat==='bones')raw.translate(-.1,0,-1.38);if(cat==='buttressScan')for(const group of geos[0].groups)raw.addGroup(group.start,group.count,group.materialIndex);const merged=mergeVertices(raw,.00001);raw.dispose();const m=new (cat==='water'?T.MeshPhysicalMaterial:T.MeshStandardMaterial)({vertexColors:true,side:T.DoubleSide,roughness:cat==='water'?.18:.91,metalness:0});m.name=cat;if(cat==='webs'){m.transparent=true;m.opacity=.48;m.depthWrite=false;m.roughness=.78;}if(cat==='water'){m.transparent=true;m.opacity=.68;m.depthWrite=false;m.roughness=.35;m.specularIntensity=.12;m.ior=1.33;}if(cat==='floor')m.color.setRGB(1.9,1.65,1.38);const fracture=m.clone();fracture.name='buttressFracture';fracture.color.setRGB(.40,.32,.22);const mesh=new T.Mesh(merged,m);mesh.name=cat;root.add(mesh);for(const g of geos)g.dispose();}return root;
}
