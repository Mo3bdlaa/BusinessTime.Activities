#!/usr/bin/env python3
"""
Draws the calendar icon shown on the ribbon button.

The icon is a tiny PNG written by hand rather than exported from a drawing tool, so that it can be
regenerated anywhere the repository builds, without an imaging library. Run it after changing anything
here and commit the result:

    python3 tools/make-icon.py
"""
import struct
import zlib
from pathlib import Path

ACCENT = (31, 111, 178)      # the blue used by the activity icons
PAPER = (255, 255, 255)
MUTED = (126, 146, 166)      # the date marks

def draw(size):
    """The same drawing at any size: every coordinate is given on a 32 unit grid and scaled."""
    scale = size / 32
    canvas = [[(0, 0, 0, 0)] * size for _ in range(size)]

    def put(x, y, colour):
        if 0 <= x < size and 0 <= y < size:
            canvas[y][x] = colour + (255,)

    def fill(x0, y0, x1, y1, colour):
        for y in range(round(y0 * scale), round((y1 + 1) * scale)):
            for x in range(round(x0 * scale), round((x1 + 1) * scale)):
                put(x, y, colour)

    def outline(x0, y0, x1, y1, colour, weight=2):
        thickness = max(1, round(weight * scale))
        for i in range(thickness):
            fill(x0, y0 + i / scale, x1, y0 + i / scale, colour)
            fill(x0, y1 - i / scale, x1, y1 - i / scale, colour)
            fill(x0 + i / scale, y0, x0 + i / scale, y1, colour)
            fill(x1 - i / scale, y0, x1 - i / scale, y1, colour)

    # The two rings the page hangs from.
    fill(9, 2, 11, 8, ACCENT)
    fill(20, 2, 22, 8, ACCENT)

    # The page itself, with the darker band across the top.
    fill(3, 6, 28, 29, PAPER)
    outline(3, 6, 28, 29, ACCENT)
    fill(3, 6, 28, 13, ACCENT)

    # Three rows of dates, with one picked out.
    for row, y in enumerate((17, 21, 25)):
        for column, x in enumerate((7, 13, 19, 24)):
            if y == 25 and x > 19:
                continue
            colour = ACCENT if (row, column) == (1, 1) else MUTED
            fill(x, y - 1, x + 2, y + 1, colour)

    return canvas


def chunk(kind, payload):
    return (struct.pack(">I", len(payload)) + kind + payload +
            struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF))


def write(size, target):
    canvas = draw(size)
    raw = b"".join(
        b"\x00" + b"".join(struct.pack("BBBB", *pixel) for pixel in row)
        for row in canvas
    )
    png = (b"\x89PNG\r\n\x1a\n" +
           chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)) +
           chunk(b"IDAT", zlib.compress(raw, 9)) +
           chunk(b"IEND", b""))
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(png)
    print(f"wrote {target} ({len(png)} bytes, {size}x{size})")


root = Path(__file__).resolve().parent.parent

# The ribbon button, and the larger one the package itself is listed with.
write(32, root / "src" / "BusinessTime.Activities.Wizard" / "Resources" / "calendar.png")
write(128, root / "src" / "BusinessTime.Activities" / "package-icon.png")
