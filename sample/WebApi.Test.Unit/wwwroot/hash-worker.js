/* eslint-disable no-undef */
// Web Worker: 通过 hash-wasm 在后台计算 SHA256, 避免阻塞主线程
let hasher = null;
let processed = 0;
let total = 0;
let useSubtleFallback = false;
let subtleChunks = [];

self.onmessage = async (event) => {
  const { type, chunk, size } = event.data;

  try {
    if (type === "start") {
      total = size || 0;
      processed = 0;
      try {
        importScripts("./lib/hash-wasm/sha256.umd.min.js");
        if (self.hashwasm) {
          hasher = await self.hashwasm.createSHA256();
          hasher.init();
        }
      } catch (e) {
        // ignore and fallback
      }

      if (!hasher) {
        useSubtleFallback = true;
        subtleChunks = [];
      }
      postMessage({ type: "ready", impl: hasher ? "hash-wasm" : "subtle" });
      return;
    }

    if (type === "chunk") {
      if (hasher) {
        hasher.update(chunk);
      } else {
        subtleChunks.push(chunk);
      }
      processed += chunk.byteLength;
      postMessage({ type: "progress", loaded: processed, total });
      return;
    }

    if (type === "finalize") {
      let hex = "";
      if (hasher) {
        hex = hasher.digest().toUpperCase();
      } else {
        // Fallback: 使用 SubtleCrypto(需要拼接内存,但只在 hash-wasm 不可用时执行)
        const combined = concatBuffers(subtleChunks);
        const digest = await crypto.subtle.digest("SHA-256", combined);
        hex = bufferToHex(new Uint8Array(digest)).toUpperCase();
      }
      postMessage({ type: "done", hash: hex });
      close();
    }
  } catch (err) {
    postMessage({ type: "error", error: err?.message || String(err) });
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
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("");
}
