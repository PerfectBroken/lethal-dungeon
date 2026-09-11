import sharp from 'sharp';
import {readFile,writeFile} from 'node:fs/promises';
const file='model/dead-end-cave.glb';
await sharp('textures/fur-normal.png').jpeg({quality:98,chromaSubsampling:'4:4:4'}).toFile('textures/fur-normal.jpg');
// Bake the soil/stone blend into a full-height wall atlas so the GLB retains it.
const size=1024;
const stone=await sharp('textures/limestone.jpg').resize(size,size).removeAlpha().raw().toBuffer();
const soil=await sharp('textures/soil.jpg').resize(size,size).removeAlpha().raw().toBuffer();
const mixed=Buffer.alloc(size*size*3);
for(let y=0;y<size;y++)for(let x=0;x<size;x++){
 const height=4*(1-y/(size-1)),u=x/size;
 const boundary=.5+.19*Math.sin(u*6.283*3)+.11*Math.sin(u*6.283*7)+.045*Math.sin(u*6.283*19);
 const noise=.055*Math.sin(x*.12+y*.16)+.025*Math.sin(x*.46-y*.33);
 const alpha=Math.max(0,Math.min(1,(boundary+noise-height)/.45+.48));
 const sy=Math.floor(y*2.6)%size,k=(sy*size+x)*3,o=(y*size+x)*3;
 for(let c=0;c<3;c++)mixed[o+c]=Math.round(stone[k+c]*(1-alpha)+soil[k+c]*alpha);
}
await sharp(mixed,{raw:{width:size,height:size,channels:3}}).jpeg({quality:94,chromaSubsampling:'4:4:4'}).toFile('textures/wall-blend.jpg');
for(const name of ['limestone','soil','wall-blend']){
 const {data,info}=await sharp(`textures/${name}.jpg`).resize(size,size).greyscale().raw().toBuffer({resolveWithObject:true});const w=info.width,h=info.height,out=Buffer.alloc(w*h*3),sample=(x,y)=>data[((y+h)%h)*w+(x+w)%w]/255;
 for(let y=0;y<h;y++)for(let x=0;x<w;x++){const dx=(sample(x+1,y)-sample(x-1,y))*2.4,dy=(sample(x,y+1)-sample(x,y-1))*2.4,d=Math.hypot(dx,dy,1),o=(y*w+x)*3;out[o]=Math.round((.5-dx/d*.5)*255);out[o+1]=Math.round((.5+dy/d*.5)*255);out[o+2]=Math.round((.5+1/d*.5)*255);}
 await sharp(out,{raw:{width:w,height:h,channels:3}}).jpeg({quality:98,chromaSubsampling:'4:4:4'}).toFile(`textures/${name}-normal.jpg`);
}
const b=await readFile(file),jl=b.readUInt32LE(12),j=JSON.parse(b.subarray(20,20+jl).toString());const binStart=20+jl+8;let bin=b.subarray(binStart,binStart+b.readUInt32LE(20+jl));
j.images=[];j.textures=[];j.samplers=[{magFilter:9729,minFilter:9987,wrapS:10497,wrapT:10497}];
for(const name of ['limestone','soil','limestone-normal','soil-normal','wall-blend','wall-blend-normal','bark','bark-normal','bark-rough','fur','fur-normal','fur-rough']){const image=await readFile(`textures/${name}.jpg`),pad=Buffer.alloc((4-bin.length%4)%4),offset=bin.length+pad.length;j.bufferViews.push({buffer:0,byteOffset:offset,byteLength:image.length});j.images.push({name,mimeType:'image/jpeg',bufferView:j.bufferViews.length-1});j.textures.push({sampler:0,source:j.images.length-1});bin=Buffer.concat([bin,pad,image]);}
for(const m of j.materials){let base=-1,normal=-1;
 if(/^(walls|front|crag)_/.test(m.name)){base=4;normal=5;}
 else if(/^(ceiling|columns|formations|debris)_/.test(m.name)){base=0;normal=2;}
 else if(/^(floor|wallFoot)_/.test(m.name)){base=1;normal=3;}
 if(/^timber_/.test(m.name)){base=6;normal=7;m.pbrMetallicRoughness.metallicRoughnessTexture={index:8};}
 if(/^hides_/.test(m.name)){base=9;normal=10;m.pbrMetallicRoughness.metallicRoughnessTexture={index:11};m.pbrMetallicRoughness.roughnessFactor=.95;}
 if(base>=0){m.pbrMetallicRoughness.baseColorTexture={index:base};m.normalTexture={index:normal,scale:.65};}
}
j.buffers[0].byteLength=bin.length;bin=Buffer.concat([bin,Buffer.alloc((4-bin.length%4)%4)]);let json=Buffer.from(JSON.stringify(j));json=Buffer.concat([json,Buffer.alloc((4-json.length%4)%4,32)]);const header=Buffer.alloc(20);header.write('glTF');header.writeUInt32LE(2,4);header.writeUInt32LE(12+8+json.length+8+bin.length,8);header.writeUInt32LE(json.length,12);header.writeUInt32LE(0x4e4f534a,16);const bh=Buffer.alloc(8);bh.writeUInt32LE(bin.length);bh.writeUInt32LE(0x004e4942,4);await writeFile(file,Buffer.concat([header,json,bh,bin]));const info=JSON.parse(await readFile('model/model-info.json','utf8'));info.bytes=header.readUInt32LE(8);info.textures=12;info.stage='refined review model v0.26';await writeFile('model/model-info.json',JSON.stringify(info,null,2));console.log(info);
