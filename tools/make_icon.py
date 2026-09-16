import pathlib
import struct
import zlib
import math

W = H = 256


def lerp(a, b, t):
    return int(a + (b - a) * t)


pixels = bytearray(W * H * 3)
for y in range(H):
    for x in range(W):
        i = (y * W + x) * 3
        gy = y / (H - 1)
        pixels[i] = lerp(28, 18, gy)
        pixels[i + 1] = lerp(32, 24, gy)
        pixels[i + 2] = lerp(42, 30, gy)


def setp(x, y, r, g, b, a=1.0):
    if 0 <= x < W and 0 <= y < H and a > 0:
        i = (y * W + x) * 3
        pixels[i] = int(pixels[i] * (1 - a) + r * a)
        pixels[i + 1] = int(pixels[i + 1] * (1 - a) + g * a)
        pixels[i + 2] = int(pixels[i + 2] * (1 - a) + b * a)


cx, cy = 128, 118
for y in range(H):
    for x in range(W):
        dx = (x - cx) / 70.0
        dy = (y - cy) / 92.0
        if dy < -0.85:
            continue
        if abs(dx) <= 1.0 - max(0.0, (dy - 0.15) * 0.85) and dy <= 1.15:
            t = (dy + 1.0) / 2.2
            setp(x, y, lerp(196, 140, t), lerp(168, 96, t), lerp(72, 40, t), 1.0)

for y in range(H):
    for x in range(W):
        if math.hypot(x - cx, y - (cy - 8)) <= 18:
            setp(x, y, 72, 56, 36, 1.0)

for ox in (-22, 22):
    hx, hy = cx + ox, cy + 8
    for y in range(hy - 10, hy + 2):
        for x in range(hx - 6, hx + 7):
            if math.hypot(x - hx, y - (hy - 6)) <= 5:
                setp(x, y, 236, 228, 210, 1.0)
    for y in range(hy + 2, hy + 28):
        for x in range(hx - 4, hx + 5):
            setp(x, y, 236, 228, 210, 1.0)


def chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


raw = b"".join(b"\x00" + bytes(pixels[y * W * 3 : (y + 1) * W * 3]) for y in range(H))
png = (
    b"\x89PNG\r\n\x1a\n"
    + chunk(b"IHDR", struct.pack(">IIBBBBB", W, H, 8, 2, 0, 0, 0))
    + chunk(b"IDAT", zlib.compress(raw, 9))
    + chunk(b"IEND", b"")
)
path = pathlib.Path(__file__).resolve().parents[1] / "icon.png"
path.write_bytes(png)
print(f"wrote {path} {path.stat().st_size} bytes")
