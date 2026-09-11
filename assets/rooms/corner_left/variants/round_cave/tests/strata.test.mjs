import test from 'node:test';
import assert from 'node:assert/strict';
import {createField} from '../source/room.mjs';
test('CL019 north wall has readable geometric bedding, not texture-only relief',()=>{
 const f=createField();const zs=[];
 for(let y=.5;y<=2.4;y+=.04){let a=1,b=3.9;for(let k=0;k<22;k++){const z=(a+b)/2;if(f(.7,y,z)>0)a=z;else b=z;}zs.push((a+b)/2);}
 // Remove broad cave curvature: measure excursions over 24cm vertical neighborhoods.
 const residual=zs.slice(3,-3).map((z,i)=>z-(zs[i]+zs[i+6])/2);
 assert.ok(Math.max(...residual)-Math.min(...residual)>.20,'north wall needs >20cm local bedding relief');
});
test('CL024 bedding also varies across the north face horizontally',()=>{const f=createField();const zs=[];for(let x=-.8;x<=2.2;x+=.04){let a=1,b=3.9;for(let k=0;k<22;k++){let z=(a+b)/2;if(f(x,1.5,z)>0)a=z;else b=z;}zs.push((a+b)/2);}const residual=zs.slice(4,-4).map((z,i)=>z-(zs[i]+zs[i+8])/2);assert.ok(Math.max(...residual)-Math.min(...residual)>.18,'ring relief must cross the horizontal section');});
