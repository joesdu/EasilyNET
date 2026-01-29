/**
 * @easilynet/gridfs-sdk - Hash Worker
 * Web Worker for non-blocking SHA256 calculation using hash-wasm
 * 
 * Performance optimizations:
 * - Uses hash-wasm (WebAssembly) for fast incremental hashing
 * - Processes large chunks (4MB+) to reduce message passing overhead
 * - Falls back to SubtleCrypto for small files when hash-wasm unavailable
 */

// SubtleCrypto fallback limit - only for small files since it requires full file in memory
const SUBTLE_FALLBACK_MAX_BYTES = 100 * 1024 * 1024; // 100 MiB

let hasher = null;
let processed = 0;
let total = 0;
let subtleChunks = [];

// Throttle progress updates to reduce main thread communication
let lastProgressUpdate = 0;
const PROGRESS_THROTTLE_MS = 50; // Update at most every 50ms

self.onmessage = async (event) => {
  const { type, chunk, size } = event.data;

  try {
    if (type === 'start') {
      total = size || 0;
      processed = 0;
      lastProgressUpdate = 0;
      hasher = null;
      subtleChunks = [];
      
      // Try to load hash-wasm (WebAssembly-based, very fast)
      try {
        // Load from lib directory relative to this worker file
        importScripts('./lib/sha256.umd.min.js');
        if (self.hashwasm) {
          hasher = await self.hashwasm.createSHA256();
          hasher.init();
        }
      } catch (e) {
        // hash-wasm not available, will use fallback
        console.warn('hash-wasm not available:', e.message);
      }

      if (!hasher) {
        // Check file size limit for SubtleCrypto fallback
        if (total > SUBTLE_FALLBACK_MAX_BYTES) {
          postMessage({
            type: 'error',
            error: `hash-wasm unavailable and SubtleCrypto fallback only supports files <= ${formatBytes(SUBTLE_FALLBACK_MAX_BYTES)} (current: ${formatBytes(total)}). Please ensure hash-wasm library is available at ./lib/sha256.umd.min.js`,
          });
          close();
          return;
        }
        subtleChunks = [];
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
        // hash-wasm incremental update - very fast
        hasher.update(data);
      } else {
        // SubtleCrypto fallback - need to collect all chunks
        // Store a copy since the original buffer may be transferred
        subtleChunks.push(data.slice().buffer);
      }
      
      processed += data.byteLength;
      
      // Throttle progress updates to reduce overhead
      const now = performance.now();
      if (now - lastProgressUpdate >= PROGRESS_THROTTLE_MS || processed >= total) {
        postMessage({ type: 'progress', loaded: processed, total });
        lastProgressUpdate = now;
      }
      return;
    }

    if (type === 'finalize') {
      let hex = '';
      
      if (hasher) {
        // hash-wasm finalization
        const digestResult = hasher.digest();
        hex = (typeof digestResult === 'string' ? digestResult : bufferToHex(digestResult)).toUpperCase();
      } else {
        // SubtleCrypto fallback - concatenate all chunks and hash
        const combined = concatBuffers(subtleChunks);
        const digest = await crypto.subtle.digest('SHA-256', combined);
        hex = bufferToHex(new Uint8Array(digest)).toUpperCase();
        // Clear memory
        subtleChunks = [];
      }
      
      postMessage({ type: 'done', hash: hex });
      close();
    }
  } catch (err) {
    postMessage({ type: 'error', error: err?.message || String(err) });
    close();
  }
};

/**
 * Concatenate multiple ArrayBuffers into one
 */
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

/**
 * Convert Uint8Array to hex string
 */
function bufferToHex(bytes) {
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('');
}

/**
 * Format bytes to human-readable string
 */
function formatBytes(bytes) {
  if (!Number.isFinite(bytes)) return 'unknown';
  if (bytes === 0) return '0 B';
  const units = ['B', 'KiB', 'MiB', 'GiB', 'TiB'];
  const idx = Math.floor(Math.log(bytes) / Math.log(1024));
  const value = bytes / Math.pow(1024, idx);
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[idx]}`;
}
