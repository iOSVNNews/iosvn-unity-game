#!/usr/bin/env node
'use strict';

// Original combat sprites: one full-body creature per monster ID, one weapon
// drawing per equipment ID, and one sigil per skill ID. No Telegram UI icons
// are copied into this set. Re-running preserves the commissioned boss sprites.
const fs = require('node:fs');
const path = require('node:path');
const zlib = require('node:zlib');
const C = require('../ipa_core/catalog');

const root = path.join(__dirname, '..', 'unity_project', 'Assets', 'Resources', 'CombatPixel');
const commissioned = new Set(['cuu_vi_ho', 'thanh_long_anh', 'da_lang', 'bach_ho_anh', 'hoa_ho']);
const palettes = {
  kim: ['#211c29', '#776338', '#c6a65d', '#f9e7aa', '#ffffff'],
  moc: ['#152b26', '#315d45', '#5eac6d', '#b7df88', '#eaffbc'],
  thuy: ['#122a3d', '#2b6387', '#56a9ca', '#b5e8e9', '#eafcff'],
  hoa: ['#2b1725', '#963d39', '#e77644', '#ffbf66', '#fff0ad'],
  tho: ['#322520', '#72513f', '#bc8961', '#e7c08c', '#fff1ba'],
  loi: ['#251d3b', '#604595', '#a076d4', '#d6b7ff', '#fff6ff'],
  phong: ['#183533', '#3e8473', '#70c5ad', '#b4ecd2', '#effff0'],
  bang: ['#1b3149', '#477aab', '#85c4dd', '#cdeef6', '#ffffff'],
  thien: ['#3a2730', '#a47864', '#ead287', '#fff1be', '#ffffff'],
  ma: ['#26182e', '#663454', '#bd4f76', '#ee90ab', '#ffe5eb'],
};

function hash(s) {
  let h = 2166136261;
  for (const ch of String(s)) h = Math.imul(h ^ ch.codePointAt(0), 16777619) >>> 0;
  return h >>> 0;
}
function norm(s) { return String(s || '').normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd').toLowerCase(); }
function rgb(hex) { return [1, 3, 5].map(i => parseInt(hex.slice(i, i + 2), 16)); }
function crc32(buf) {
  let crc = 0xffffffff;
  for (const byte of buf) { crc ^= byte; for (let i = 0; i < 8; i++) crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1)); }
  return (crc ^ 0xffffffff) >>> 0;
}
function chunk(type, data) {
  const label = Buffer.from(type); const n = Buffer.alloc(4); n.writeUInt32BE(data.length);
  const check = Buffer.alloc(4); check.writeUInt32BE(crc32(Buffer.concat([label, data])));
  return Buffer.concat([n, label, data, check]);
}
function savePng(filename, art) {
  const stride = art.size * 4;
  const scanlines = Buffer.alloc((stride + 1) * art.size);
  for (let y = 0; y < art.size; y++) art.data.copy(scanlines, y * (stride + 1) + 1, y * stride, (y + 1) * stride);
  const header = Buffer.alloc(13); header.writeUInt32BE(art.size, 0); header.writeUInt32BE(art.size, 4); header[8] = 8; header[9] = 6;
  fs.writeFileSync(filename, Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), chunk('IHDR', header), chunk('IDAT', zlib.deflateSync(scanlines, {level: 9})), chunk('IEND', Buffer.alloc(0))]));
}

class Art {
  constructor(size) { this.size = size; this.data = Buffer.alloc(size * size * 4); }
  dot(x, y, hex, alpha = 255) {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= this.size || y >= this.size) return;
    const i = (y * this.size + x) * 4; const c = rgb(hex);
    this.data[i] = c[0]; this.data[i+1] = c[1]; this.data[i+2] = c[2]; this.data[i+3] = alpha;
  }
  rect(x, y, w, h, c) { for(let yy=Math.round(y); yy<y+h; yy++) for(let xx=Math.round(x); xx<x+w; xx++) this.dot(xx,yy,c); }
  ellipse(cx, cy, rx, ry, c) {
    for(let y=Math.ceil(cy-ry); y<=cy+ry; y++) for(let x=Math.ceil(cx-rx); x<=cx+rx; x++)
      if(((x-cx)/rx)**2+((y-cy)/ry)**2<=1) this.dot(x,y,c);
  }
  line(x1,y1,x2,y2,w,c) {
    const steps=Math.max(Math.abs(x2-x1),Math.abs(y2-y1))*2+1;
    for(let i=0;i<=steps;i++) { const t=i/steps; this.ellipse(x1+(x2-x1)*t,y1+(y2-y1)*t,w/2,w/2,c); }
  }
  shards(cx,cy,rx,ry,count,seed,c) {
    for(let i=0;i<count;i++) { const a=((seed>>>((i%8)*4))%360+i*137)*Math.PI/180; const d=.25+((seed+i*31)%70)/100;
      const x=cx+Math.cos(a)*rx*d, y=cy+Math.sin(a)*ry*d;
      this.rect(x,y,1+(i%3),1+(i%2),c); }
  }
}

function family(m) {
  const t=norm(m.name+' '+m.id);
  if(/diep|nhen|trung|ret|ong|bo cap|kien|sau|buom/.test(t)) return 'insect';
  if(/cuu vi|ma ho|linh ho|ho yeu|huyen canh ho|thac nguyet ho|ho ly|cao/.test(t)) return 'fox';
  if(/long|giao|ky lan|linh lan/.test(t)) return 'dragon';
  if(/phuong|chim|dieu|hac|ung|loan|khong tuoc|qua den|hoa nha/.test(t)) return 'bird';
  if(/xa|ran|mang|luon/.test(t)) return 'serpent';
  if(/ca |ngac|hai sam|thuy linh|rua nuoc/.test(t)) return /rua/.test(t)?'turtle':'aquatic';
  if(/tinh|moc|thao|hoa diep/.test(t)) return 'plant';
  if(/thach|vien|khoi loi|cu nhan|son nhac/.test(t)) return 'golem';
  if(/u hon|oan hon|ma linh|ma chu|quy|tu la|tien quan|chien linh/.test(t)) return 'wraith';
  if(/ho |bach ho|soi|lang|bao|gau|thu|huu|loc|nguu|chuot|meo/.test(t)) return 'beast';
  return ['beast','wraith','golem','bird','dragon'][hash(m.id)%5];
}

function creature(m) {
  const a=new Art(96), seed=hash(m.id), p=palettes[m.element]||palettes.ma, kind=family(m);
  const v=(shift,mod)=>((seed>>>shift)%mod), horn=4+v(2,7), bodyY=48+v(6,7)-3;
  const dark=p[0], shade=p[1], body=p[2], light=p[3], glint=p[4];
  // Every ID changes proportions, appendages, markings, and a runic signature.
  if(kind==='dragon' || kind==='serpent') {
    const serpent=kind==='serpent';
    const pts=[[8,57],[18,bodyY+10],[27,bodyY-6],[39,bodyY+7],[48,bodyY-10],[62,bodyY+2],[72,bodyY-13]];
    for(let j=0;j<pts.length-1;j++) { const q=pts[j],r=pts[j+1]; a.line(...q,...r,serpent?15:18,dark); a.line(...q,...r,serpent?11:14,shade); a.line(q[0],q[1]-3,r[0],r[1]-3,3,body); }
    a.ellipse(77,bodyY-16,serpent?12:15,serpent?10:12,dark); a.ellipse(77,bodyY-18,11,8,body);
    a.line(79,bodyY-13,91,bodyY-11,5,dark); a.line(81,bodyY-15,90,bodyY-13,2,light);
    if(!serpent) for(const [x,y] of [[27,bodyY+2],[56,bodyY+1]]) { a.line(x,y,x-4,bodyY+21,8,dark); a.line(x+2,y,x+1,bodyY+21,5,body); for(let c=0;c<3;c++) a.line(x-5+c*3,bodyY+21,x-7+c*3,bodyY+25,2,glint); }
    for(let j=0;j<9+v(12,7);j++) { const x=13+j*6,y=bodyY+Math.sin(j*1.4)*8-7; a.line(x,y,x-2,y-4-v(j%12,4),2,j%3?light:glint); }
    for(let j=0;j<(serpent?2:3);j++) a.line(72+j*5,bodyY-23,71+j*5-horn/2,bodyY-27-horn-j%2*3,3,glint);
    a.dot(82,bodyY-20,glint); a.dot(83,bodyY-20,dark);
    if(serpent) { a.line(84,bodyY-8,89,bodyY-3,2,glint); a.line(89,bodyY-3,92,bodyY-6,1,glint); }
  } else if(kind==='bird' || kind==='insect') {
    const insect=kind==='insect', wingSpan=18+v(7,11);
    a.ellipse(46,bodyY+4,17,13,dark); a.ellipse(47,bodyY+2,13,10,body);
    for(let side of [-1,1]) { const bx=46+side*11, tip=46+side*wingSpan;
      a.line(bx,bodyY-3,tip,bodyY-25-v(10,9),10,dark); a.line(bx,bodyY-5,tip,bodyY-26-v(10,9),6,shade);
      for(let f=0;f<5;f++) a.line(tip-side*f*2,bodyY-20+f*3,tip+side*(6+f),bodyY-9+f*3,3,f%2?body:light);
      if(insect) { a.line(bx,bodyY+7,bx+side*12,bodyY+24,3,dark); a.line(bx+side*12,bodyY+24,bx+side*18,bodyY+20,2,glint); }
    }
    a.ellipse(62,bodyY-5,10,8,dark); a.ellipse(63,bodyY-7,7,6,body);
    if(insect) { a.line(66,bodyY-13,70,bodyY-23,2,glint); a.line(62,bodyY-13,59,bodyY-23,2,glint); }
    else { a.line(69,bodyY-5,78,bodyY-1,5,glint); for(let f=0;f<5;f++) a.line(30+f*3,bodyY+8,20-f*2,bodyY+14+f*2,2,shade); }
    a.dot(67,bodyY-8,glint); a.dot(68,bodyY-8,dark);
  } else if(kind==='wraith' || kind==='plant') {
    const plant=kind==='plant';
    a.ellipse(48,bodyY+1,20,22,dark); a.ellipse(48,bodyY,16,19,shade);
    for(let i=0;i<9;i++) { const x=29+i*5; a.line(x,bodyY+10,x-4+v(i%12,8),bodyY+25+v(i%10,8),3,i%2?shade:body); }
    a.ellipse(49,bodyY-15,12,12,dark); a.ellipse(49,bodyY-17,9,9,plant?body:shade);
    if(plant) { for(let i=0;i<6;i++) { const ang=i*Math.PI/3+v(3,4)*.13; const x=49+Math.cos(ang)*18,y=bodyY-17+Math.sin(ang)*17; a.ellipse(x,y,5+v(i%8,3),9,body); a.line(49,bodyY-17,x,y,2,light); } }
    else { a.line(35,bodyY-2,16,bodyY+12,6,dark); a.line(61,bodyY-2,77,bodyY+7,6,dark); a.line(41,bodyY-26,39-horn,bodyY-38,3,glint); a.line(56,bodyY-26,58+horn,bodyY-38,3,glint); }
    a.rect(43,bodyY-19,3,2,glint); a.rect(53,bodyY-19,3,2,glint);
  } else if(kind==='golem' || kind==='turtle') {
    const turtle=kind==='turtle';
    a.ellipse(46,bodyY+1,turtle?23:20,turtle?17:19,dark); a.ellipse(46,bodyY-1,turtle?19:16,turtle?14:15,shade);
    for(let i=0;i<6;i++) { const x=31+(i%3)*11,y=bodyY-12+Math.floor(i/3)*11; a.rect(x,y,8,7,i%2?body:shade); a.line(x,y,x+7,y,1,light); }
    for(let x of [28,58]) for(let d of [-1,1]) { a.line(x,bodyY+7,x+d*6,bodyY+23,8,dark); a.line(x,bodyY+7,x+d*6,bodyY+21,5,body); }
    a.ellipse(71,bodyY-10,11,9,dark); a.ellipse(72,bodyY-11,8,6,body);
    a.rect(74,bodyY-14,3,2,glint); if(!turtle) for(let i=0;i<3;i++) a.line(30+i*12,bodyY-17,27+i*12,bodyY-23-v(i%8,5),3,light);
  } else if(kind==='aquatic') {
    a.ellipse(48,bodyY,25,13,dark); a.ellipse(49,bodyY-2,21,10,body);
    a.line(29,bodyY-1,12,bodyY-18,8,dark); a.line(29,bodyY+2,12,bodyY+18,8,dark);
    for(let i=0;i<4;i++) a.line(38+i*9,bodyY-11,42+i*9,bodyY-22-v(i%10,5),3,light);
    a.line(66,bodyY+3,84,bodyY+6,6,dark); a.rect(68,bodyY-5,3,2,glint);
  } else {
    const fox=kind==='fox', tailCount=fox?(/cuu_vi/.test(m.id)?9:2+v(6,4)):1;
    const bodyX=45+v(9,5)-2;
    a.ellipse(bodyX,bodyY,24,14,dark); a.ellipse(bodyX,bodyY-2,20,11,shade); a.ellipse(bodyX+6,bodyY-5,10,7,body);
    for(let leg=0;leg<4;leg++) { const x=28+leg*10,y=bodyY+8+(leg%2)*2;
      a.line(x,y,x+(leg%2?3:-3),bodyY+26,6,dark); a.line(x+1,y,x+(leg%2?4:-2),bodyY+24,3,body);
      for(let claw=0;claw<2;claw++) a.line(x+(leg%2?3:-3)+claw*2,bodyY+25,x+(leg%2?4:-2)+claw*2,bodyY+28,1,glint);
    }
    if(fox) for(let t=0;t<tailCount;t++) { const y=bodyY-10-t*3, tipY=13+t*6;
      a.line(27,bodyY-3,8+t%3*3,y,8,dark); a.line(8+t%3*3,y,8+(t%4)*3,tipY,7,shade);
      a.line(8+(t%4)*3,tipY,10+(t%4)*3,tipY-4,3,glint); }
    else { a.line(24,bodyY-4,9,bodyY-13-v(4,10),7,dark); a.line(20,bodyY-7,10,bodyY-14-v(4,10),3,body); }
    a.ellipse(70,bodyY-13,13,10,dark); a.ellipse(72,bodyY-15,10,7,body);
    for(let e=0;e<2;e++) { const x=65+e*12; a.line(x,bodyY-20,x+(e?2:-2),bodyY-30-horn,5,dark); a.line(x,bodyY-22,x+(e?2:-2),bodyY-30-horn,2,light); }
    a.line(78,bodyY-10,89,bodyY-7,5,dark); a.line(78,bodyY-12,88,bodyY-9,2,light);
    a.rect(76,bodyY-18,3,2,glint); a.dot(78,bodyY-18,dark); a.line(84,bodyY-3,87,bodyY+1,2,glint);
  }
  // Scales, fur strokes, crystals and unique dao markings differ by ID.
  a.shards(46,bodyY,18,11,18+v(16,13),seed,light);
  for(let i=0;i<7;i++) { const x=30+i*5+v(i%16,3),y=bodyY-7+v((i+3)%16,12);
    a.line(x,y,x+((seed>>>(i+2))&3)-1,y+2,1,i%3?dark:glint); }
  for(let i=0;i<5;i++) { const x=17+v(i%24,64),y=15+v((i+8)%24,57);
    if(a.data[(Math.round(y)*96+Math.round(x))*4+3]===0) { a.dot(x,y,i%2?glint:light,200); a.dot(x+1,y,glint,160); } }
  return a;
}

function weapon(w) {
  const a=new Art(64), seed=hash(w.id), t=w.wtype||'kiem', p=palettes[w.element]||palettes[w.tier==='tien'?'ma':'kim'];
  const [dark,shade,body,light,glint]=p, variation=(n,m)=>(seed>>>n)%m;
  const line=(x,y,X,Y,width,color)=>a.line(x,y,X,Y,width,color);
  if(t==='kiem' || w.slot==='phi_kiem' || t==='phapkhi') {
    const width=3+variation(3,4), curve=variation(8,7)-3;
    line(17,47,47+curve,9,width+4,dark); line(18,46,47+curve,9,width,body); line(22,40,47+curve,9,2,glint);
    line(14,47,23,53,5,dark); line(14,47,23,53,2,light); line(15,46,7,57,4,shade); a.ellipse(7,57,3,3,glint);
    if(t==='phapkhi') for(let i=0;i<4;i++) a.ellipse(27+i*5,25-i*4,2,2,i%2?glint:body);
  } else if(t==='bua' || t==='trongkhi') {
    line(15,55,39,13,7,dark); line(17,54,40,14,4,shade); line(19,48,35,20,1,light);
    const large=t==='trongkhi'; a.rect(25,7,large?29:25,large?19:15,dark); a.rect(28,10,large?23:19,large?13:9,body);
    a.line(30,11,45,11,2,light); a.rect(35,14,4+variation(7,5),4,glint);
  } else if(t==='quyensao') {
    a.ellipse(31,29,20,15,dark); a.ellipse(31,28,16,11,shade);
    for(let i=0;i<4;i++) { a.rect(15+i*8,16-variation(i+1,4),7,12,dark); a.rect(17+i*8,18,4,7,body); }
    a.rect(19,37,26,14,dark); a.rect(22,39,20,10,body);
    line(24,43,39,43,2,light);
  } else if(t==='dinh') {
    a.ellipse(32,30,22,16,dark); a.ellipse(32,29,18,12,body); a.rect(13,23,38,4,shade);
    for(let x of [18,40]) { a.rect(x,41,6,12,dark); a.rect(x+1,43,4,9,shade); }
    for(let x of [17,43]) line(x,20,x,10,4,glint);
    a.ellipse(32,24,4+variation(5,4),4,glint);
  } else if(t==='but') {
    line(13,55,45,11,7,dark); line(15,53,46,11,4,body); line(16,51,44,14,1,light);
    a.ellipse(46,11,5,9,shade); a.ellipse(47,7,3,6,glint); a.rect(24,34,5,3,glint);
  } else {
    line(15,55,42,9,7,dark); line(17,53,43,10,4,body); a.ellipse(43,10,8,8,light);
  }
  // Forging differences: blade serrations, grip wrap and a unique gemstone seal.
  for(let i=0;i<5;i++) { const x=16+i*5,y=49-i*6; a.rect(x,y,1+variation(i%10,3),2,i%2?glint:shade); }
  a.ellipse(31+variation(4,9)-4,30+variation(13,9)-4,2+variation(18,3),2,glint);
  return a;
}

function skill(s) {
  const a=new Art(64),seed=hash(s.id),text=norm(s.name+' '+s.id),element=Object.keys(palettes).find(k=>text.includes(k))||({kiem:'kim',phap:'loi',dan:'hoa',thu:'moc'}[s.mon])||'ma';
  const p=palettes[element], [dark,shade,body,light,glint]=p;
  const radius=17+(seed%7);
  for(let ray=0;ray<12;ray++) { const theta=(ray*30+(seed%19))*Math.PI/180;
    const x=32+Math.cos(theta)*radius,y=32+Math.sin(theta)*radius;
    a.line(x,y,32+Math.cos(theta)*(radius+7+(seed>>>ray)%5),32+Math.sin(theta)*(radius+7+(seed>>>ray)%5),2,ray%3?body:glint); }
  a.ellipse(32,32,16,16,dark); a.ellipse(32,32,13,13,shade);
  if(/kiem|tram|kiem khi/.test(text)) { a.line(21,46,45,18,6,dark); a.line(23,44,44,18,3,glint); a.line(20,41,27,47,3,body); }
  else if(/loi|dien/.test(text)) { a.line(33,14,26,32,6,glint); a.line(26,32,38,29,5,glint); a.line(38,29,29,50,6,glint); }
  else if(/hoa|viem/.test(text)) { for(let i=0;i<5;i++) a.line(25+i*4,44,24+i*4+(i%2?4:-3),19+(seed>>>i)%9,4,i%2?body:glint); }
  else if(/bang|thuy/.test(text)) { for(let i=0;i<6;i++) { const q=i*Math.PI/3; a.line(32,32,32+Math.cos(q)*14,32+Math.sin(q)*14,3,light); } }
  else if(/phu|an|tran/.test(text)) { a.rect(24,18,17,29,light); a.rect(26,20,13,25,dark); a.line(29,23,37,40,2,glint); a.line(37,23,29,40,2,body); }
  else { for(let i=0;i<7;i++) { const q=i*2.4; a.ellipse(32+Math.cos(q)*8,32+Math.sin(q)*8,3,3,i%2?light:glint); } }
  // The central seal is based on the entire ID; no two skill bitmaps share it.
  for(let i=0;i<12;i++) { const x=26+i%4*4,y=26+Math.floor(i/4)*5;
    if((seed>>>(i%24))&1) a.rect(x,y,2+(i%2),2,i%3?glint:body); }
  return a;
}

function item(record) {
  const a=new Art(64),seed=hash(record.id),t=norm(record.name+' '+record.id),kind=record.kind||'',p=palettes[record.element]||palettes[record.tier]||palettes[Object.keys(palettes)[seed%10]];
  const [dark,shade,body,light,glint]=p, v=(n,m)=>(seed>>>n)%m;
  const type=/giap|bao y|ao |armor/.test(t)?'armor'
    :/nhan|vong|ring/.test(t)?'ring'
      :/dan|hoan|pill/.test(t)?'pill'
        :/thao|hoa|la |herb/.test(t)?'herb'
          :/quang|thiet|thach|ore/.test(t)?'ore'
            :/phu|lenh|talisman/.test(t)?'talisman'
              :/kinh|quyen|sach|book/.test(t)?'book'
                :/ho lo|binh|gourd/.test(t)?'gourd'
                  :/dinh|lo |vessel/.test(t)?'vessel'
                    :kind==='mat'?'gem':'relic';
  if(type==='armor') {
    a.line(20,16,43,16,9,dark); a.rect(16,22,32,23,dark); a.rect(20,24,24,19,body);
    a.line(17,22,10,39,7,shade); a.line(47,22,54,39,7,shade); a.rect(24,42,16,13,dark);
    for(let i=0;i<4;i++) a.line(21,27+i*5,43,27+i*5,2,i%2?light:shade);
  } else if(type==='ring') {
    for(let deg=0;deg<360;deg+=4) { const q=deg*Math.PI/180; a.ellipse(32+Math.cos(q)*15,36+Math.sin(q)*12,3,3,dark); a.dot(32+Math.cos(q)*15,36+Math.sin(q)*12,light); }
    a.ellipse(32,17,7+v(3,3),8,dark); a.ellipse(32,16,5,6,body);
  } else if(type==='pill'||type==='gem') {
    const r=type==='pill'?13:18; a.ellipse(32,33,r+3,r+3,dark); a.ellipse(32,31,r,r,shade); a.ellipse(29,27,r-5,r-6,body);
    a.line(21,38,41,23,3,light); a.ellipse(39,21,3,3,glint);
    if(type==='gem') for(let i=0;i<5;i++) a.line(32,32,15+i*8,13+i%2*5,2,glint);
  } else if(type==='herb') {
    a.line(32,56,32,18,4,dark); a.line(33,53,33,20,2,body);
    for(let i=0;i<5;i++) { const y=49-i*8,side=i%2?-1:1;
      a.line(32,y,32+side*(14+v(i+2,6)),y-10,4,shade); a.ellipse(32+side*(13+v(i+2,6)),y-10,7,4,body); a.line(33,y-2,32+side*15,y-10,1,light); }
    a.ellipse(33,17,5+v(5,4),6,glint);
  } else if(type==='ore') {
    a.ellipse(32,40,20,13,dark); a.ellipse(31,38,16,10,shade);
    for(let i=0;i<6;i++) { const x=19+i*5; a.line(x,38,x+v(i+5,8)-3,14+v(i+9,15),6,dark); a.line(x+1,35,x+v(i+5,8)-2,17+v(i+9,15),3,i%2?body:light); }
  } else if(type==='talisman'||type==='book') {
    a.rect(16,9,32,46,dark); a.rect(19,11,26,42,type==='book'?shade:light);
    if(type==='book') { a.line(32,11,32,52,2,dark); a.rect(20,15,11,3,glint); a.rect(34,15,10,3,glint); }
    for(let i=0;i<9;i++) { const x=23+v(i+2,18),y=19+i*3; a.rect(x,y,2+v(i+9,4),2,i%3?body:glint); }
    a.rect(15,48,34,4,shade);
  } else if(type==='gourd'||type==='vessel') {
    a.ellipse(32,40,18,15,dark); a.ellipse(31,39,14,12,body); a.ellipse(32,22,type==='gourd'?9:15,10,dark); a.ellipse(31,21,type==='gourd'?7:12,7,shade);
    a.rect(27,8,10,7,glint); a.line(19,37,43,37,3,light);
    if(type==='vessel') for(let x of [15,45]) a.line(x,25,x,49,3,glint);
  } else {
    a.ellipse(32,34,19,19,dark); a.ellipse(31,33,15,15,body);
    for(let i=0;i<6;i++) { const q=i*Math.PI/3; a.line(32,34,32+Math.cos(q)*16,34+Math.sin(q)*16,3,i%2?shade:glint); }
  }
  // A species/recipe seal and irregular marks keep each object individually legible.
  for(let i=0;i<9;i++) { const x=23+i%3*6+v(i+5,3),y=28+Math.floor(i/3)*5+v(i+11,3);
    if((seed>>>i)&1) a.rect(x,y,1+(i%3),1+(i%2),i%2?glint:dark); }
  for(let i=0;i<4;i++) { const x=12+v(i+5,43),y=10+v(i+14,45); a.dot(x,y,i%2?light:glint,220); }
  return a;
}

function generate() {
  const monsterDir=path.join(root,'Monsters'), weaponDir=path.join(root,'Weapons'), skillDir=path.join(root,'Skills'), itemDir=path.join(root,'Items');
  for(const dir of [monsterDir,weaponDir,skillDir,itemDir]) fs.mkdirSync(dir,{recursive:true});
  for(const id of commissioned) if(!fs.existsSync(path.join(monsterDir,id+'.png'))) throw new Error('Missing commissioned monster art: '+id);
  let monsters=0,weapons=0,skills=0,items=0;
  for(const m of C.MONSTERS) { if(commissioned.has(m.id)) continue; savePng(path.join(monsterDir,m.id+'.png'),creature(m)); monsters++; }
  for(const w of C.EQUIPMENT.filter(x=>x.slot==='weapon'||x.slot==='phi_kiem'||x.wtype)) { savePng(path.join(weaponDir,w.id+'.png'),weapon(w)); weapons++; }
  for(const s of C.SKILLS) { savePng(path.join(skillDir,s.id+'.png'),skill(s)); skills++; }
  for(const m of C.MONSTERS) { savePng(path.join(skillDir,'quai_'+m.id+'.png'),skill({id:'quai_'+m.id,name:m.name+' '+m.element,mon:m.element})); skills++; }
  const metadata=JSON.parse(fs.readFileSync(path.join(__dirname,'..','unity_project','Assets','Resources','OfflineHuntCatalog.json'),'utf8'));
  const byId=new Map((metadata.items||[]).map(x=>[x.id,x]));
  const oldItems=path.join(root,'..','PixelArt','Items');
  for(const file of fs.readdirSync(oldItems).filter(x=>x.endsWith('.png'))) {
    const id=file.slice(0,-4);
    if(fs.existsSync(path.join(weaponDir,file))||fs.existsSync(path.join(skillDir,file))) continue;
    savePng(path.join(itemDir,file),item(byId.get(id)||{id,name:id})); items++;
  }
  console.log(JSON.stringify({monsters, commissioned: [...commissioned], weapons, skills, items}));
}
generate();
