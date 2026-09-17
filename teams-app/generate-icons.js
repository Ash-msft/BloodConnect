// generate-icons.js
// Generates the two PNG icons required by a Microsoft Teams app manifest
// (a 192x192 color icon and a 32x32 transparent outline icon) using only
// Node's built-in zlib module — no image libraries or external assets.
//
// Run with: node generate-icons.js
'use strict'

const fs = require('fs')
const path = require('path')
const zlib = require('zlib')

function crc32(buf) {
  let c
  const table = crc32.table || (crc32.table = (() => {
    const t = new Uint32Array(256)
    for (let n = 0; n < 256; n++) {
      c = n
      for (let k = 0; k < 8; k++) {
        c = c & 1 ? 0xedb88320 ^ (c >>> 1) : c >>> 1
      }
      t[n] = c
    }
    return t
  })())
  let crc = 0xffffffff
  for (let i = 0; i < buf.length; i++) {
    crc = table[(crc ^ buf[i]) & 0xff] ^ (crc >>> 8)
  }
  return (crc ^ 0xffffffff) >>> 0
}

function chunk(type, data) {
  const typeBuf = Buffer.from(type, 'ascii')
  const lenBuf = Buffer.alloc(4)
  lenBuf.writeUInt32BE(data.length, 0)
  const crcBuf = Buffer.alloc(4)
  crcBuf.writeUInt32BE(crc32(Buffer.concat([typeBuf, data])), 0)
  return Buffer.concat([lenBuf, typeBuf, data, crcBuf])
}

/**
 * Encodes an RGBA pixel buffer (width * height * 4 bytes) as a PNG file.
 */
function encodePng(width, height, rgba) {
  const signature = Buffer.from([137, 80, 78, 71, 13, 10, 26, 10])

  const ihdr = Buffer.alloc(13)
  ihdr.writeUInt32BE(width, 0)
  ihdr.writeUInt32BE(height, 4)
  ihdr[8] = 8 // bit depth
  ihdr[9] = 6 // color type: RGBA
  ihdr[10] = 0 // compression
  ihdr[11] = 0 // filter
  ihdr[12] = 0 // interlace

  // Add a filter-type byte (0 = None) before each scanline, as PNG requires.
  const stride = width * 4
  const raw = Buffer.alloc((stride + 1) * height)
  for (let y = 0; y < height; y++) {
    raw[y * (stride + 1)] = 0
    rgba.copy(raw, y * (stride + 1) + 1, y * stride, y * stride + stride)
  }

  const idat = zlib.deflateSync(raw, { level: 9 })

  return Buffer.concat([
    signature,
    chunk('IHDR', ihdr),
    chunk('IDAT', idat),
    chunk('IEND', Buffer.alloc(0)),
  ])
}

/** Sets pixel (x, y) to an RGBA color in a flat pixel buffer. */
function setPixel(rgba, width, x, y, r, g, b, a) {
  if (x < 0 || y < 0 || x >= width) return
  const i = (y * width + x) * 4
  rgba[i] = r
  rgba[i + 1] = g
  rgba[i + 2] = b
  rgba[i + 3] = a
}

/** Draws a filled circle by testing each pixel against the circle equation. */
function fillCircle(rgba, width, height, cx, cy, radius, r, g, b, a) {
  const r2 = radius * radius
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const dx = x - cx
      const dy = y - cy
      if (dx * dx + dy * dy <= r2) {
        setPixel(rgba, width, x, y, r, g, b, a)
      }
    }
  }
}

/**
 * Draws a simple blood-drop silhouette: a circle with a triangular tip on top,
 * using the point-in-shape test (distance from circle center, or inside the
 * triangular tip region).
 */
function fillDrop(rgba, width, height, cx, cyCircle, radius, tipY, r, g, b, a) {
  const r2 = radius * radius
  for (let y = 0; y < height; y++) {
    for (let x = 0; x < width; x++) {
      const dx = x - cx
      const dyCircle = y - cyCircle
      const inCircle = dx * dx + dyCircle * dyCircle <= r2 && y >= cyCircle - radius
      // Triangular tip: linearly narrows from the circle's top width down to a point at tipY.
      let inTip = false
      if (y >= tipY && y <= cyCircle) {
        const t = (y - tipY) / (cyCircle - tipY) // 0 at tip, 1 at circle top
        const halfWidth = t * radius
        inTip = Math.abs(dx) <= halfWidth
      }
      if (inCircle || inTip) {
        setPixel(rgba, width, x, y, r, g, b, a)
      }
    }
  }
}

function buildColorIcon() {
  const size = 192
  const rgba = Buffer.alloc(size * size * 4, 0) // fully transparent background
  // Solid rounded background circle in BloodConnect brand red.
  fillCircle(rgba, size, size, size / 2, size / 2, size / 2, 0xb3, 0x1b, 0x1b, 255)
  // White blood-drop silhouette centered on top of the red circle.
  fillDrop(rgba, size, size, size / 2, size * 0.68, size * 0.24, size * 0.24, 255, 255, 255, 255)
  return encodePng(size, size, rgba)
}

function buildOutlineIcon() {
  // Outline icons must be a simple white silhouette on a fully transparent background,
  // sized 32x32, per Teams app manifest requirements.
  const size = 32
  const rgba = Buffer.alloc(size * size * 4, 0)
  fillDrop(rgba, size, size, size / 2, size * 0.66, size * 0.34, size * 0.1, 255, 255, 255, 255)
  return encodePng(size, size, rgba)
}

const outDir = __dirname
fs.writeFileSync(path.join(outDir, 'color.png'), buildColorIcon())
fs.writeFileSync(path.join(outDir, 'outline.png'), buildOutlineIcon())
console.log('Generated color.png (192x192) and outline.png (32x32) in', outDir)
