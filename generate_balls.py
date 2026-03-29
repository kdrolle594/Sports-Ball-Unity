"""
Sports ball sprites — cartoon style matching reference image.
Thick black outline, bold fill, subtle spherical highlight, clean details.
Renders at 512×512 (2× SS), scales to 256×256 output.
"""
import math, os
from PIL import Image, ImageDraw, ImageFont

OUT_DIR = "Balls"
os.makedirs(OUT_DIR, exist_ok=True)

S   = 512       # render canvas
C   = S // 2    # center = 256
R   = 228       # outer (outline) radius
OL  = 13        # outline thickness
RI  = R - OL    # inner fill radius = 215
OUT = 256       # output size

BOLD_FONT = "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf"


# ── Utilities ─────────────────────────────────────────────────────────────────

def new_canvas():
    return Image.new("RGBA", (S, S), (0, 0, 0, 0))


def apply_circle_clip(img, rx=R, ry=R):
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse([C-rx, C-ry, C+rx, C+ry], fill=255)
    img.putalpha(mask)
    return img.resize((OUT, OUT), Image.LANCZOS)


def outline_circle(d, rx=R, ry=R):
    d.ellipse([C-rx, C-ry, C+rx, C+ry], fill=(0, 0, 0, 255))


def fill_circle(d, color, r=RI):
    d.ellipse([C-r, C-r, C+r, C+r], fill=(*color, 255))


def highlight(img, r=RI):
    """Composite a soft specular highlight over img using a separate layer.
    Returns the new composited RGBA image."""
    layer = Image.new("RGBA", (S, S), (0, 0, 0, 0))
    ld    = ImageDraw.Draw(layer)
    hx = C - int(r * 0.26)
    hy = C - int(r * 0.28)
    hr = int(r * 0.17)
    for i in range(hr, 0, -2):
        a = int(62 * (1 - (i / hr) ** 1.7))
        ld.ellipse([hx-i, hy-i, hx+i, hy+i], fill=(255, 255, 255, a))
    return Image.alpha_composite(img, layer)


def polyline(d, fn, n, color, width):
    pts = [fn(i / n) for i in range(n + 1)]
    for i in range(len(pts) - 1):
        x0, y0 = pts[i]; x1, y1 = pts[i+1]
        if math.hypot(x1-x0, y1-y0) < 80:
            d.line([(x0, y0), (x1, y1)], fill=color, width=width)


def save(img, name, rx=R, ry=R):
    out = apply_circle_clip(img, rx, ry)
    out.save(f"{OUT_DIR}/{name}.png")
    print(f"  {name}.png")


# ── 1. Golf Ball ──────────────────────────────────────────────────────────────
def make_golf():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (248, 248, 248))
    sp, dr = 44, 10
    for row in range(-6, 7):
        for col in range(-6, 7):
            ox = (row % 2) * (sp // 2)
            x, y = C + col*sp + ox, C + row*sp
            if math.hypot(x-C, y-C) + dr < RI - 8:
                d.ellipse([x-dr, y-dr, x+dr, y+dr], fill=(205, 205, 205, 215))
    img = highlight(img)
    save(img, "GolfBall")


# ── 2. Pool / 8-Ball ─────────────────────────────────────────────────────────
def make_pool():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (15, 15, 15))

    # White circle (smaller than before)
    wr = 62
    d.ellipse([C-wr, C-wr, C+wr, C+wr], fill=(255, 255, 255, 255))

    # "8" using system bold font
    try:
        font = ImageFont.truetype(BOLD_FONT, 72)
        bbox = d.textbbox((0, 0), "8", font=font)
        tw, th = bbox[2] - bbox[0], bbox[3] - bbox[1]
        d.text((C - tw//2 - bbox[0], C - th//2 - bbox[1]), "8",
               font=font, fill=(15, 15, 15, 255))
    except Exception:
        # Fallback: draw "8" as two stacked rings
        for cy_off, or_, ir_ in [(-20, 22, 13), (20, 24, 14)]:
            d.ellipse([C-or_, C+cy_off-or_, C+or_, C+cy_off+or_], fill=(15,15,15))
            d.ellipse([C-ir_, C+cy_off-ir_, C+ir_, C+cy_off+ir_], fill=(255,255,255))

    img = highlight(img)
    save(img, "PoolBall")


# ── 3. Tennis Ball ────────────────────────────────────────────────────────────
def make_tennis():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (162, 210, 0))   # optic yellow-green

    sc, lw = (255, 255, 255, 235), 12
    # Tennis seam: two diagonal banana-curves rotated 45°
    # Use 155° sweep (not 180°) so the two arcs leave small gaps — no closed oval
    def s1(t):
        a = math.radians(192 + 155 * t)   # 192° → 347° (155° arc, gaps at ends)
        lx = RI * 0.70 * math.cos(a)
        ly = RI * 0.48 * math.sin(a)
        # Rotate -45° so arcs are diagonal
        x = C + int(lx * 0.707 + ly * 0.707)
        y = C + int(-lx * 0.707 + ly * 0.707)
        return (x, y)

    def s2(t):   # 180° rotation of s1
        x, y = s1(t)
        return (2*C - x, 2*C - y)

    polyline(d, s1, 500, sc, lw)
    polyline(d, s2, 500, sc, lw)
    img = highlight(img)
    save(img, "TennisBall")


# ── 4. Baseball ──────────────────────────────────────────────────────────────
def make_baseball():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (245, 238, 220))
    sc, lw, tw = (210, 35, 35, 245), 7, 5
    span = int(RI * 0.76)

    def larc(t):
        y = C - span + int(2*span*t)
        x = C - 38 - int(55 * math.sin(math.pi * t))
        return (x, y)

    def rarc(t):
        y = C - span + int(2*span*t)
        x = C + 38 + int(55 * math.sin(math.pi * t))
        return (x, y)

    n = 500
    for arc_fn in [larc, rarc]:
        pts = [arc_fn(i/n) for i in range(n+1)]
        for i in range(len(pts)-1):
            d.line([pts[i], pts[i+1]], fill=sc, width=lw)
        # Cross-stitch ticks
        for i in range(10, len(pts)-10, 30):
            x0, y0 = pts[i]; x1, y1 = pts[min(i+5, len(pts)-1)]
            dx, dy = x1-x0, y1-y0; L = math.hypot(dx, dy) or 1
            nx, ny = -dy/L*17, dx/L*17
            d.line([(int(x0-nx), int(y0-ny)), (int(x0+nx), int(y0+ny))],
                   fill=sc, width=tw)

    img = highlight(img)
    save(img, "Baseball")


# ── 5. Volleyball ─────────────────────────────────────────────────────────────
def make_volleyball():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (245, 245, 245))   # white base

    yellow = (238, 185, 15, 255)
    blue   = (28,  75, 185, 255)

    # Use pieslice for clean panel fills
    bb = [C-RI, C-RI, C+RI, C+RI]
    d.pieslice(bb, start=-55, end=100, fill=yellow)    # right/upper panel
    d.pieslice(bb, start=148, end=280, fill=blue)       # lower-left panel

    # Seam lines: draw the 4 radial spokes that form the panel boundaries
    lc, lw = (25, 25, 25, 230), 9
    for deg in [-55, 100, 148, 280]:
        a = math.radians(deg)
        d.line([(C, C),
                (C + int(RI * math.cos(a)), C + int(RI * math.sin(a)))],
               fill=lc, width=lw)

    img = highlight(img)
    save(img, "Volleyball")


# ── 6. Soccer Ball ───────────────────────────────────────────────────────────
def make_soccer():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (238, 238, 238))
    black = (20, 20, 20, 255)
    pr = 56   # slightly smaller for clearer separation

    def pent(cx, cy, r, rot=0):
        return [(cx + r*math.cos(math.radians(rot + i*72 - 90)),
                 cy + r*math.sin(math.radians(rot + i*72 - 90))) for i in range(5)]

    # Draw surrounding pentagons first, then center on top so it's always visible
    dist = 148   # farther out = more white gap around center
    for i in range(5):
        a = math.radians(i*72 - 90)
        px, py = C + dist*math.cos(a), C + dist*math.sin(a)
        if math.hypot(px-C, py-C) < RI - 10:
            d.polygon(pent(int(px), int(py), pr, rot=i*72+180), fill=black)
    d.polygon(pent(C, C, pr, rot=0), fill=black)   # center pentagon on top

    img = highlight(img)
    save(img, "SoccerBall")


# ── 7. American Football ─────────────────────────────────────────────────────
def make_football():
    """Horizontal oval. Uses separate oval mask (not circle)."""
    img = new_canvas(); d = ImageDraw.Draw(img)

    bw, bh = 240, 158   # semi-width, semi-height (horizontal oval)
    dark_br  = (95,  42,  6)
    mid_br   = (162, 78, 16)

    # Black outline
    d.ellipse([C-bw, C-bh, C+bw, C+bh], fill=(0, 0, 0, 255))
    # Fill
    iw, ih = bw - OL, bh - OL
    d.ellipse([C-iw, C-ih, C+iw, C+ih], fill=(*mid_br, 255))

    # White parallel seam stripes
    seam = (255, 255, 255, 235)
    for y_off in [-50, 50]:
        pts = []
        for x in range(C - iw + 14, C + iw - 14, 3):
            t = (x - C) / iw
            y = C + y_off + int(14 * (1 - t*t))
            pts.append((x, y))
        for i in range(len(pts)-1):
            d.line([pts[i], pts[i+1]], fill=seam, width=9)

    # Laces
    lace = (255, 255, 255, 245)
    ly0, ly1 = C - 38, C + 38
    d.line([(C, ly0), (C, ly1)], fill=lace, width=7)
    for y in range(ly0 + 12, ly1, 15):
        d.line([(C-22, y), (C+22, y)], fill=lace, width=7)

    # Oval mask
    mask = Image.new("L", (S, S), 0)
    ImageDraw.Draw(mask).ellipse([C-bw, C-bh, C+bw, C+bh], fill=255)
    img.putalpha(mask)
    # Composite highlight via layer (proper blending)
    img = highlight(img, r=ih)
    img.resize((OUT, OUT), Image.LANCZOS).save(f"{OUT_DIR}/AmericanFootball.png")
    print("  AmericanFootball.png")


# ── 8. Basketball ────────────────────────────────────────────────────────────
def make_basketball():
    img = new_canvas(); d = ImageDraw.Draw(img)
    outline_circle(d)
    fill_circle(d, (218, 96, 12))

    lc, lw = (28, 16, 6, 252), 11

    # 1. Horizontal equator
    def horiz(t):
        return (C - RI + 8 + int((2*RI-16)*t), C)

    # 2. Vertical seam — slight right bow
    def vert(t):
        y = C - RI + 8 + int((2*RI-16)*t)
        tn = (y - C) / RI
        x = C + int(14 * (1 - tn*tn))
        return (x, y)

    # 3. Left panel arc — always stays LEFT of center
    def left_arc(t):
        y  = C - int(RI*0.88) + int(RI*1.76*t)
        tn = (y - C) / RI
        # Minimum offset from center = RI*0.22, max = RI*0.22 + RI*0.42 at equator
        bow = int(RI * 0.22) + int(RI * 0.42 * (1 - tn*tn))
        return (C - bow, y)

    # 4. Right panel arc — mirror
    def right_arc(t):
        y  = C - int(RI*0.88) + int(RI*1.76*t)
        tn = (y - C) / RI
        bow = int(RI * 0.22) + int(RI * 0.42 * (1 - tn*tn))
        return (C + bow, y)

    for fn in [horiz, vert, left_arc, right_arc]:
        polyline(d, fn, 500, lc, lw)

    img = highlight(img)
    save(img, "Basketball")


# ── Main ──────────────────────────────────────────────────────────────────────
if __name__ == "__main__":
    print("Generating ball sprites…")
    make_golf()
    make_pool()
    make_tennis()
    make_baseball()
    make_volleyball()
    make_soccer()
    make_football()
    make_basketball()
    print(f"\nDone — all sprites in ./{OUT_DIR}/")
