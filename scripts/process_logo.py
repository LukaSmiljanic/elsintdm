from PIL import Image
import os

src = r"C:\Users\PKS\.cursor\projects\c-Users-PKS-source-repos-SalonPro-Backend\assets\c__Users_PKS_AppData_Roaming_Cursor_User_workspaceStorage_6449116ebc088c0d665701169c39c3ac_images_imagesss-53a7c72c-5873-4d90-bb57-199f9da44774.png"
out_dir = r"c:\Users\PKS\source\repos\ElsInt\web\public\images\brand"
out_api = r"c:\Users\PKS\source\repos\ElsInt\src\ElsInt.Api\wwwroot\images\brand"
os.makedirs(out_dir, exist_ok=True)
os.makedirs(out_api, exist_ok=True)

img = Image.open(src).convert("RGBA")
pixels = img.load()
w, h = img.size

corners = [pixels[2, 2], pixels[w - 3, 2], pixels[2, h - 3], pixels[w - 3, h - 3]]
br = sum(c[0] for c in corners) // 4
bg = sum(c[1] for c in corners) // 4
bb = sum(c[2] for c in corners) // 4


def dist(p):
    return abs(p[0] - br) + abs(p[1] - bg) + abs(p[2] - bb)


for y in range(h):
    for x in range(w):
        r, g, b, a = pixels[x, y]
        d = dist((r, g, b))
        if d < 50:
            pixels[x, y] = (r, g, b, 0)
        elif d < 85:
            alpha = int(255 * (d - 50) / 35)
            pixels[x, y] = (r, g, b, max(0, min(255, alpha)))

bbox = img.getbbox()
if bbox:
    pad = 16
    l, t, r, b = bbox
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(w, r + pad)
    b = min(h, b + pad)
    img = img.crop((l, t, r, b))

# Slightly taller header-friendly max height via resize keeping aspect
max_h = 320
if img.height > max_h:
    ratio = max_h / img.height
    img = img.resize((int(img.width * ratio), max_h), Image.Resampling.LANCZOS)

path1 = os.path.join(out_dir, "elsint-logo.png")
path2 = os.path.join(out_api, "elsint-logo.png")
img.save(path1)
img.save(path2)
print("saved", path1, img.size)
