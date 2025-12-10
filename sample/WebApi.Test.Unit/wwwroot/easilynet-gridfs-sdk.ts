/**
 * GridFS 断点续传客户端库
 * 支持文件分块上传、断点续传、下载恢复
 *
 * @version 1.0.0
 * @author EasilyNET
 * @license MIT
 */

/**
 * 声明全局变量类型
 */
declare const sha256: any;
declare const hashwasm: any;

/**
 * 上传配置选项
 */
export interface UploadOptions {
  /** 服务端地址 (例如: https://api.example.com), 默认为当前域名 */
  url?: string;
  /** 分块大小 (字节), 默认 1MB */
  chunkSize?: number;
  /** 最大并发上传数, 默认 3 */
  maxConcurrent?: number;
  /** 自动重试次数, 默认 3 */
  retryCount?: number;
  /** 是否在服务器端执行最终哈希校验, 默认 true(安全但更慢); 设为 false 可跳过服务端全量哈希以换取速度 */
  verifyOnServer?: boolean;
  /** 额外的请求头 */
  headers?: Record<string, string>;
  /** 文件元数据 */
  metadata?: Record<string, any>;
  /** 上传进度回调 */
  onProgress?: (progress: UploadProgress) => void;
  /** 错误回调 */
  onError?: (error: Error) => void;
  /** 完成回调 */
  onComplete?: (fileId: string) => void;
}

/**
 * 上传进度信息
 */
export interface UploadProgress {
  /** 已上传字节数 */
  loaded: number;
  /** 总字节数 */
  total: number;
  /** 进度百分比 (0-100) */
  percentage: number;
  /** 当前速度 (字节/秒) */
  speed: number;
  /** 预计剩余时间 (秒) */
  remainingTime: number;
  /** 上传状态: 'hashing' | 'uploading' | 'merging' | 'completed' */
  status?: "hashing" | "uploading" | "merging" | "completed";
  /** 哈希进度百分比 (0-100) */
  hashPercentage?: number;
}

/**
 * 下载配置选项
 */
export interface DownloadOptions {
  /** 服务端地址 (例如: https://api.example.com), 默认为当前域名 */
  url?: string;
  /** 文件 ID */
  fileId: string;
  /** 保存的文件名 */
  filename?: string;
  /** 额外的请求头 */
  headers?: Record<string, string>;
  /** 下载进度回调 */
  onProgress?: (progress: DownloadProgress) => void;
  /** 错误回调 */
  onError?: (error: Error) => void;
}

/**
 * 下载进度信息
 */
export interface DownloadProgress {
  /** 已下载字节数 */
  loaded: number;
  /** 总字节数 */
  total: number;
  /** 进度百分比 (0-100) */
  percentage: number;
}

/**
 * 分块上传任务
 */
interface ChunkTask {
  index: number;
  start: number;
  end: number;
  retries: number;
  uploaded: boolean;
}

/**
 * GridFS 断点续传上传器
 */
export class GridFSUploader {
  private file: File;
  private options: Required<UploadOptions>;
  private chunks: ChunkTask[] = [];
  private uploadId: string = "";
  private abortController: AbortController | null = null;
  private startTime: number = 0;
  private uploadedBytes: number = 0;
  private isPaused: boolean = false;
  private elapsedBeforePause: number = 0;
  private hashPromise: Promise<string> | null = null;
  private cachedFileHash?: string;

  constructor(file: File, options: UploadOptions) {
    this.file = file;
    this.options = {
      chunkSize: 1024 * 1024, // 1MB
      maxConcurrent: Math.min(
        6,
        Math.max(2, navigator?.hardwareConcurrency ?? 4)
      ),
      retryCount: 3,
      verifyOnServer: true,
      headers: {},
      metadata: {},
      onProgress: () => {},
      onError: () => {},
      onComplete: () => {},
      url: "",
      ...options,
    };

    // chunks 将在获取到后端返回的 chunkSize 后再初始化
    // this.initializeChunks();
  }

  /**
   * 初始化分块信息
   */
  private initializeChunks(): void {
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
  async start(): Promise<string> {
    if (this.isPaused) {
      await this.resume();
      return this.uploadId;
    }

    this.isPaused = false;
    this.abortController = new AbortController();
    this.startTime = performance.now();
    this.elapsedBeforePause = 0;
    this.uploadedBytes = 0;

    try {
      // 0. 预先计算文件哈希 (用于秒传), 在独立线程中避免阻塞 UI
      this.hashPromise = calculateSHA256(this.file, (hashProgress) => {
        this.options.onProgress?.({
          loaded: 0,
          total: this.file.size,
          percentage: 0,
          speed: 0,
          remainingTime: 0,
          status: "hashing",
          hashPercentage: hashProgress.percentage,
        });
      });

      const fileHash =
        this.cachedFileHash ??
        (await this.hashPromise.catch((e) => {
          console.warn("哈希计算失败:", e);
          return undefined;
        }));
      if (fileHash) this.cachedFileHash = fileHash;

      if (this.isPaused) {
        return this.uploadId;
      }

      // 1. 初始化上传会话 (携带哈希)
      const sessionInfo = await this.initializeUpload(fileHash);
      this.uploadId = sessionInfo.sessionId;

      // 检查是否秒传成功
      if (sessionInfo.status === "Completed" && sessionInfo.fileId) {
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

      // 如果服务端返回的块大小与配置不一致，更新配置并重新分块
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }

      // 现在初始化分块 (使用后端的 chunkSize)
      this.initializeChunks();
      this.options.onProgress?.({
        loaded: 0,
        total: this.file.size,
        percentage: 0,
        speed: 0,
        remainingTime: 0,
        status: "uploading",
      });

      // 2. 并发上传分块
      await this.uploadChunks();

      if (this.isPaused) return this.uploadId;

      // 3. 完成上传
      this.options.onProgress?.({
        loaded: this.file.size,
        total: this.file.size,
        percentage: 100,
        speed: 0,
        remainingTime: 0,
        status: "merging",
      });

      const finalHash =
        this.cachedFileHash ?? (await this.hashPromise?.catch(() => undefined));
      const finalFileId = await this.completeUpload(finalHash);

      this.options.onComplete(finalFileId);
      return finalFileId;
    } catch (error) {
      this.options.onError(error as Error);
      throw error;
    }
  }

  /**
   * 暂停上传
   */
  pause(): void {
    this.isPaused = true;
    this.elapsedBeforePause += performance.now() - this.startTime;
    this.abortController?.abort();
  }

  /**
   * 恢复上传
   */
  async resume(): Promise<void> {
    if (!this.isPaused) return;

    if (!this.uploadId) {
      throw new Error("没有可恢复的上传会话");
    }

    this.isPaused = false;
    this.abortController = new AbortController();
    this.startTime = performance.now();

    try {
      await this.uploadChunks();

      if (this.isPaused) return;

      // 优先使用已缓存/已在进行的哈希
      const fileHash =
        this.cachedFileHash ?? (await this.hashPromise?.catch(() => undefined));

      const fileId = await this.completeUpload(fileHash);
      this.options.onComplete(fileId);
    } catch (error) {
      this.options.onError(error as Error);
      throw error;
    }
  }

  /**
   * 取消上传
   */
  async cancel(): Promise<void> {
    this.abortController?.abort();
    if (this.uploadId) {
      await this.abortUpload();
    }
  }

  /**
   * 初始化上传会话
   */
  private async initializeUpload(fileHash?: string): Promise<{
    sessionId: string;
    chunkSize: number;
    status?: string;
    fileId?: string;
  }> {
    const params = new URLSearchParams({
      filename: this.file.name,
      totalSize: this.file.size.toString(),
      contentType: this.file.type,
    });

    if (fileHash) {
      params.append("fileHash", fileHash);
    }

    const host = (this.options.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    const response = await fetch(
      `${apiBase}/CreateSession?${params.toString()}`,
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

    const { sessionId, chunkSize, status, fileId } = await response.json();
    return { sessionId, chunkSize, status, fileId };
  }

  /**
   * 并发上传分块
   */
  private async uploadChunks(): Promise<void> {
    const pendingChunks = this.chunks.filter((c) => !c.uploaded);
    const queue = [...pendingChunks];
    const executing: Promise<void>[] = [];

    while (queue.length > 0 || executing.length > 0) {
      if (this.isPaused) break;

      while (
        executing.length < this.options.maxConcurrent &&
        queue.length > 0
      ) {
        const chunk = queue.shift()!;
        const promise = this.uploadChunk(chunk)
          .then(() => {
            const index = executing.indexOf(promise);
            if (index > -1) executing.splice(index, 1);
          })
          .catch((error) => {
            const isAbort = (error as DOMException)?.name === "AbortError";
            const removeExecuting = () => {
              const index = executing.indexOf(promise);
              if (index > -1) executing.splice(index, 1);
            };
            if (this.isPaused && isAbort) {
              removeExecuting();
              return;
            }
            // 重试逻辑
            if (!isAbort && chunk.retries < this.options.retryCount) {
              chunk.retries++;
              queue.push(chunk);
              removeExecuting();
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
  private async uploadChunk(
    chunk: ChunkTask,
    internalRetry = 0
  ): Promise<void> {
    const blob = this.file.slice(chunk.start, chunk.end);
    const chunkHash = await calculateHash(blob);

    const host = (this.options.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    let response: Response;
    try {
      response = await fetch(
        `${apiBase}/UploadChunk?sessionId=${this.uploadId}&chunkNumber=${chunk.index}&chunkHash=${chunkHash}`,
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
    } catch (error) {
      const isAbort = (error as DOMException)?.name === "AbortError";
      if (this.isPaused && isAbort) {
        return;
      }
      throw error;
    }

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
  private async completeUpload(fileHash?: string): Promise<string> {
    const host = (this.options.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    const params = new URLSearchParams();
    if (fileHash) {
      params.append("fileHash", fileHash);
    }
    if (this.options.verifyOnServer === false) {
      params.append("skipHashValidation", "true");
    }
    const qs = params.toString();
    const url = qs
      ? `${apiBase}/Finalize/${this.uploadId}?${qs}`
      : `${apiBase}/Finalize/${this.uploadId}`;

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
  private async abortUpload(): Promise<void> {
    const host = (this.options.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    await fetch(`${apiBase}/Cancel/${this.uploadId}`, {
      method: "DELETE",
      headers: this.options.headers,
    });
  }

  /**
   * 更新上传进度
   */
  private updateProgress(): void {
    const elapsed =
      (this.elapsedBeforePause + (performance.now() - this.startTime)) / 1000; // 秒
    const speed = elapsed > 0 ? this.uploadedBytes / elapsed : 0;
    const remainingBytes = this.file.size - this.uploadedBytes;
    const remainingTime = speed > 0 ? remainingBytes / speed : 0;

    const progress: UploadProgress = {
      loaded: this.uploadedBytes,
      total: this.file.size,
      percentage: (this.uploadedBytes / this.file.size) * 100,
      speed,
      remainingTime,
      status: "uploading",
    };

    this.options.onProgress(progress);
  }
}

/**
 * GridFS 断点续传下载器
 */
export class GridFSDownloader {
  private abortController: AbortController | null = null;
  private options: Required<DownloadOptions>;

  constructor(options: DownloadOptions) {
    this.options = {
      filename: "download",
      headers: {},
      onProgress: () => {},
      onError: () => {},
      url: "",
      ...options,
    } as Required<DownloadOptions>;
  }

  /**
   * 获取文件下载链接 (静态辅助方法)
   * @param fileId 文件 ID
   * @param options 可选配置 (url, headers)
   */
  static getUrl(
    fileId: string,
    options?: { url?: string; headers?: Record<string, string> }
  ): string {
    const host = (options?.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    let url = `${apiBase}/StreamRange/${fileId}`;

    const auth =
      options?.headers?.["Authorization"] ||
      options?.headers?.["authorization"];
    if (auth) {
      const token = auth.replace(/^Bearer\s+/i, "");
      const separator = url.includes("?") ? "&" : "?";
      url += `${separator}access_token=${encodeURIComponent(token)}`;
    }
    return url;
  }

  /**
   * 开始下载 (支持断点续传)
   * @param getWritableStream 可选: 返回一个 WritableStream 用于流式保存
   */
  async start(
    getWritableStream?: (
      filename: string,
      total: number
    ) => Promise<WritableStream | null>
  ): Promise<Blob | void> {
    this.abortController = new AbortController();

    try {
      const startByte = 0;
      const headers: Record<string, string> = {
        ...this.options.headers,
      };

      if (startByte > 0) {
        headers["Range"] = `bytes=${startByte}-`;
      }

      const host = (this.options.url || "").replace(/\/+$/, "");
      const apiBase = `${host}/api/GridFS`;
      const response = await fetch(
        `${apiBase}/StreamRange/${this.options.fileId}`,
        {
          headers,
          signal: this.abortController.signal,
        }
      );

      if (!response.ok && response.status !== 206) {
        throw new Error(`下载失败: ${response.statusText}`);
      }

      const contentDisposition = response.headers.get("Content-Disposition");
      if (contentDisposition) {
        const filenameMatch = contentDisposition.match(
          /filename\*?=(?:UTF-8'')?["']?([^"';]+)["']?/i
        );
        if (filenameMatch && filenameMatch[1]) {
          this.options.filename = decodeURIComponent(filenameMatch[1]);
        }
      }

      const contentRange = response.headers.get("Content-Range");
      const totalSize = contentRange
        ? parseInt(contentRange.split("/")[1])
        : parseInt(response.headers.get("Content-Length") || "0");

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error("无法读取响应流");
      }

      let writer: WritableStreamDefaultWriter<Uint8Array> | null = null;
      if (getWritableStream) {
        const stream = await getWritableStream(
          this.options.filename,
          totalSize
        );
        if (stream) {
          writer = stream.getWriter();
        }
      }

      const chunks: Uint8Array[] = [];
      let receivedLength = startByte;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        if (writer) {
          await writer.write(value);
        } else {
          chunks.push(value);
        }

        receivedLength += value.length;

        this.options.onProgress?.({
          loaded: receivedLength,
          total: totalSize,
          percentage: (receivedLength / totalSize) * 100,
        });
      }

      if (writer) {
        await writer.close();
        return;
      }

      const blob = new Blob(chunks as BlobPart[]);
      return blob;
    } catch (error) {
      this.options.onError?.(error as Error);
      throw error;
    }
  }

  /**
   * 取消下载
   */
  cancel(): void {
    this.abortController?.abort();
  }

  /**
   * 获取下载链接 (带鉴权参数)
   */
  getDownloadUrl(): string {
    return GridFSDownloader.getUrl(this.options.fileId, {
      url: this.options.url,
      headers: this.options.headers,
    });
  }

  /**
   * 使用浏览器原生下载
   */
  async downloadAndSave(): Promise<void> {
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
   * 保存文件到本地
   */
  saveFile(blob: Blob, filename: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = filename;
    a.click();
    URL.revokeObjectURL(url);
  }
}

/**
 * 格式化文件大小
 */
export function formatFileSize(bytes: number): string {
  if (bytes === 0) return "0 B";
  const k = 1024;
  const sizes = ["B", "KB", "MB", "GB", "TB"];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${(bytes / Math.pow(k, i)).toFixed(2)} ${sizes[i]}`;
}

/**
 * 格式化时间
 */
export function formatTime(seconds: number): string {
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
async function calculateHash(blob: Blob): Promise<string> {
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
type HashProgress = { loaded: number; total: number; percentage: number };

async function calculateSHA256(
  file: File,
  onProgress?: (progress: HashProgress) => void
): Promise<string> {
  // 优先使用 Web Worker + hash-wasm, 避免主线程阻塞
  try {
    return await calculateSHA256WithWorker(file, onProgress);
  } catch (e) {
    console.warn("Worker 哈希计算失败, 回退到主线程", e);
  }

  // 回退: 主线程 hash-wasm (依旧较快, 但可能阻塞)
  try {
    // @ts-ignore
    if (typeof hashwasm === "undefined") {
      await new Promise<void>((resolve, reject) => {
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

    // @ts-ignore
    if (typeof hashwasm !== "undefined") {
      // @ts-ignore
      const hasher = await hashwasm.createSHA256();
      hasher.init();

      const reader = file.stream().getReader();
      let processed = 0;
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        hasher.update(value);
        processed += value.length;
        onProgress?.({
          loaded: processed,
          total: file.size,
          percentage: (processed / file.size) * 100,
        });
      }
      return (hasher.digest() as string).toUpperCase();
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
  let processed = 0;

  if (typeof sha256 !== "undefined") {
    return new Promise((resolve, reject) => {
      const hasher = sha256.create();
      const fileReader = new FileReader();

      fileReader.onload = (e) => {
        try {
          const buffer = e.target?.result as ArrayBuffer;
          hasher.update(buffer);
          currentChunk++;
          processed += buffer.byteLength;
          onProgress?.({
            loaded: processed,
            total: file.size,
            percentage: (processed / file.size) * 100,
          });

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
  const hash = await calculateHash(file);
  onProgress?.({ loaded: file.size, total: file.size, percentage: 100 });
  return hash.toUpperCase();
}

async function calculateSHA256WithWorker(
  file: File,
  onProgress?: (progress: HashProgress) => void
): Promise<string> {
  const workerUrl = new URL("./hash-worker.js", import.meta.url);
  return new Promise((resolve, reject) => {
    const worker = new Worker(workerUrl);
    let aborted = false;

    type WorkerMessage =
      | { type: "ready"; impl: string }
      | { type: "progress"; loaded: number; total: number }
      | { type: "done"; hash: string }
      | { type: "error"; error: string };

    worker.onmessage = (event) => {
      const message = event.data as WorkerMessage;
      switch (message.type) {
        case "progress":
          onProgress?.({
            loaded: message.loaded,
            total: message.total,
            percentage: (message.loaded / message.total) * 100,
          });
          break;
        case "done":
          worker.terminate();
          resolve(message.hash.toUpperCase());
          break;
        case "error":
          worker.terminate();
          reject(new Error(message.error));
          break;
        default:
          break;
      }
    };

    worker.onerror = (err) => {
      if (!aborted) {
        reject(err);
      }
      worker.terminate();
    };

    worker.postMessage({ type: "start", size: file.size });

    (async () => {
      const reader = file.stream().getReader();
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        worker.postMessage({ type: "chunk", chunk: value }, [value.buffer]);
      }
      worker.postMessage({ type: "finalize" });
    })().catch((err) => {
      aborted = true;
      worker.terminate();
      reject(err);
    });
  });
}
