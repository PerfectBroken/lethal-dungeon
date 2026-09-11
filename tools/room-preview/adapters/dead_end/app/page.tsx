'use client';
import {useEffect,useRef,useState} from 'react';
import * as THREE from 'three';
import {OrbitControls} from 'three/addons/controls/OrbitControls.js';
import {GLTFLoader} from 'three/addons/loaders/GLTFLoader.js';
import {Switch} from '@/components/ui/switch';
import {Slider} from '@/components/ui/slider';
import {Eye,Box,ArrowDown,RotateCcw,Download,Layers,Sun,Move,Image as ImageIcon,X} from 'lucide-react';
import modelInfo from '../model-info.json';
type View='inside'|'model'|'top'|'shelter';
type Viewer={setView:(v:View)=>void;roof:(b:boolean)=>void;wire:(b:boolean)=>void;light:(v:number)=>void;path:(b:boolean)=>void};
export default function Page(){
 const host=useRef<HTMLDivElement>(null),api=useRef<Viewer|null>(null);
 const [view,setView]=useState<View>('inside'),[roof,setRoof]=useState(true),[wire,setWire]=useState(false),[path,setPath]=useState(false),[light,setLight]=useState(100),[reference,setReference]=useState(false),[status,setStatus]=useState('正在加载房间模型…'),[ready,setReady]=useState(false),[failed,setFailed]=useState(false);
 useEffect(()=>{
  if(!host.current)return;const el=host.current;let disposed=false,raf=0,room:THREE.Group|undefined,renderer:THREE.WebGLRenderer;
  try{renderer=new THREE.WebGLRenderer({antialias:true,alpha:false,powerPreference:'high-performance'});}catch{setStatus('当前浏览器无法启动 3D 显示，请使用支持 WebGL 2 的浏览器。');setFailed(true);return;}
  renderer.setPixelRatio(Math.min(window.devicePixelRatio,1.75));renderer.shadowMap.enabled=true;renderer.shadowMap.type=THREE.PCFSoftShadowMap;renderer.toneMapping=THREE.ACESFilmicToneMapping;renderer.toneMappingExposure=1.3;el.appendChild(renderer.domElement);renderer.domElement.setAttribute('aria-label','山洞房间三维预览，拖动旋转，滚轮缩放');
  const scene=new THREE.Scene();scene.background=new THREE.Color('#121715');
  const camera=new THREE.PerspectiveCamera(62,1,.04,90);camera.position.set(.4,1.7,3.35);
  const orbit=new OrbitControls(camera,renderer.domElement);orbit.target.set(-.3,1.65,-1.85);orbit.enableDamping=true;orbit.dampingFactor=.08;orbit.minDistance=.5;orbit.maxDistance=23;orbit.maxPolarAngle=Math.PI*.96;orbit.update();
  const hemi=new THREE.HemisphereLight(0xd8e4d6,0x514433,1.6);scene.add(hemi);
  const inspection=new THREE.SpotLight(0xffe7c7,38,19,1.1,.7,1.6);inspection.position.set(.1,2.65,2.8);inspection.target.position.set(0,1.6,-1);inspection.castShadow=true;inspection.shadow.mapSize.set(1024,1024);inspection.shadow.bias=-.001;inspection.shadow.normalBias=.04;scene.add(inspection,inspection.target);
  const fill=new THREE.PointLight(0x9ab4c1,7,10,1.7);fill.position.set(-2.9,2.7,-1.2);scene.add(fill);
  const external=new THREE.DirectionalLight(0xffe7c5,2.4);external.position.set(-3,7,4);scene.add(external);
  const floor=new THREE.Mesh(new THREE.CylinderGeometry(7,7, .12,96),new THREE.MeshStandardMaterial({color:0x1b2420,roughness:1}));floor.position.y=-.19;scene.add(floor);
  const pts=[[2,.09,3.65],[1,.09,2.65],[0,.09,1.6],[0,.09,0],[-.6,.09,-1.65]].map(a=>new THREE.Vector3(...a as [number,number,number]));const pg=new THREE.BufferGeometry().setFromPoints(pts);const line=new THREE.Line(pg,new THREE.LineDashedMaterial({color:0xd9be79,dashSize:.16,gapSize:.13,depthTest:false}));line.computeLineDistances();line.visible=false;line.renderOrder=5;scene.add(line);
  const droplets=new THREE.Group();scene.add(droplets);
  function addDrips(data:{start:number[];end:number[];surface:string;period?:number;phase?:number}[]){for(const [i,d] of data.entries()){const m=new THREE.Mesh(new THREE.SphereGeometry(.014,6,5),new THREE.MeshBasicMaterial({color:0xa1b5b2}));m.scale.y=2.3;m.userData={start:d.start,end:d.end,period:d.period??1.8,phase:d.phase??i*.17};m.position.set(...d.start as [number,number,number]);droplets.add(m);}}
  let roofShown=true,currentView:View='inside';
  function visibility(){if(!room)return;const c=room.getObjectByName('ceiling'),f=room.getObjectByName('front');if(c)c.visible=roofShown;if(f)f.visible=currentView==='inside';floor.visible=currentView!=='inside';droplets.visible=roofShown;}
  function select(v:View){currentView=v;camera.up.set(0,1,0);if(v==='inside'){camera.position.set(.4,1.7,3.35);orbit.target.set(-.3,1.65,-1.85);camera.fov=172;}else if(v==='shelter'){camera.position.set(.0,1.65,.5);orbit.target.set(-2.25,.92,-2.55);camera.fov=46;}else if(v==='model'){camera.position.set(10,8.4,12);orbit.target.set(0,1.3,0);camera.fov=174;}else{camera.position.set(0,14,.001);orbit.target.set(0,0,0);camera.fov=170;}camera.updateProjectionMatrix();orbit.update();visibility();}
  api.current={setView:select,roof(b){roofShown=b;visibility()},wire(b){room?.traverse(o=>{if(o instanceof THREE.Mesh){const mats=Array.isArray(o.material)?o.material:[o.material];mats.forEach(m=>{if(m instanceof THREE.MeshStandardMaterial)m.wireframe=b})}})},light(v){renderer.toneMappingExposure=v/100*1.3},path(b){line.visible=b}};
  const loader=new GLTFLoader();loader.load('/models/dead-end-cave.glb?v=26b',g=>{if(disposed){g.scene.traverse(o=>{if(o instanceof THREE.Mesh){o.geometry.dispose();(Array.isArray(o.material)?o.material:[o.material]).forEach(m=>m.dispose());}});return;}room=g.scene;room.traverse(o=>{if(o instanceof THREE.Mesh){o.castShadow=o.name!=='water'&&o.name!=='webs';o.receiveShadow=true;const mats=Array.isArray(o.material)?o.material:[o.material];for(const m of mats){if(m instanceof THREE.MeshStandardMaterial&&m.map)m.map.anisotropy=Math.min(8,renderer.capabilities.getMaxAnisotropy());}}});scene.add(room);const dripNode=room.getObjectByProperty("name","dead_end_cave_v1");addDrips(dripNode?.userData.drips??room.userData.drips??[]);visibility();setReady(true);setStatus('模型已就绪');},e=>{if(e.total&&!disposed)setStatus(`正在加载房间模型 ${Math.round(e.loaded/e.total*100)}%`);},()=>{if(!disposed){setStatus('模型加载失败，请刷新重试，或下载 GLB 文件查看。');setFailed(true);}});
  const resize=()=>{const {width,height}=el.getBoundingClientRect();renderer.setSize(width,height);camera.aspect=width/height;camera.updateProjectionMatrix();};const observer=new ResizeObserver(resize);observer.observe(el);resize();
  const lost=(e:Event)=>{e.preventDefault();setStatus('3D 显示已中断，请刷新页面恢复。');setFailed(true);};renderer.domElement.addEventListener('webglcontextlost',lost);
  let last=0;function frame(t:number){raf=requestAnimationFrame(frame);if(document.hidden||t-last<32)return;last=t;orbit.update();droplets.children.forEach(m=>{const {start,end,phase,period}=m.userData;const age=(t*.001+phase*period)%period,fall=Math.sqrt(2*(start[1]-end[1])/9.81);m.visible=age<fall;m.position.y=Math.max(end[1],start[1]-4.905*age*age)});renderer.render(scene,camera);}raf=requestAnimationFrame(frame);
  return()=>{disposed=true;cancelAnimationFrame(raf);observer.disconnect();orbit.dispose();scene.traverse(o=>{if(o instanceof THREE.Mesh||o instanceof THREE.Line){o.geometry.dispose();(Array.isArray(o.material)?o.material:[o.material]).forEach(m=>m.dispose());}});renderer.dispose();renderer.domElement.remove();api.current=null;};
 },[]);
 function changeView(v:View){setView(v);const b=v==='inside'||v==='shelter';setRoof(b);api.current?.roof(b);api.current?.setView(v)}
 return <main className="studio dark">
  <header className="mast"><div className="brand"><span className="brandmark">LD</span><div><span className="eyebrow">致命地下城 / 环境制作</span><h1>哥布林栖居洞穴</h1></div></div><a className="download" href="/models/dead-end-cave.glb" download><Download size={16}/>下载模型 <span>GLB</span></a></header>
  <section className="workspace"><div className="viewport"><div ref={host} className="canvas"/>
   <div className="view-label"><span className="dot"/>01 · 尽头房<span className="divider">/</span><span>{view==='inside'?'室内视角':view==='shelter'?'棚屋近景':view==='top'?'俯视布局':'模型总览'}</span></div>
   {(!ready||failed)&&<div className="loading" role="status">{!failed&&<span className="spinner"/>}{status}{failed&&<button onClick={()=>location.reload()}>重新加载</button>}</div>}
   <nav className="viewbar" aria-label="视角"><button className={view==='inside'?'active':''} onClick={()=>changeView('inside')}><Eye size={17}/>室内</button><button className={view==='shelter'?'active':''} onClick={()=>changeView('shelter')}><Eye size={17}/>棚屋近景</button><button className={view==='model'?'active':''} onClick={()=>changeView('model')}><Box size={17}/>总览</button><button className={view==='top'?'active':''} onClick={()=>changeView('top')}><ArrowDown size={17}/>俯视</button><span/><button aria-label="重置视角" title="重置视角" onClick={()=>changeView(view)}><RotateCcw size={17}/></button></nav>
   <div className="gesture"><Move size={14}/>拖动旋转 · 滚轮缩放 · 右键平移</div>
  </div>
  <aside className="inspector"><div className="section-label">ROOM STUDY <span>V 0.26</span></div><h2>深山 · 天然洞穴</h2><p className="intro">接顶的厚重岩体、细腰石柱，以及盲眼哥布林留下的生活痕迹。</p>
   <div className="dimensions"><div><strong>8 × 8</strong><span>占地 / 米</span></div><div><strong>4</strong><span>层高 / 米</span></div><div><strong>1</strong><span>连接口</span></div></div>
   <div className="controls"><h3><Layers size={15}/>查看模型</h3><label>显示洞顶<Switch checked={roof} onCheckedChange={b=>{setRoof(b);api.current?.roof(b)}} aria-label="显示洞顶"/></label><label>预留通路<Switch checked={path} onCheckedChange={b=>{setPath(b);api.current?.path(b)}} aria-label="显示预留通路"/></label><label>网格线框<Switch checked={wire} onCheckedChange={b=>{setWire(b);api.current?.wire(b)}} aria-label="网格线框"/></label><div className="light-label"><span><Sun size={15}/>展示补光</span><span>{light}%</span></div><Slider value={[light]} min={40} max={170} step={5} onValueChange={v=>{const n=Array.isArray(v)?v[0]:v;setLight(n);api.current?.light(n)}} aria-label="展示补光强度"/><p className="fine">仅用于审阅，房间内没有固定照明。</p></div>
   <button className="reference-button" onClick={()=>setReference(!reference)}><ImageIcon size={16}/>{reference?'收起概念参考':'查看已确认概念图'}<span>↗</span></button>
   <div className="notes"><h3>本版审阅重点</h3><p>三组大石柱改为不对称沉积主干、底部簇生石笋和顶部成簇垂挂；落石移到墙边。</p><p className="fine">精细化审阅版，已更新墙脚过渡、倒三角岩体、棚屋及头骨。门位为本次示例；碰撞体与引擎通行测试待接入。</p></div>
   <footer><span>{modelInfo.triangles.toLocaleString()} 三角形</span><span>米制 · Y 向上</span></footer><p style={{fontSize:11,opacity:.65,padding:"0 20px"}}>地面残骨：<a href="https://www.artec3d.com/3d-models/human-skeleton-hd" target="_blank" rel="noreferrer">Human skeleton HD by Artec 3D</a>（CC BY 4.0，裁切、减面、调色）。头骨：<a href="https://www.artec3d.com/3d-models/goat-skull" target="_blank" rel="noreferrer">Goat skull by Artec 3D</a> · <a href="https://creativecommons.org/licenses/by/4.0/" target="_blank" rel="noreferrer">CC BY 4.0</a> · 已减面、缩放与旋转</p>
  </aside></section>
  {reference&&<section className="reference-panel" aria-label="已确认概念图"><div><strong>已确认概念图</strong><button aria-label="关闭参考图" onClick={()=>setReference(false)}><X size={20}/></button></div><img src="/reference.png" alt="已确认的洞穴设计：右侧接顶岩体，左侧哥布林窝棚与细腰石柱"/></section>}
 </main>
}
