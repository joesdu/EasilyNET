// src/api-client.ts
var GridFSApiClient = class {
  constructor(config) {
    this.baseUrl = config.baseUrl.replace(/\/+$/, "");
    this.headers = config.headers ?? {};
  }
  /**
   * Get the API base URL
   */
  get apiBase() {
    return `${this.baseUrl}/api/GridFS`;
  }
  /**
   * Create a new upload session
   */
  async createSession(filename, totalSize, fileHash, contentType, signal) {
    const params = new URLSearchParams({
      filename,
      totalSize: totalSize.toString()
    });
    if (contentType) {
      params.append("contentType", contentType);
    }
    if (fileHash) {
      params.append("fileHash", fileHash);
    }
    const response = await fetch(`${this.apiBase}/CreateSession?${params}`, {
      method: "POST",
      headers: this.headers,
      signal
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to create session: ${response.status} ${response.statusText} - ${errorText}`);
    }
    return response.json();
  }
  /**
   * Upload a chunk of data
   */
  async uploadChunk(sessionId, chunkNumber, chunkHash, data, signal) {
    const params = new URLSearchParams({
      sessionId,
      chunkNumber: chunkNumber.toString(),
      chunkHash
    });
    const response = await fetch(`${this.apiBase}/UploadChunk?${params}`, {
      method: "POST",
      headers: {
        ...this.headers,
        "Content-Type": "application/octet-stream"
      },
      body: data,
      signal
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to upload chunk ${chunkNumber}: ${response.status} ${response.statusText} - ${errorText}`);
    }
    return response.json();
  }
  /**
   * Get session information
   */
  async getSession(sessionId, signal) {
    const response = await fetch(`${this.apiBase}/Session/${sessionId}`, {
      method: "GET",
      headers: this.headers,
      signal
    });
    if (!response.ok) {
      throw new Error(`Failed to get session: ${response.status} ${response.statusText}`);
    }
    return response.json();
  }
  /**
   * Get missing chunk numbers
   */
  async getMissingChunks(sessionId, signal) {
    const response = await fetch(`${this.apiBase}/MissingChunks/${sessionId}`, {
      method: "GET",
      headers: this.headers,
      signal
    });
    if (!response.ok) {
      throw new Error(`Failed to get missing chunks: ${response.status} ${response.statusText}`);
    }
    return response.json();
  }
  /**
   * Finalize the upload
   */
  async finalize(sessionId, fileHash, skipHashValidation, signal) {
    const params = new URLSearchParams();
    if (fileHash) {
      params.append("fileHash", fileHash);
    }
    if (skipHashValidation) {
      params.append("skipHashValidation", "true");
    }
    const queryString = params.toString();
    const url = queryString ? `${this.apiBase}/Finalize/${sessionId}?${queryString}` : `${this.apiBase}/Finalize/${sessionId}`;
    const response = await fetch(url, {
      method: "POST",
      headers: {
        ...this.headers,
        "Content-Type": "application/json"
      },
      signal
    });
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Failed to finalize upload: ${response.status} ${response.statusText} - ${errorText}`);
    }
    return response.json();
  }
  /**
   * Cancel an upload session
   */
  async cancel(sessionId, signal) {
    const response = await fetch(`${this.apiBase}/Cancel/${sessionId}`, {
      method: "DELETE",
      headers: this.headers,
      signal
    });
    if (!response.ok) {
      throw new Error(`Failed to cancel upload: ${response.status} ${response.statusText}`);
    }
  }
  /**
   * Get stream URL for a file (for video/audio playback)
   */
  getStreamUrl(fileId) {
    let url = `${this.apiBase}/StreamRange/${fileId}`;
    const auth = this.headers["Authorization"] ?? this.headers["authorization"];
    if (auth) {
      const token = auth.replace(/^Bearer\s+/i, "");
      url += `?access_token=${encodeURIComponent(token)}`;
    }
    return url;
  }
};

// src/utils.ts
function formatFileSize(bytes) {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}
function formatTime(seconds) {
  if (!Number.isFinite(seconds) || seconds < 0) return "--";
  if (seconds < 60) return `${Math.round(seconds)}s`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m ${Math.round(seconds % 60)}s`;
  return `${Math.floor(seconds / 3600)}h ${Math.floor(seconds % 3600 / 60)}m`;
}
async function calculateBlobHash(blob) {
  const buffer = await blob.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest("SHA-256", buffer);
  return bufferToHex(new Uint8Array(hashBuffer));
}
function bufferToHex(bytes) {
  return Array.from(bytes).map((b) => b.toString(16).padStart(2, "0")).join("").toUpperCase();
}
async function calculateFileHash(file, onProgress, workerUrl) {
  try {
    return await calculateHashWithWorker(file, onProgress, workerUrl);
  } catch (e) {
    console.warn("Worker hash calculation failed, falling back to main thread", e);
  }
  return calculateHashMainThread(file, onProgress);
}
var HASH_READ_CHUNK_SIZE = 4 * 1024 * 1024;
async function calculateHashWithWorker(file, onProgress, workerUrl) {
  const url = workerUrl ?? new URL("./hash-worker.js", import.meta.url).href;
  return new Promise((resolve, reject) => {
    const worker = new Worker(url);
    let settled = false;
    let lastProgressTime = 0;
    let lastProgressPercent = 0;
    worker.onmessage = (event) => {
      const message = event.data;
      switch (message.type) {
        case "ready":
          break;
        case "progress": {
          const now = Date.now();
          const percent = message.loaded / message.total * 100;
          if (onProgress && (now - lastProgressTime > 100 || percent - lastProgressPercent >= 1 || percent === 100)) {
            onProgress({
              loaded: message.loaded,
              total: message.total,
              percentage: percent
            });
            lastProgressTime = now;
            lastProgressPercent = percent;
          }
          break;
        }
        case "done":
          settled = true;
          worker.terminate();
          resolve(message.hash.toUpperCase());
          break;
        case "error":
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
    worker.postMessage({ type: "start", size: file.size });
    (async () => {
      try {
        let offset = 0;
        while (offset < file.size) {
          const end = Math.min(offset + HASH_READ_CHUNK_SIZE, file.size);
          const blob = file.slice(offset, end);
          const buffer = await blob.arrayBuffer();
          const chunk = new Uint8Array(buffer);
          worker.postMessage({ type: "chunk", chunk }, [chunk.buffer]);
          offset = end;
        }
        worker.postMessage({ type: "finalize" });
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
async function calculateHashMainThread(file, onProgress) {
  if (file.size < 50 * 1024 * 1024) {
    const buffer2 = await file.arrayBuffer();
    const hashBuffer2 = await crypto.subtle.digest("SHA-256", buffer2);
    onProgress?.({ loaded: file.size, total: file.size, percentage: 100 });
    return bufferToHex(new Uint8Array(hashBuffer2));
  }
  console.warn("Large file hash calculation on main thread may cause memory issues");
  const buffer = await file.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest("SHA-256", buffer);
  onProgress?.({ loaded: file.size, total: file.size, percentage: 100 });
  return bufferToHex(new Uint8Array(hashBuffer));
}
function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
async function retry(fn, maxRetries, baseDelay = 1e3) {
  let lastError;
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

// src/uploader.ts
var DEFAULT_OPTIONS = {
  baseUrl: "",
  chunkSize: 1024 * 1024,
  // 1MB default, will be overridden by server
  maxConcurrent: Math.min(6, Math.max(2, typeof navigator !== "undefined" ? navigator.hardwareConcurrency ?? 4 : 4)),
  retryCount: 3,
  retryDelay: 1e3,
  verifyOnServer: true,
  headers: {},
  metadata: {}
};
var GridFSUploader = class {
  constructor(file, options = {}) {
    this.chunks = [];
    this.sessionId = "";
    this.abortController = null;
    this.startTime = 0;
    this.uploadedBytes = 0;
    this.elapsedBeforePause = 0;
    this.hashPromise = null;
    this._state = "idle";
    this._phase = "idle";
    this.file = file;
    this.options = {
      ...DEFAULT_OPTIONS,
      onProgress: () => {
      },
      onError: () => {
      },
      onComplete: () => {
      },
      onStateChange: () => {
      },
      ...options
    };
    this.apiClient = new GridFSApiClient({
      baseUrl: this.options.baseUrl,
      headers: this.options.headers
    });
  }
  /**
   * Get current upload state
   */
  get state() {
    return this._state;
  }
  /**
   * Get current upload phase
   */
  get phase() {
    return this._phase;
  }
  /**
   * Get session ID (available after upload starts)
   */
  get uploadSessionId() {
    return this.sessionId;
  }
  /**
   * Start the upload
   */
  async start() {
    if (this._state === "paused") {
      return this.resume();
    }
    this.setState("preparing");
    this.setPhase("hashing");
    this.abortController = new AbortController();
    this.startTime = performance.now();
    this.elapsedBeforePause = 0;
    this.uploadedBytes = 0;
    try {
      this.hashPromise = calculateFileHash(this.file, (hashProgress) => {
        this.emitProgress({
          loaded: 0,
          total: this.file.size,
          percentage: 0,
          speed: 0,
          remainingTime: 0,
          phase: "hashing",
          hashProgress: hashProgress.percentage,
          chunksUploaded: 0,
          totalChunks: 0
        });
      });
      const fileHash = this.cachedFileHash ?? await this.hashPromise.catch((e) => {
        console.warn("Hash calculation failed:", e);
        return void 0;
      });
      if (fileHash) {
        this.cachedFileHash = fileHash;
      }
      if (this._state === "paused" || this._state === "cancelled") {
        return this.sessionId;
      }
      this.setPhase("creating-session");
      const sessionInfo = await this.apiClient.createSession(
        this.file.name,
        this.file.size,
        fileHash,
        this.file.type || void 0,
        this.abortController.signal
      );
      this.sessionId = sessionInfo.sessionId;
      if (sessionInfo.status === "Completed" && sessionInfo.fileId) {
        this.setPhase("completed");
        this.setState("completed");
        this.emitProgress({
          loaded: this.file.size,
          total: this.file.size,
          percentage: 100,
          speed: 0,
          remainingTime: 0,
          phase: "completed",
          chunksUploaded: 0,
          totalChunks: 0
        });
        this.options.onComplete(sessionInfo.fileId);
        return sessionInfo.fileId;
      }
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }
      this.initializeChunks();
      this.setState("uploading");
      this.setPhase("uploading");
      await this.uploadChunks();
      if (this._state === "paused" || this._state === "cancelled") {
        return this.sessionId;
      }
      this.setPhase("finalizing");
      const finalHash = this.cachedFileHash ?? await this.hashPromise?.catch(() => void 0);
      const result = await this.apiClient.finalize(
        this.sessionId,
        finalHash,
        !this.options.verifyOnServer,
        this.abortController.signal
      );
      this.setPhase("completed");
      this.setState("completed");
      this.options.onComplete(result.fileId);
      return result.fileId;
    } catch (error) {
      this.setPhase("error");
      this.setState("error");
      const err = error instanceof Error ? error : new Error(String(error));
      this.options.onError(err);
      throw err;
    }
  }
  /**
   * Pause the upload
   */
  pause() {
    if (this._state !== "uploading") return;
    this.setState("paused");
    this.elapsedBeforePause += performance.now() - this.startTime;
    this.abortController?.abort();
  }
  /**
   * Resume a paused upload
   */
  async resume() {
    if (this._state !== "paused") {
      throw new Error("Cannot resume: upload is not paused");
    }
    if (!this.sessionId) {
      throw new Error("Cannot resume: no session ID");
    }
    this.setState("uploading");
    this.setPhase("uploading");
    this.abortController = new AbortController();
    this.startTime = performance.now();
    try {
      const sessionInfo = await this.apiClient.getSession(this.sessionId, this.abortController.signal);
      if (sessionInfo.status === "Completed") {
        this.setPhase("completed");
        this.setState("completed");
        const result2 = await this.apiClient.finalize(this.sessionId);
        this.options.onComplete(result2.fileId);
        return result2.fileId;
      }
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }
      if (this.chunks.length === 0) {
        this.initializeChunks();
      }
      const uploadedSet = new Set(sessionInfo.uploadedChunks ?? []);
      let uploadedBytes = 0;
      for (const chunk of this.chunks) {
        if (uploadedSet.has(chunk.index)) {
          chunk.uploaded = true;
          uploadedBytes += chunk.end - chunk.start;
        }
      }
      this.uploadedBytes = uploadedBytes;
      await this.uploadChunks();
      if (this._state === "paused" || this._state === "cancelled") {
        return this.sessionId;
      }
      this.setPhase("finalizing");
      const finalHash = this.cachedFileHash ?? await this.hashPromise?.catch(() => void 0);
      const result = await this.apiClient.finalize(
        this.sessionId,
        finalHash,
        !this.options.verifyOnServer,
        this.abortController.signal
      );
      this.setPhase("completed");
      this.setState("completed");
      this.options.onComplete(result.fileId);
      return result.fileId;
    } catch (error) {
      this.setPhase("error");
      this.setState("error");
      const err = error instanceof Error ? error : new Error(String(error));
      this.options.onError(err);
      throw err;
    }
  }
  /**
   * Cancel the upload
   */
  async cancel() {
    this.setState("cancelled");
    this.abortController?.abort();
    if (this.sessionId) {
      try {
        await this.apiClient.cancel(this.sessionId);
      } catch {
      }
    }
  }
  /**
   * Initialize chunk tasks
   */
  initializeChunks() {
    const totalChunks = Math.ceil(this.file.size / this.options.chunkSize);
    this.chunks = Array.from({ length: totalChunks }, (_, i) => ({
      index: i,
      start: i * this.options.chunkSize,
      end: Math.min((i + 1) * this.options.chunkSize, this.file.size),
      retries: 0,
      uploaded: false
    }));
  }
  /**
   * Upload all pending chunks with concurrency control
   */
  async uploadChunks() {
    const pendingChunks = this.chunks.filter((c) => !c.uploaded);
    const queue = [...pendingChunks];
    const executing = [];
    while (queue.length > 0 || executing.length > 0) {
      if (this._state === "paused" || this._state === "cancelled") break;
      while (executing.length < this.options.maxConcurrent && queue.length > 0) {
        const chunk = queue.shift();
        const promise = this.uploadChunk(chunk).then(() => {
          const index = executing.indexOf(promise);
          if (index > -1) executing.splice(index, 1);
        }).catch((error) => {
          const index = executing.indexOf(promise);
          if (index > -1) executing.splice(index, 1);
          const isAbort = error instanceof DOMException && error.name === "AbortError";
          if (this._state === "paused" && isAbort) {
            return;
          }
          if (!isAbort && chunk.retries < this.options.retryCount) {
            chunk.retries++;
            queue.push(chunk);
          } else if (!isAbort) {
            throw error;
          }
        });
        executing.push(promise);
      }
      if (executing.length > 0) {
        await Promise.race(executing);
      }
    }
    if (this._state !== "paused" && this._state !== "cancelled" && executing.length > 0) {
      await Promise.all(executing);
    }
  }
  /**
   * Upload a single chunk
   */
  async uploadChunk(chunk) {
    const blob = this.file.slice(chunk.start, chunk.end);
    const chunkHash = await calculateBlobHash(blob);
    await retry(
      async () => {
        await this.apiClient.uploadChunk(
          this.sessionId,
          chunk.index,
          chunkHash,
          blob,
          this.abortController?.signal
        );
      },
      this.options.retryCount,
      this.options.retryDelay
    );
    chunk.uploaded = true;
    this.uploadedBytes += chunk.end - chunk.start;
    this.updateProgress();
  }
  /**
   * Update and emit progress
   */
  updateProgress() {
    const elapsed = (this.elapsedBeforePause + (performance.now() - this.startTime)) / 1e3;
    const speed = elapsed > 0 ? this.uploadedBytes / elapsed : 0;
    const remainingBytes = this.file.size - this.uploadedBytes;
    const remainingTime = speed > 0 ? remainingBytes / speed : 0;
    this.emitProgress({
      loaded: this.uploadedBytes,
      total: this.file.size,
      percentage: this.uploadedBytes / this.file.size * 100,
      speed,
      remainingTime,
      phase: this._phase,
      chunksUploaded: this.chunks.filter((c) => c.uploaded).length,
      totalChunks: this.chunks.length
    });
  }
  /**
   * Emit progress event
   */
  emitProgress(progress) {
    this.options.onProgress(progress);
  }
  /**
   * Set upload state
   */
  setState(state) {
    this._state = state;
    this.options.onStateChange(state);
  }
  /**
   * Set upload phase
   */
  setPhase(phase) {
    this._phase = phase;
  }
};

// src/downloader.ts
var DEFAULT_OPTIONS2 = {
  baseUrl: "",
  fileId: "",
  filename: "",
  headers: {}
};
var GridFSDownloader = class {
  constructor(options) {
    this.abortController = null;
    this.startTime = 0;
    this.options = {
      ...DEFAULT_OPTIONS2,
      onProgress: () => {
      },
      onError: () => {
      },
      ...options
    };
    this.apiClient = new GridFSApiClient({
      baseUrl: this.options.baseUrl,
      headers: this.options.headers
    });
  }
  /**
   * Get the download URL for a file
   */
  static getUrl(fileId, options) {
    const client = new GridFSApiClient({
      baseUrl: options?.baseUrl ?? "",
      headers: options?.headers
    });
    return client.getStreamUrl(fileId);
  }
  /**
   * Get the download URL for this file
   */
  getDownloadUrl() {
    return this.apiClient.getStreamUrl(this.options.fileId);
  }
  /**
   * Start downloading the file
   * @param getWritableStream Optional function to get a WritableStream for streaming save
   */
  async start(getWritableStream) {
    this.abortController = new AbortController();
    this.startTime = performance.now();
    try {
      const url = this.getDownloadUrl();
      const response = await fetch(url, {
        headers: this.options.headers,
        signal: this.abortController.signal
      });
      if (!response.ok && response.status !== 206) {
        throw new Error(`Download failed: ${response.status} ${response.statusText}`);
      }
      const contentDisposition = response.headers.get("Content-Disposition");
      if (contentDisposition && !this.options.filename) {
        const filenameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)["']?/i);
        if (filenameMatch?.[1]) {
          this.options.filename = decodeURIComponent(filenameMatch[1]);
        }
      }
      const contentRange = response.headers.get("Content-Range");
      const totalSize = contentRange ? parseInt(contentRange.split("/")[1]) : parseInt(response.headers.get("Content-Length") ?? "0");
      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error("Cannot read response stream");
      }
      let writer = null;
      if (getWritableStream) {
        const stream = await getWritableStream(this.options.filename, totalSize);
        if (stream) {
          writer = stream.getWriter();
        }
      }
      const chunks = [];
      let receivedLength = 0;
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        if (writer) {
          await writer.write(value);
        } else {
          chunks.push(value);
        }
        receivedLength += value.length;
        this.emitProgress(receivedLength, totalSize);
      }
      if (writer) {
        await writer.close();
        return;
      }
      return new Blob(chunks);
    } catch (error) {
      const err = error instanceof Error ? error : new Error(String(error));
      this.options.onError(err);
      throw err;
    }
  }
  /**
   * Cancel the download
   */
  cancel() {
    this.abortController?.abort();
  }
  /**
   * Download and save using browser's native download
   */
  async downloadAndSave() {
    const url = this.getDownloadUrl();
    const a = document.createElement("a");
    a.style.display = "none";
    a.href = url;
    if (this.options.filename) {
      a.download = this.options.filename;
    }
    document.body.appendChild(a);
    a.click();
    setTimeout(() => {
      document.body.removeChild(a);
    }, 100);
  }
  /**
   * Save a blob to file
   */
  saveBlob(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename ?? this.options.filename ?? "download";
    a.click();
    URL.revokeObjectURL(url);
  }
  /**
   * Emit progress event
   */
  emitProgress(loaded, total) {
    const elapsed = (performance.now() - this.startTime) / 1e3;
    const speed = elapsed > 0 ? loaded / elapsed : 0;
    const progress = {
      loaded,
      total,
      percentage: total > 0 ? loaded / total * 100 : 0,
      speed
    };
    this.options.onProgress(progress);
  }
};
export {
  GridFSApiClient,
  GridFSDownloader,
  GridFSUploader,
  bufferToHex,
  calculateBlobHash,
  calculateFileHash,
  delay,
  formatFileSize,
  formatTime,
  retry
};
