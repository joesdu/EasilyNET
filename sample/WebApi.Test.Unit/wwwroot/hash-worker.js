/* eslint-disable no-undef */
// Web Worker: 通过 hash-wasm 在后台计算 SHA256, 避免阻塞主线程
// SubtleCrypto 回退模式必须一次性拼接全部分片，容易耗尽内存，因此设置硬性文件大小上限。
const SUBTLE_FALLBACK_MAX_BYTES = 64 * 1024 * 1024; // 64 MiB

let hasher = null;
let processed = 0;
let total = 0;
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
        subtleChunks = [];
        if (total > SUBTLE_FALLBACK_MAX_BYTES) {
          postMessage({
            type: "error",
            error: `hash-wasm 不可用且 SubtleCrypto 回退仅支持 <= ${formatBytes(
              SUBTLE_FALLBACK_MAX_BYTES
            )} 的文件（当前 ${formatBytes(total)}）。`,
          });
          close();
          return;
        }
      }
      postMessage({
        type: "ready",
        impl: hasher ? "hash-wasm" : "subtle",
        subtleLimit: SUBTLE_FALLBACK_MAX_BYTES,
      });
      return;
    }

    if (type === "chunk") {
      // 确保 chunk 是 Uint8Array 类型，处理 Transferable 传递后可能的类型变化
      const data = chunk instanceof Uint8Array ? chunk : new Uint8Array(chunk);
      if (hasher) {
        hasher.update(data);
      } else {
        // 对于 SubtleCrypto 回退，需要保存 ArrayBuffer 的副本
        subtleChunks.push(data.buffer.slice(0));
      }
      processed += data.byteLength;
      postMessage({ type: "progress", loaded: processed, total });
      return;
    }

    if (type === "finalize") {
      let hex = "";
      if (hasher) {
        const digestResult = hasher.digest();
        hex = (
          typeof digestResult === "string"
            ? digestResult
            : bufferToHex(digestResult)
        ).toUpperCase();
      } else {
        // Fallback: 使用 SubtleCrypto(需要拼接内存,但只在 hash-wasm 不可用且文件大小受限时执行)
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

function formatBytes(bytes) {
  if (!Number.isFinite(bytes)) return "未知";
  if (bytes === 0) return "0 B";
  const units = ["B", "KiB", "MiB", "GiB", "TiB"];
  const idx = Math.floor(Math.log(bytes) / Math.log(1024));
  const value = bytes / Math.pow(1024, idx);
  return `${value.toFixed(value >= 10 ? 0 : 1)} ${units[idx]}`;
}
