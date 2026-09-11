import * as T from 'three';import {mergeGeometries,mergeVertices} from 'three/addons/utils/BufferGeometryUtils.js';import {refineStone} from './fractured-stone.mjs';
export function addDetails(root,ground){let seed=28039;const rand=()=>{seed=(Math.imul(seed,1664525)+1013904223)>>>0;return seed/4294967296};root.updateMatrixWorld(true);const wall=root.getObjectByName('rock'),roof=root.getObjectByName('ceiling'),stones=[],tips=[],records={rubble:[],fineRubble:[],pendants:[]};
const ray=(p,d,obj)=>new T.Raycaster(new T.Vector3(...p),new T.Vector3(...d),.001,5).intersectObject(obj);
function store(g,category,color){if(g.index)g=g.toNonIndexed();if(!g.attributes.normal)g.computeVertexNormals();const c=new T.Color(color),colors=[];for(let i=0;i<g.attributes.position.count;i++)colors.push(c.r,c.g,c.b);g.setAttribute('color',new T.Float32BufferAttribute(colors,3));category.push(g)}
// Unequal clusters at naturally closed perimeter sections, never a regular ring.
for(const [angle,count]of [[-.65,28],[.1,24],[.85,30],[1.7,22],[2.45,25],[-2.4,26]])for(let j=0;j<count;j++){const a=angle+(rand()-.5)*.60,dx=Math.sin(a),dz=Math.cos(a),hit=ray([0,.35,0],[dx,0,dz],wall)[0];if(!hit||hit.distance>4.8)continue;const radius=hit.distance-(.20+rand()*.18),x=dx*radius,z=dz*radius;if(Math.hypot(x,z)<2.3||(x< -2.5&&Math.abs(z)<2.3)||(z< -2.5&&Math.abs(x)<2.3))continue;
const s=.035+rand()**2.5*.25,sx=s*(.85+rand()*.7),sy=s*(.3+rand()*.6),sz=s*(.6+rand()*.75),salt=rand()*90;let g=new T.IcosahedronGeometry(1,0),p=g.attributes.position;for(let i=0;i<p.count;i++){const xx=p.getX(i),yy=p.getY(i),zz=p.getZ(i),r=1+.16*Math.sin(xx*4+yy*3+zz*6+salt);p.setXYZ(i,xx*r+.18*yy,Math.min(.7,yy)*r,zz*r)}g.scale(sx,sy,sz);g.rotateY(rand()*6.28);g=refineStone(g,salt,2);g.computeBoundingBox();const y=ground(x,z)-g.boundingBox.min.y-.025;g.translate(x,y,z);g.computeBoundingBox();const bb=g.boundingBox;
const blocked=Object.entries(root.userData.doorOffsets).some(([side,offset])=>{if(offset===null)return false;const c=offset*.5;return side==='south'?bb.min.z< -1.0&&bb.max.z> -4&&bb.min.x<c+1.05&&bb.max.x>c-1.05:bb.min.x< -1.0&&bb.max.x> -4&&bb.min.z<c+1.05&&bb.max.z>c-1.05;});if(blocked)continue;
store(g,stones,new T.Color().setRGB(.24+rand()*.06,.22+rand()*.05,.19+rand()*.05));records.rubble.push({x,z,sx,sy,sz,y});}
// Small fragments rest on actual rock triangles or terrain; cluster spacing is irregular.
const support=new T.Group(),supportMaterial=new T.MeshBasicMaterial({side:T.DoubleSide});for(const geometry of stones)support.add(new T.Mesh(geometry,supportMaterial));support.updateMatrixWorld(true);
for(const base of records.rubble.filter(p=>Math.max(p.sx,p.sz)>.075))for(let j=0;j<3;j++){
 const angle=rand()*6.28,reach=j<1?.35:1.6+rand()*1.2;
 const x=base.x+Math.cos(angle)*base.sx*reach,z=base.z+Math.sin(angle)*base.sz*reach;
 if(Math.hypot(x,z)<2.3)continue;
 const size=.018+rand()**.65*.048;
 const blocked=Object.entries(root.userData.doorOffsets).some(([side,offset])=>offset!==null&&(side==='south'?z-size< -1&&Math.abs(x-offset*.5)<1.05+size:x-size< -1&&Math.abs(z-offset*.5)<1.05+size));if(blocked)continue;
 const supportHit=new T.Raycaster(new T.Vector3(x,1,z),new T.Vector3(0,-1,0),0,1).intersectObject(support)[0];
 const top=supportHit?Math.max(ground(x,z),supportHit.point.y):ground(x,z);
 const g=new T.IcosahedronGeometry(1,1),p=g.attributes.position,salt=rand()*70;
 for(let i=0;i<p.count;i++){const a=p.getX(i),b=p.getY(i),c=p.getZ(i),k=1+.2*Math.sin(a*7+b*5+c*6+salt);p.setXYZ(i,a*k+.25*b,b*k,c*k);}
 g.scale(size*(.7+rand()*.7),size*(.35+rand()*.5),size*(.65+rand()*.7));g.rotateY(rand()*6.28);g.computeBoundingBox();let lift=ground(x,z)-g.boundingBox.min.y;
let supportMin=Infinity,supportMax=-Infinity;
for(let k=0;k<p.count;k++){const vx=x+p.getX(k),vz=z+p.getZ(k);const h=new T.Raycaster(new T.Vector3(vx,1,vz),new T.Vector3(0,-1,0),0,1).intersectObject(support)[0];const sy=h?Math.max(ground(vx,vz),h.point.y):ground(vx,vz);lift=Math.max(lift,sy-p.getY(k));supportMin=Math.min(supportMin,sy);supportMax=Math.max(supportMax,sy);}
if(supportMax-supportMin>size*.8)continue;
g.translate(x,lift+.001,z);g.computeVertexNormals();store(g,stones,new T.Color().setRGB(.26+rand()*.08,.24+rand()*.06,.20+rand()*.055));records.fineRubble.push({x,z,size,stacked:top>ground(x,z)+.025});
}
supportMaterial.dispose();
for(const [x,z,length,width]of [[-1.45,.35,.98,.115],[-1.85,.2,.48,.075],[-1.05,.75,.62,.065],[-2.0,.9,.54,.085],[1.45,1.4,.68,.09],[1.9,.75,.43,.075],[1.95,1.55,.48,.085]]){
const slenderWidth=width*.8,wavy=length>=.6;
const hit=new T.Raycaster(new T.Vector3(x,1.5,z),new T.Vector3(0,1,0),.001,5).intersectObjects([roof,wall])[0];if(!hit)continue;const top=hit.point.y+.07,bottom=hit.point.y-length;if(bottom<2.0)continue;const verts=[],uv=[],segments=28,rows=40,phase=rand()*6.28;
const rootHeights=new Map();const rootAt=(a)=>{const rr=2.6*slenderWidth*1.012*(1+.10*Math.sin(a*3+4+phase)),xx=x+Math.cos(a)*rr,zz=z+Math.sin(a)*rr;const key=a.toFixed(5);if(rootHeights.has(key))return rootHeights.get(key);const hit=new T.Raycaster(new T.Vector3(xx,1.5,zz),new T.Vector3(0,1,0),.001,5).intersectObjects([roof,wall])[0],h=hit?hit.point.y+.012:top;rootHeights.set(key,h);return h};
const pt=(u,v)=>{const a=u*Math.PI*2;
let shelves=0;for(const [h,w,k]of [[.23,.040,.30],[.41,.055,.26],[.62,.07,.35],[.81,.065,.25]])shelves+=(wavy?k:0)*Math.exp(-(((v-h+.012*Math.sin(a*3+phase))/w)**2));
const flare=Math.max(0,(v-.65)/.35);const fade=Math.sin(Math.PI*v),r=(1+1.6*flare**3)*slenderWidth*(.012+v**(wavy?1.1:1.7))*(1+shelves)*(1+fade*.17*Math.sin(a*5+phase)+fade*.085*Math.sin(a*9+v*3)+.10*Math.sin(a*3+v*4+phase));
const xx=x+Math.cos(a)*r,zz=z+Math.sin(a)*r,height=rootAt(a);const blend=flare*flare*(3-2*flare);return[xx,(bottom+(top-bottom)*v)*(1-blend)+height*blend,zz]};for(let i=0;i<segments;i++)for(let j=0;j<rows;j++){for(const [u,v]of [[i/segments,j/rows],[(i+1)/segments,j/rows],[i/segments,(j+1)/rows],[(i+1)/segments,j/rows],[(i+1)/segments,(j+1)/rows],[i/segments,(j+1)/rows]]){verts.push(...pt(u,v));uv.push(u,v*2)}}const g=new T.BufferGeometry();g.setAttribute('position',new T.Float32BufferAttribute(verts,3));g.setAttribute('uv',new T.Float32BufferAttribute(uv,2));const smooth=mergeVertices(g,1e-5);smooth.computeVertexNormals();store(smooth,tips,new T.Color().setRGB(.90,.873,.837));records.pendants.push({x,z,roof:hit.point.y,bottom,length,width,wavy});}
for(const [name,list]of [['rubble',stones],['pendants',tips]]){const g=mergeGeometries(list,false);const m=new T.MeshStandardMaterial({name,color:0xffffff,roughness:.95,vertexColors:true,side:T.DoubleSide});const mesh=new T.Mesh(g,m);mesh.name=name;root.add(mesh)}root.userData.details=records;return records;
}
