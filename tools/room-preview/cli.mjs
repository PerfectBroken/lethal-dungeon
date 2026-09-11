import fs from 'node:fs';
import path from 'node:path';
import {spawnSync,spawn} from 'node:child_process';
const root=path.resolve(import.meta.dirname,'../..');
const registry=JSON.parse(fs.readFileSync(path.join(root,'assets/rooms/registry.json')));
const [command='preview',roomId='corner_left',variantId]=process.argv.slice(2);
function run(args,cwd=root){const r=spawnSync(process.execPath,args,{cwd,stdio:'inherit'});if(r.status!==0)process.exit(r.status??1);}
if(command==='test'){
 run(['--test','tests/Assets.Tests/registry.test.mjs']);
 for(const v of registry.variants){const cwd=path.join(root,v.path);run(['--test',...fs.readdirSync(path.join(cwd,'tests')).filter(n=>n.endsWith('.test.mjs')).map(n=>'tests/'+n)],cwd);}
}else if(command==='preview'){
 const v=registry.variants.find(v=>v.roomId===roomId&&(!variantId||v.variantId===variantId));
 if(!v)throw new Error('Unknown room/variant. See assets/rooms/registry.json');
 const dir=path.join(root,'.work/room-preview',roomId),asset=path.join(root,v.path);
 fs.mkdirSync(dir,{recursive:true});
 fs.cpSync(path.join(root,'tools/room-preview/adapters',roomId),dir,{recursive:true});
 fs.mkdirSync(path.join(dir,'public'),{recursive:true});
 fs.copyFileSync(path.join(asset,'model/model-info.json'),path.join(dir,'model-info.json'));
 function link(from,to){fs.rmSync(to,{force:true,recursive:true});fs.symlinkSync(from,to);}
 link(path.join(asset,'source'),path.join(dir,'model'));
 for(const [from,to] of [['model','models'],['textures','textures'],['concept/reference.png','reference.png']])link(path.join(asset,from),path.join(dir,'public',to));
 const port=process.env.PORT||'3010';
 console.log(`${v.roomId}/${v.variantId}: http://localhost:${port}/ — ${asset}`);
 const child=spawn(process.execPath,[path.join(root,'node_modules/vite/bin/vite.js'),'--host','127.0.0.1','--port',port,'--strictPort'],{cwd:dir,stdio:'inherit'});
 for(const signal of ['SIGINT','SIGTERM'])process.on(signal,()=>child.kill(signal));
 child.on('exit',code=>process.exit(code??0));
}else throw new Error('Usage: node tools/room-preview/cli.mjs preview [roomId] [variantId] | test');
