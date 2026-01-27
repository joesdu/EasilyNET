/**
 * @easilynet/gridfs-sdk - Hash Worker
 * Web Worker for non-blocking SHA256 calculation using hash-wasm
 */

// SubtleCrypto fallback limit (64 MiB)
const SUBTLE_FALLBACK_MAX_BYTES = 64 * 1024 * 1024;

let hasher = null;
let processed = 0;
let total = 0;
let subtleChunks = [];

self.onmessage = async (event) => {
  const { type, chunk, size } = event.data;

  try {
    if (type === 'start') {
      total = size || 0;
      processed = 0;
      
      // Try to load hash-wasm
      try {
        importScripts('./lib/hash-wasm/sha256.umd.min.js');
        if (self.hashwasm) {
          hasher = await self.hashwasm.createSHA256();
          hasher.init();
        }
      } catch (e) {
        // Fallback to SubtleCrypto
      }

      if (!hasher) {
        subtleChunks = [];
        if (total > SUBTLE_FALLBACK_MAX_BYTES) {
          postMessage({
            type: 'error',
            error: `hash-wasm unavailable and SubtleCrypto fallback only supports files <= ${formatBytes(SUBTLE_FALLBACK_MAX_BYTES)} (current: ${formatBytes(total)}).`,
          });
          close();
          return;
        }
      }
      
      postMessage({
        type: 'ready',
        impl: hasher ? 'hash-wasm' : 'subtle',
        subtleLimit: SUBTLE_FALLBACK_MAX_BYTES,
      });
      return;
    }

    if (type === 'chunk') {
      // Ensure chunk is Uint8Array
      const data = chunk instanceof Uint8Array ? chunk : new Uint8Array(chunk);
      
      if (hasher) {
        hasher.update(data);
      } else {
        // For SubtleCrypto fallback, save a copy of the ArrayBuffer
        subtleChunks.push(data.buffer.slice(0));
      }
      
      processed += data.byteLength;
      postMessage({ type: 'progress', loaded: processed, total });
      return;
    }

    if (type === 'finalize') {
      let hex = '';
      
      if (hasher) {
        const digestResult = hasher.digest();
        hex = (typeof digestResult === 'string' ? digestResult : bufferToHex(digestResult)).toUpperCase();
      } else {
        // Fallback: Use SubtleCrypto (requires concatenating all chunks)
        const combined = concatBuffers(subtleChunks);
        const digest = await crypto.subtle.digest('SHA-256', combined);
        hex = bufferToHex(new Uint8Array(digest)).toUpperCase();
      }
      
      postMessage({ type: 'done', hash: hex });
      close();
    }
  } catch (err) {
    postMessage({ type: 'error', error: err?.message || String(err) });
    close();
  }
};

function concatBuffers(parts) {
  const totalLength = parts.reduce((sum, cur) => sum + cur.byteLength, 0);
  const merged = new Uint8Array(totalLength);
  let offset = 0;
  for (const part of parts) {
    merged.set(new Uint8Array(part), offset);
    offset += part.byteLength;
  }
  return merged.buffer;
}

function bufferToHex(bytes) {
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

function formatBytes(bytes) {
  if (!Number.isFinite(bytes)) return 'unknown';
  if (bytes === 0) return '0 B';
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB'];
  const idx = Math.floor(Math.log(bytes) / Math.log(1024));
  const value = bytes / Math.pow(1024, idx);
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[idx]}`;
}
