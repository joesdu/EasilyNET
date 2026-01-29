/**
 * @easilynet/gridfs-sdk - Utilities
 * Helper functions for file operations
 */

import type { HashProgress, HashWorkerMessage } from './types';

/**
 * Format bytes to human-readable string
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * Format seconds to human-readable time string
 */
export function formatTime(seconds: number): string {
  if (!Number.isFinite(seconds) || seconds < 0) return '--';
  if (seconds < 60) return `${Math.round(seconds)}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${Math.round(seconds % 60)}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor((seconds % 3600) / 60)}m`;
}

/**
 * Calculate SHA256 hash of a Blob using Web Crypto API
 */
export async function calculateBlobHash(blob: Blob): Promise<string> {
  const buffer = await blob.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
  return bufferToHex(new Uint8Array(hashBuffer));
}

/**
 * Convert Uint8Array to hex string
 */
export function bufferToHex(bytes: Uint8Array): string {
  return Array.from(bytes)
    .map((b) => b.toString(16).padStart(2, '0'))
    .join('')
    .toUpperCase();
}

/**
 * Calculate SHA256 hash of a File using Web Worker (non-blocking)
 */
export async function calculateFileHash(
  file: File,
  onProgress?: (progress: HashProgress) => void,
  workerUrl?: string
): Promise<string> {
  // Try Web Worker first for non-blocking hash calculation
  try {
    return await calculateHashWithWorker(file, onProgress, workerUrl);
  } catch (e) {
    console.warn('Worker hash calculation failed, falling back to main thread', e);
  }

  // Fallback: Main thread streaming hash (may block UI for large files)
  return calculateHashMainThread(file, onProgress);
}

/**
 * Optimal chunk size for reading file and sending to worker.
 * Larger chunks reduce postMessage overhead significantly for large files.
 * 4MB provides good balance between memory usage and performance.
 */
const HASH_READ_CHUNK_SIZE = 4 * 1024 * 1024; // 4MB

/**
 * Calculate hash using Web Worker
 */
async function calculateHashWithWorker(
  file: File,
  onProgress?: (progress: HashProgress) => void,
  workerUrl?: string
): Promise<string> {
  const url = workerUrl ?? new URL('./hash-worker.js', import.meta.url).href;
  
  return new Promise((resolve, reject) => {
    const worker = new Worker(url);
    let settled = false;
    let lastProgressTime = 0;
    let lastProgressPercent = 0;

    worker.onmessage = (event: MessageEvent<HashWorkerMessage>) => {
      const message = event.data;
      
      switch (message.type) {
        case 'ready':
          // Worker is ready, start sending chunks
          break;
        case 'progress': {
          const now = Date.now();
          const percent = (message.loaded / message.total) * 100;
          // Throttle progress updates
          if (onProgress && (now - lastProgressTime > 100 || percent - lastProgressPercent >= 1 || percent === 100)) {
            onProgress({
              loaded: message.loaded,
              total: message.total,
              percentage: percent,
            });
            lastProgressTime = now;
            lastProgressPercent = percent;
          }
          break;
        }
        case 'done':
          settled = true;
          worker.terminate();
          resolve(message.hash.toUpperCase());
          break;
        case 'error':
          settled = true;
          worker.terminate();
          reject(new Error(message.error));
          break;
      }
    };

    worker.onerror = (err) => {
      if (!settled) {
        settled = true;
        reject(err);
      }
      worker.terminate();
    };

    // Start the worker
    worker.postMessage({ type: 'start', size: file.size });

    // Stream file chunks to worker using larger chunk sizes for better performance
    (async () => {
      try {
        let offset = 0;
        while (offset < file.size) {
          const end = Math.min(offset + HASH_READ_CHUNK_SIZE, file.size);
          const blob = file.slice(offset, end);
          const buffer = await blob.arrayBuffer();
          const chunk = new Uint8Array(buffer);
          
          // Transfer the buffer to avoid copying (zero-copy transfer)
          worker.postMessage({ type: 'chunk', chunk }, [chunk.buffer]);
          offset = end;
        }
        worker.postMessage({ type: 'finalize' });
      } catch (err) {
        if (!settled) {
          settled = true;
          worker.terminate();
          reject(err);
        }
      }
    })();
  });
}

/**
 * Calculate hash on main thread (fallback)
 */
async function calculateHashMainThread(
  file: File,
  onProgress?: (progress: HashProgress) => void
): Promise<string> {
  // For small files, use simple approach
  if (file.size < 50 * 1024 * 1024) {
    const buffer = await file.arrayBuffer();
    const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
    onProgress?.({ loaded: file.size, total: file.size, percentage: 100 });
    return bufferToHex(new Uint8Array(hashBuffer));
  }

  // For larger files, we need to use incremental hashing
  // This requires SubtleCrypto which doesn't support incremental hashing natively
  // So we fall back to loading the entire file (memory intensive)
  console.warn('Large file hash calculation on main thread may cause memory issues');
  const buffer = await file.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest('SHA-256', buffer);
  onProgress?.({ loaded: file.size, total: file.size, percentage: 100 });
  return bufferToHex(new Uint8Array(hashBuffer));
}

/**
 * Delay execution for specified milliseconds
 */
export function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

/**
 * Create an AbortController with timeout
 */
export function createTimeoutController(timeoutMs: number): AbortController {
  const controller = new AbortController();
  setTimeout(() => controller.abort(), timeoutMs);
  return controller;
}

/**
 * Retry a function with exponential backoff
 */
export async function retry<T>(
  fn: () => Promise<T>,
  maxRetries: number,
  baseDelay: number = 1000
): Promise<T> {
  let lastError: Error | undefined;
  
  for (let attempt = 0; attempt <= maxRetries; attempt++) {
    try {
      return await fn();
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error));
      if (attempt < maxRetries) {
        const delayMs = baseDelay * Math.pow(2, attempt);
        await delay(delayMs);
      }
    }
  }
  
  throw lastError;
}
