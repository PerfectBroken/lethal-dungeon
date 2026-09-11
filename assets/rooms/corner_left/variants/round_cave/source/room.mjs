import catalog from '../../../../../../docs/examples/base-rooms.json' with {type:'json'};
import {addDetails} from './details.mjs';
import identity from '../identity.json' with {type:'json'};
import * as T from 'three';
const {sin,cos,abs,max,min,sqrt}=Math;
const mix=(a,b,t)=>a+(b-a)*t;
function noise(x,y,z){const ix=Math.floor(x),iy=Math.floor(y),iz=Math.floor(z);let a=x-ix,b=y-iy,c=z-iz;a=a*a*(3-2*a);b=b*b*(3-2*b);c=c*c*(3-2*c);const h=(x,y,z)=>{let n=Math.imul(x,374761393)^Math.imul(y,668265263)^Math.imul(z,1274126177);n=Math.imul(n^(n>>>13),1274126177);return ((n^(n>>>16))>>>0)/4294967295*2-1};return mix(mix(mix(h(ix,iy,iz),h(ix+1,iy,iz),a),mix(h(ix,iy+1,iz),h(ix+1,iy+1,iz),a),b),mix(mix(h(ix,iy,iz+1),h(ix+1,iy,iz+1),a),mix(h(ix,iy+1,iz+1),h(ix+1,iy+1,iz+1),a),b),c)}
function smin(a,b,k){const h=max(k-abs(a-b),0)/k;return min(a,b)-h*h*k*.25}
function box(x,y,z,a,b,c){const q=[abs(x)-a,abs(y)-b,abs(z)-c];return Math.hypot(...q.map(v=>max(v,0)))+min(max(...q),0)}
export function floorHeight(x,z){const fade=min(1,max(0,(4-max(abs(x),abs(z)))*2));return .16+fade*(.018*noise(x*2,0,z*2)+.01*sin(x*4+z*3))}
export function createField(doors={south:0,west:0}){
 const definition=catalog.rooms.find(r=>r.id===identity.roomId),selected={};
 if(Object.keys(doors).some(k=>!definition.sockets.some(s=>s.id===k)))throw new Error('Unknown door');
 for(const socket of definition.sockets){const offset=doors[socket.id]??(doors[socket.id]===null?null:0);if(offset!==null&&!socket.tangentOffsets.includes(offset))throw new Error('Unconfigured door offset');selected[socket.id]=offset===null?null:{x:(socket.position[0]+(socket.facing==='south'?offset:0))*.5,z:(socket.position[2]+(socket.facing==='west'?offset:0))*.5,width:socket.size[0]*.5,height:socket.size[1]*.5};}
 return function field(x,y,z){
 const r=sqrt((x/3.45)**2+((y-.80)/2.72)**2+(z/3.48)**2);
 const strata=.06*sin(y*7+.85*sin(x*.85+z*.6)+.4*sin(z*2.6))+.075*sin(Math.atan2(z,x)*13+y*.85+.6*sin(y*2.5));
 let chamber=(r-1)*2.65+.18*noise(x*.8,y*.9,z*.8)+.068*noise(x*2.7,y*2.1,z*2.7)+.027*noise(x*7,y*5,z*7)+strata;
 // Broaden south/west interior into continuous wall bays spanning every configured opening.
 const bayRelief=(along)=>{
  const shoulder=.34*((y-1.25)/1.75)**2;
  const mass=.22+.24*sin(along*1.65+y*.9)+.17*sin(y*2.3-along*.55);
  const folded=.105*sin(y*6.2+along*.8+.55*sin(along*1.3));
  return 1.45*(shoulder+mass+folded+.085*noise(along*2.3,y*2.6,1));
 };
 chamber=smin(chamber,box(x,y-1.72,z+1.95,2.85,1.40,1.50)-.12+bayRelief(x),.35);
 chamber=smin(chamber,box(x+1.95,y-1.72,z,1.50,1.40,2.85)-.12+bayRelief(z),.35);
 // V04: unequal continuous corner shoulders, broader near roof and floor.
 const rise=Math.max(0,(y-1.8)/1.7),foot=Math.max(0,(.65-y)/.65);
 for(const [cx,cz,sx,sz,base,stretch]of [[-2.50,1.95,-1,1,.94,.88],[-2.48,-2.50,-1,-1,.85,1.10],[2.25,-2.50,1,-1,.75,.88]]){
  const u=sx*(x-cx),v=sz*(z-cz);
  if(u>0&&v>0){const radius=base-.13*rise-.055*foot+.025*sin(y*3.8+x*2+z*1.7);
   const limit=(Math.hypot(u,v*stretch)-radius)+.015*noise(x*5,y*4,z*5);
   let blend=Math.min(1,Math.min(u,v)/.30);blend=blend*blend*(3-2*blend);
   chamber=mix(chamber,-smin(-chamber,-limit,.22),blend);
  }
 }
 // North face: bent bedding ledges sculpted into the rock volume, not painted stripes.
 const northWeight=Math.max(0,Math.min(1,(z-1.3)/1.0));
 const sideFade=Math.max(0,Math.min(1,(3.3-Math.abs(x))/.85));
 const heightFade=Math.max(0,Math.min(1,(y-.20)/.40,(3.45-y)/.55));
 const rx=(x-.25)+.24*(y-1.55),ry=(y-1.75)*1.35;
 const angle=Math.atan2(ry,rx);
 const bedding=Math.hypot(rx,ry)+.10*sin(angle*3+.5)+.065*sin(x*2+y*2.3);
 let relief=0;
 for(const [level,width,depth] of [[1.24,.25,.28],[2.42,.28,.29]]){
  const t=(bedding-level+.045*sin(angle*5+level*2))/(width*(1+.18*sin(angle*2+level)));
  relief+=depth*Math.exp(-t*t*(t>0?1.6:.65));
 }
 chamber+=northWeight*sideFade*heightFade*(relief-.055);
 for(const [side,door]of Object.entries(selected)){if(!door)continue;const along=side==='south';const throat=along?box(x-door.x,y-1.65,z+2.5,door.width/2-.5,1.3,2.6)-.5:box(x+2.5,y-1.65,z-door.z,2.6,1.3,door.width/2-.5)-.5;chamber=smin(chamber,throat,.18);}
 const cavity=-smin(-chamber,y-floorHeight(x,z),.12);
 // Broad partly buried lens in the upper-left north face, fused into parent rock.
 const bx=x+1.10,by=y-2.60,bz=z-2.30;
 const lens=(sqrt(((bx+.22*by)/1.12)**2+(by/.66)**2+(bz/1.13)**2)-1)*.66
  +.034*sin(x*7+y*9+z*3)+.026*noise(x*5,y*4,z*5);
 let solid=smin(-cavity,lens,.23);
 // Selected door clearance takes priority over the decorative embedded boulder.
 for(const [side,door]of Object.entries(selected)){if(!door)continue;const throat=side==='south'?box(x-door.x,y-1.65,z+2.5,door.width/2-.5,1.3,2.6)-.5:box(x+2.5,y-1.65,z-door.z,2.6,1.3,door.width/2-.5)-.5;solid=max(solid,-throat);}
 solid=min(solid,y-floorHeight(x,z));
 return max(solid,box(x,y-2,z,4,2,4));
}
}
export const field=createField();
const tet=[[0,5,1,6],[0,1,2,6],[0,2,3,6],[0,3,7,6],[0,7,4,6],[0,4,5,6]],edges=[[0,1],[0,2],[0,3],[1,2],[1,3],[2,3]];
export function buildRoom({details=false,doors={south:0,west:0}}={}){
 const field=createField(doors);
 function gradient(x,y,z){const e=.002;return new T.Vector3(field(x+e,y,z)-field(x-e,y,z),field(x,y+e,z)-field(x,y-e,z),field(x,y,z+e)-field(x,y,z-e)).normalize()}
 const root=new T.Group();root.name='corner_left_round_cave';root.userData={roomIdentity:identity,doorOffsets:doors,north:'+Z',units:'metres',concept:'V04',stage:details?'details':'structure'};
 const bins={rock:[],ceiling:[],floor:[],shell:[]},N=66,M=34,step=.125,ox=-4.125,oy=-.125,oz=-4.125,vals=new Float32Array((N+1)*(M+1)*(N+1));const idx=(i,j,k)=>(i*(M+1)+j)*(N+1)+k;
 for(let i=0;i<=N;i++)for(let j=0;j<=M;j++)for(let k=0;k<=N;k++)vals[idx(i,j,k)]=field(ox+i*step,oy+j*step,oz+k*step);
 function tri(a,b,c){const center=a.clone().add(b).add(c).multiplyScalar(1/3),normal=gradient(...center.toArray()),cross=b.clone().sub(a).cross(c.clone().sub(a));if(cross.lengthSq()<1e-14)return;if(cross.dot(normal)<0)[b,c]=[c,b];const exterior=Math.abs(center.x)>3.99||Math.abs(center.z)>3.99||center.y<.01||center.y>3.99;const cat=exterior?'shell':center.y<.24?'floor':center.y>2.82?'ceiling':'rock';bins[cat].push(...a.toArray(),...b.toArray(),...c.toArray())}
 for(let i=0;i<N;i++)for(let j=0;j<M;j++)for(let k=0;k<N;k++){
  const offsets=[[0,0,0],[1,0,0],[1,1,0],[0,1,0],[0,0,1],[1,0,1],[1,1,1],[0,1,1]],v=offsets.map(([a,b,c])=>vals[idx(i+a,j+b,k+c)]);if(v.every(n=>n>=0)||v.every(n=>n<0))continue;
  const p=offsets.map(([a,b,c])=>new T.Vector3(ox+(i+a)*step,oy+(j+b)*step,oz+(k+c)*step));
  for(const t of tet){let poly=[];for(const [a,b]of edges){const ia=t[a],ib=t[b];if((v[ia]<0)===(v[ib]<0))continue;poly.push(p[ia].clone().lerp(p[ib],v[ia]/(v[ia]-v[ib])))}if(poly.length<3)continue;if(poly.length===4){const center=poly.reduce((a,b)=>a.add(b),new T.Vector3()).multiplyScalar(.25),normal=gradient(...center.toArray()),u=poly[0].clone().sub(center).normalize(),w=normal.clone().cross(u);poly.sort((a,b)=>Math.atan2(a.clone().sub(center).dot(w),a.clone().sub(center).dot(u))-Math.atan2(b.clone().sub(center).dot(w),b.clone().sub(center).dot(u)))}tri(poly[0],poly[1],poly[2]);if(poly.length===4)tri(poly[0],poly[2],poly[3]);}
 }
 for(const [name,vertices]of Object.entries(bins)){const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(vertices,3));const n=[],color=[],uv=[];for(let i=0;i<vertices.length;i+=3){const [x,y,z]=vertices.slice(i,i+3);n.push(...gradient(x,y,z).toArray());const tint=.90+.055*noise(x*.6,y*.8,z*.6);color.push(tint,tint*.97,tint*.93);uv.push(x/1.5,z/1.5)}g.setAttribute('normal',new T.Float32BufferAttribute(n,3));g.setAttribute('color',new T.Float32BufferAttribute(color,3));g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));g.setIndex(Array.from({length:vertices.length/3},(_,i)=>i));const m=new T.MeshStandardMaterial({name,color:0xffffff,roughness:.9,vertexColors:true,side:T.DoubleSide});const mesh=new T.Mesh(g,m);mesh.name=name;root.add(mesh)}
 if(details)addDetails(root,floorHeight);
 return root;
}
