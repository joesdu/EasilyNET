/**
 * GridFS 断点续传客户端库
 * 支持文件分块上传、断点续传、下载恢复
 *
 * @version 1.0.0
 * @author EasilyNET
 * @license MIT
 */

/**
 * GridFS 断点续传上传器
 */
export class GridFSResumableUploader {
  constructor(file, options) {
    this.chunks = [];
    this.uploadId = "";
    this.abortController = null;
    this.startTime = 0;
    this.uploadedBytes = 0;
    this.isPaused = false;
    this.file = file;
    this.options = {
      chunkSize: 1024 * 1024, // 1MB (初始值,将被后端返回的值覆盖)
      maxConcurrent: 3,
      retryCount: 3,
      headers: {},
      metadata: {},
      onProgress: () => {},
      onError: () => {},
      onComplete: () => {},
      ...options,
    };

    // 注意: chunks 将在获取到后端返回的 chunkSize 后再初始化
    // this.initializeChunks();
  }

  /**
   * 初始化分块信息
   */
  initializeChunks() {
    const totalChunks = Math.ceil(this.file.size / this.options.chunkSize);
    this.chunks = Array.from({ length: totalChunks }, (_, i) => ({
      index: i,
      start: i * this.options.chunkSize,
      end: Math.min((i + 1) * this.options.chunkSize, this.file.size),
      retries: 0,
      uploaded: false,
    }));
  }

  /**
   * 开始上传
   */
  async start() {
    if (this.isPaused) {
      this.resume();
      return this.uploadId;
    }

    this.abortController = new AbortController();
    this.startTime = Date.now();

    try {
      // 0. 预先计算文件哈希 (用于秒传)
      // 通知用户正在计算哈希
      if (this.options.onProgress) {
        this.options.onProgress({
          loaded: 0,
          total: this.file.size,
          percentage: 0,
          speed: 0,
          remainingTime: 0,
          status: "hashing", // 新增状态标识
        });
      }

      let fileHash;
      try {
        fileHash = await calculateSHA256(this.file);
      } catch (e) {
        console.warn("哈希计算失败:", e);
      }

      // 1. 初始化上传会话 (携带哈希)
      const sessionInfo = await this.initializeUpload(fileHash);
      this.uploadId = sessionInfo.sessionId;

      // 检查是否秒传成功
      if (sessionInfo.status === "Completed" && sessionInfo.fileId) {
        console.log("秒传成功!");
        // 直接完成
        this.options.onProgress?.({
          loaded: this.file.size,
          total: this.file.size,
          percentage: 100,
          speed: 0,
          remainingTime: 0,
          status: "completed",
        });
        this.options.onComplete(sessionInfo.fileId);
        return sessionInfo.fileId;
      }

      // 使用后端返回的 chunkSize
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }

      // 现在初始化分块 (使用后端的 chunkSize)
      this.initializeChunks();

      // 2. 并发上传分块
      await this.uploadChunks();

      // 3. 完成上传
      // 通知用户开始合并文件
      if (this.options.onProgress) {
        this.options.onProgress({
          loaded: this.file.size,
          total: this.file.size,
          percentage: 100,
          speed: 0,
          remainingTime: 0,
          status: "merging", // 新增状态标识
        });
      }

      // 此时 fileHash 已经计算过了，直接使用
      const finalFileId = await this.completeUpload(fileHash);

      this.options.onComplete(finalFileId);
      return finalFileId;
    } catch (error) {
      this.options.onError(error);
      throw error;
    }
  }

  /**
   * 暂停上传
   */
  pause() {
    this.isPaused = true;
    this.abortController?.abort();
  }

  /**
   * 恢复上传
   */
  async resume() {
    if (!this.isPaused) return;

    this.isPaused = false;
    this.abortController = new AbortController();

    try {
      await this.uploadChunks();

      // 计算哈希 (同 start 方法)
      let fileHash;
      try {
        fileHash = await calculateSHA256(this.file);
      } catch (e) {
        console.warn("哈希计算失败:", e);
      }

      const fileId = await this.completeUpload(fileHash);
      this.options.onComplete(fileId);
    } catch (error) {
      this.options.onError(error);
      throw error;
    }
  }

  /**
   * 取消上传
   */
  async cancel() {
    this.abortController?.abort();
    if (this.uploadId) {
      await this.abortUpload();
    }
  }

  /**
   * 初始化上传会话
   */
  async initializeUpload(fileHash) {
    const params = new URLSearchParams({
      filename: this.file.name,
      totalSize: this.file.size.toString(),
      contentType: this.file.type,
    });

    if (fileHash) {
      params.append("fileHash", fileHash);
    }

    const response = await fetch(
      `${this.options.uploadUrl}/CreateSession?${params.toString()}`,
      {
        method: "POST",
        headers: {
          ...this.options.headers,
        },
      }
    );

    if (!response.ok) {
      throw new Error(`初始化上传失败: ${response.statusText}`);
    }

    const sessionInfo = await response.json();
    return sessionInfo; // 返回完整的会话信息,包含 sessionId 和 chunkSize
  }

  /**
   * 并发上传分块
   */
  async uploadChunks() {
    const pendingChunks = this.chunks.filter((c) => !c.uploaded);
    const queue = [...pendingChunks];
    const executing = [];

    while (queue.length > 0 || executing.length > 0) {
      if (this.isPaused) break;

      while (
        executing.length < this.options.maxConcurrent &&
        queue.length > 0
      ) {
        const chunk = queue.shift();
        const promise = this.uploadChunk(chunk)
          .then(() => {
            const index = executing.indexOf(promise);
            if (index > -1) executing.splice(index, 1);
          })
          .catch((error) => {
            // 重试逻辑
            if (chunk.retries < this.options.retryCount) {
              chunk.retries++;
              queue.push(chunk);
              const index = executing.indexOf(promise);
              if (index > -1) executing.splice(index, 1);
            } else {
              throw error;
            }
          });

        executing.push(promise);
      }

      if (executing.length > 0) {
        await Promise.race(executing);
      }
    }

    // 确保所有分块都已上传
    if (!this.isPaused && executing.length > 0) {
      await Promise.all(executing);
    }
  }

  /**
   * 上传单个分块
   */
  async uploadChunk(chunk, internalRetry = 0) {
    const blob = this.file.slice(chunk.start, chunk.end);
    const chunkHash = await calculateHash(blob);

    const response = await fetch(
      `${this.options.uploadUrl}/UploadChunk?sessionId=${this.uploadId}&chunkNumber=${chunk.index}&chunkHash=${chunkHash}`,
      {
        method: "POST",
        headers: {
          "Content-Type": "application/octet-stream",
          ...this.options.headers,
        },
        body: blob,
        signal: this.abortController?.signal,
      }
    );

    if (!response.ok) {
      const errorText = await response.text();
      // 检查是否是哈希校验失败 (状态码 400 且错误信息包含 hash)
      if (
        response.status === 400 &&
        errorText.toLowerCase().includes("hash") &&
        internalRetry < 3
      ) {
        console.warn(
          `分块 ${chunk.index} 哈希校验失败，正在重试 (${
            internalRetry + 1
          }/3)...`
        );
        return this.uploadChunk(chunk, internalRetry + 1);
      }
      throw new Error(
        `上传分块 ${chunk.index} 失败: ${response.status} ${response.statusText} - ${errorText}`
      );
    }

    chunk.uploaded = true;
    this.uploadedBytes += chunk.end - chunk.start;
    this.updateProgress();
  }

  /**
   * 完成上传
   */
  async completeUpload(fileHash) {
    let url = `${this.options.uploadUrl}/Finalize/${this.uploadId}`;
    if (fileHash) {
      url += `?fileHash=${fileHash}`;
    }

    const response = await fetch(url, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        ...this.options.headers,
      },
    });

    if (!response.ok) {
      throw new Error(`完成上传失败: ${response.statusText}`);
    }

    const { fileId } = await response.json();
    return fileId;
  }

  /**
   * 取消上传
   */
  async abortUpload() {
    await fetch(`${this.options.uploadUrl}/Cancel/${this.uploadId}`, {
      method: "DELETE",
      headers: this.options.headers,
    });
  }

  /**
   * 更新上传进度
   */
  updateProgress() {
    const elapsed = (Date.now() - this.startTime) / 1000; // 秒
    const speed = elapsed > 0 ? this.uploadedBytes / elapsed : 0;
    const remainingBytes = this.file.size - this.uploadedBytes;
    const remainingTime = speed > 0 ? remainingBytes / speed : 0;

    const progress = {
      loaded: this.uploadedBytes,
      total: this.file.size,
      percentage: (this.uploadedBytes / this.file.size) * 100,
      speed,
      remainingTime,
    };

    this.options.onProgress(progress);
  }
}

/**
 * GridFS 断点续传下载器
 */
export class GridFSResumableDownloader {
  constructor(options) {
    this.abortController = null;
    this.options = {
      filename: "download",
      headers: {},
      onProgress: () => {},
      onError: () => {},
      ...options,
    };
  }

  /**
   * 开始下载 (支持断点续传)
   */
  async start() {
    this.abortController = new AbortController();

    try {
      // 检查是否有未完成的下载
      const partialData = this.getPartialDownload();
      const startByte = partialData ? partialData.size : 0;

      // 发送 Range 请求
      const headers = {
        ...this.options.headers,
      };

      if (startByte > 0) {
        headers["Range"] = `bytes=${startByte}-`;
      }

      const response = await fetch(
        `${this.options.downloadUrl}/${this.options.fileId}`,
        {
          headers,
          signal: this.abortController.signal,
        }
      );

      if (!response.ok && response.status !== 206) {
        throw new Error(`下载失败: ${response.statusText}`);
      }

      // 从 Content-Disposition 头中提取文件名
      const contentDisposition = response.headers.get("Content-Disposition");
      if (contentDisposition) {
        const filenameMatch = contentDisposition.match(
          /filename\*?=(?:UTF-8'')?["']?([^"';]+)["']?/i
        );
        if (filenameMatch && filenameMatch[1]) {
          // 解码 URL 编码的文件名
          this.options.filename = decodeURIComponent(filenameMatch[1]);
        }
      }

      // 获取总大小
      const contentRange = response.headers.get("Content-Range");
      const totalSize = contentRange
        ? parseInt(contentRange.split("/")[1])
        : parseInt(response.headers.get("Content-Length") || "0");

      // 流式读取响应
      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error("无法读取响应流");
      }

      const chunks = partialData
        ? [new Uint8Array(await partialData.arrayBuffer())]
        : [];
      let receivedLength = startByte;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        chunks.push(value);
        receivedLength += value.length;

        // 保存部分下载数据
        this.savePartialDownload(new Blob(chunks));

        // 更新进度
        this.options.onProgress?.({
          loaded: receivedLength,
          total: totalSize,
          percentage: (receivedLength / totalSize) * 100,
        });
      }

      // 合并所有分块
      const blob = new Blob(chunks);

      // 清除部分下载数据
      this.clearPartialDownload();

      return blob;
    } catch (error) {
      this.options.onError?.(error);
      throw error;
    }
  }

  /**
   * 取消下载
   */
  cancel() {
    this.abortController?.abort();
  }

  /**
   * 下载并保存文件
   */
  async downloadAndSave() {
    const blob = await this.start();
    this.saveFile(blob, this.options.filename);
  }

  /**
   * 保存文件到本地
   */
  saveFile(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }

  /**
   * 保存部分下载数据到 IndexedDB
   */
  savePartialDownload(blob) {
    const key = `gridfs_download_${this.options.fileId}`;
    // 使用 localStorage 作为简单示例,生产环境建议使用 IndexedDB
    // 这里仅保存引用,实际数据在内存中
    localStorage.setItem(key, "partial");
  }

  /**
   * 获取部分下载数据
   */
  getPartialDownload() {
    const key = `gridfs_download_${this.options.fileId}`;
    return localStorage.getItem(key) ? null : null; // 简化实现
  }

  /**
   * 清除部分下载数据
   */
  clearPartialDownload() {
    const key = `gridfs_download_${this.options.fileId}`;
    localStorage.removeItem(key);
  }
}

/**
 * 格式化文件大小
 */
export function formatFileSize(bytes) {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * 格式化时间
 */
export function formatTime(seconds) {
  if (seconds < 60) return `${Math.round(seconds)}秒`;
  if (seconds < 3600)
    return `${Math.floor(seconds / 60)}分${Math.round(seconds % 60)}秒`;
  return `${Math.floor(seconds / 3600)}时${Math.floor(
    (seconds % 3600) / 60
  )}分`;
}

/**
 * 计算 Blob 的 SHA256 哈希
 */
async function calculateHash(blob) {
  const buffer = await blob.arrayBuffer();
  const hashBuffer = await crypto.subtle.digest("SHA-256", buffer);
  const hashArray = Array.from(new Uint8Array(hashBuffer));
  return hashArray
    .map((b) => b.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
}

/**
 * 计算文件 SHA256 哈希 (流式)
 */
async function calculateSHA256(file) {
  // 尝试动态加载 hash-wasm
  try {
    if (typeof hashwasm === "undefined") {
      await new Promise((resolve, reject) => {
        const src = "./lib/hash-wasm/sha256.umd.min.js";
        if (document.querySelector(`script[src="${src}"]`)) {
          resolve();
          return;
        }
        const script = document.createElement("script");
        script.src = src;
        script.onload = () => resolve();
        script.onerror = () => reject(new Error("Failed to load hash-wasm"));
        document.head.appendChild(script);
      });
    }

    // 优先使用 hash-wasm (WebAssembly) 进行高性能计算
    if (typeof hashwasm !== "undefined") {
      const hasher = await hashwasm.createSHA256();
      hasher.init();

      const reader = file.stream().getReader();
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        hasher.update(value);
      }
      return hasher.digest().toUpperCase();
    }
  } catch (e) {
    console.warn(
      "Failed to load or use hash-wasm, falling back to slower methods",
      e
    );
  }

  const chunkSize = 10 * 1024 * 1024; // 10MB 块大小
  const chunks = Math.ceil(file.size / chunkSize);
  let currentChunk = 0;

  if (typeof sha256 !== "undefined") {
    return new Promise((resolve, reject) => {
      const hasher = sha256.create();
      const fileReader = new FileReader();

      fileReader.onload = (e) => {
        try {
          const buffer = e.target?.result;
          hasher.update(buffer);
          currentChunk++;

          if (currentChunk < chunks) {
            loadNext();
          } else {
            resolve(hasher.hex().toUpperCase());
          }
        } catch (error) {
          reject(error);
        }
      };

      fileReader.onerror = () => {
        reject(new Error("文件读取失败"));
      };

      function loadNext() {
        const start = currentChunk * chunkSize;
        const end = Math.min(start + chunkSize, file.size);
        const blob = file.slice(start, end);
        fileReader.readAsArrayBuffer(blob);
      }

      loadNext();
    });
  }

  console.warn(
    "hash-wasm and js-sha256 not found, using Web Crypto API (may consume high memory for large files)"
  );
  return calculateHash(file);
}
