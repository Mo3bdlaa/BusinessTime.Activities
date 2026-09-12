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

SIZE = 32
ACCENT = (31, 111, 178)      # the blue used by the activity icons
PAPER = (255, 255, 255)
MUTED = (126, 146, 166)      # the date marks

canvas = [[(0, 0, 0, 0)] * SIZE for _ in range(SIZE)]


def put(x, y, colour):
    if 0 <= x < SIZE and 0 <= y < SIZE:
        canvas[y][x] = colour + (255,)


def fill(x0, y0, x1, y1, colour):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(x, y, colour)


def outline(x0, y0, x1, y1, colour, weight=2):
    for i in range(weight):
        for x in range(x0, x1 + 1):
            put(x, y0 + i, colour)
            put(x, y1 - i, colour)
        for y in range(y0, y1 + 1):
            put(x0 + i, y, colour)
            put(x1 - i, y, colour)


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


def chunk(kind, payload):
    return (struct.pack(">I", len(payload)) + kind + payload +
            struct.pack(">I", zlib.crc32(kind + payload) & 0xFFFFFFFF))


raw = b"".join(
    b"\x00" + b"".join(struct.pack("BBBB", *pixel) for pixel in row)
    for row in canvas
)

png = (b"\x89PNG\r\n\x1a\n" +
       chunk(b"IHDR", struct.pack(">IIBBBBB", SIZE, SIZE, 8, 6, 0, 0, 0)) +
       chunk(b"IDAT", zlib.compress(raw, 9)) +
       chunk(b"IEND", b""))

target = Path(__file__).resolve().parent.parent / "src" / "BusinessTime.Activities.Wizard" / "Resources" / "calendar.png"
target.write_bytes(png)
print(f"wrote {target} ({len(png)} bytes, {SIZE}x{SIZE})")
