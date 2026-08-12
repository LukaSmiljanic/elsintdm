import { Jimp, intToRGBA } from 'jimp';
import path from 'path';
import fs from 'fs';

const src = String.raw`C:\Users\PKS\.cursor\projects\c-Users-PKS-source-repos-SalonPro-Backend\assets\c__Users_PKS_AppData_Roaming_Cursor_User_workspaceStorage_6449116ebc088c0d665701169c39c3ac_images_imagesss-53a7c72c-5873-4d90-bb57-199f9da44774.png`;
const outs = [
  String.raw`c:\Users\PKS\source\repos\ElsInt\web\public\images\brand\elsint-logo.png`,
  String.raw`c:\Users\PKS\source\repos\ElsInt\src\ElsInt.Api\wwwroot\images\brand\elsint-logo.png`,
];

const img = await Jimp.read(src);
const { r: br, g: bg, b: bb } = intToRGBA(img.getPixelColor(4, 4));

img.scan(0, 0, img.bitmap.width, img.bitmap.height, function (x, y, idx) {
  const r = this.bitmap.data[idx];
  const g = this.bitmap.data[idx + 1];
  const b = this.bitmap.data[idx + 2];
  const d = Math.abs(r - br) + Math.abs(g - bg) + Math.abs(b - bb);
  if (d < 55) {
    this.bitmap.data[idx + 3] = 0;
  } else if (d < 95) {
    this.bitmap.data[idx + 3] = Math.round(255 * ((d - 55) / 40));
  }
});

let minX = img.bitmap.width,
  minY = img.bitmap.height,
  maxX = 0,
  maxY = 0;
img.scan(0, 0, img.bitmap.width, img.bitmap.height, function (x, y, idx) {
  if (this.bitmap.data[idx + 3] > 20) {
    if (x < minX) minX = x;
    if (y < minY) minY = y;
    if (x > maxX) maxX = x;
    if (y > maxY) maxY = y;
  }
});

const pad = 18;
minX = Math.max(0, minX - pad);
minY = Math.max(0, minY - pad);
maxX = Math.min(img.bitmap.width - 1, maxX + pad);
maxY = Math.min(img.bitmap.height - 1, maxY + pad);
img.crop({ x: minX, y: minY, w: maxX - minX + 1, h: maxY - minY + 1 });

const targetH = 280;
const scale = targetH / img.bitmap.height;
img.resize({ w: Math.round(img.bitmap.width * scale), h: targetH });

for (const out of outs) {
  fs.mkdirSync(path.dirname(out), { recursive: true });
  await img.write(out);
}
console.log('done', img.bitmap.width, img.bitmap.height, 'bg', br, bg, bb);
