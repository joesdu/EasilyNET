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
export class GridFSUploader {
  constructor(file, options) {
    this.chunks = [];
    this.uploadId = "";
    this.abortController = null;
    this.startTime = 0;
    this.uploadedBytes = 0;
    this.isPaused = false;
    this.elapsedBeforePause = 0;
    this.hashPromise = null;
    this.file = file;
    this.options = {
      chunkSize: 1024 * 1024, // 1MB
      maxConcurrent: Math.min(
        6,
        Math.max(2, navigator?.hardwareConcurrency ?? 4)
      ),
      retryCount: 3,
      verifyOnServer: true,
      enableClientValidation: true,
      enableMagicNumberValidation: true,
      allowedExtensions: [],
      allowedContentTypes: [],
      maxFileSize: 0,
      headers: {},
      metadata: {},
      onProgress: () => {},
      onError: () => {},
      onValidationError: () => {},
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
      await this.resume();
      return this.uploadId;
    }
    this.isPaused = false;
    this.abortController = new AbortController();
    this.startTime = performance.now();
    this.elapsedBeforePause = 0;
    this.uploadedBytes = 0;
    try {
      await this.validateFileBeforeUpload();
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
      this.handleError(error);
      throw error;
    }
  }
  /**
   * 暂停上传
   */
  pause() {
    this.isPaused = true;
    this.elapsedBeforePause += performance.now() - this.startTime;
    this.abortController?.abort();
  }
  /**
   * 恢复上传
   */
  async resume() {
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
      this.handleError(error);
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
      const errorText = await response.text();
      throw new Error(`初始化上传失败: ${errorText || response.statusText}`);
    }
    const { sessionId, chunkSize, status, fileId } = await response.json();
    return { sessionId, chunkSize, status, fileId };
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
            const isAbort = error?.name === "AbortError";
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
  async uploadChunk(chunk, internalRetry = 0) {
    const blob = this.file.slice(chunk.start, chunk.end);
    const chunkHash = await calculateHash(blob);
    const host = (this.options.url || "").replace(/\/+$/, "");
    const apiBase = `${host}/api/GridFS`;
    let response;
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
      const isAbort = error?.name === "AbortError";
      if (isAbort) {
        return; // Always return on abort, regardless of pause state
      }
      throw error;
    }
    if (!response.ok) {
      const errorText = await response.text();
      if (isHashError(errorText) && internalRetry < 3) {
        console.warn(
          `分块 ${chunk.index} 哈希校验失败，正在重试 (${ 
            internalRetry + 1
          }/3)...`
        );
        return this.uploadChunk(chunk, internalRetry + 1);
      }
      if (isValidationError(errorText, response.status)) {
        throw createValidationError(
          `文件校验失败: ${errorText || response.statusText}`
        );
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
      const errorText = await response.text();
      throw new Error(`完成上传失败: ${errorText || response.statusText}`);
    }
    const { fileId } = await response.json();
    return fileId;
  }
  /**
   * 取消上传
   */
  async abortUpload() {
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
  updateProgress() {
    const elapsed =
      (this.elapsedBeforePause + (performance.now() - this.startTime)) / 1000; // 秒
    const speed = elapsed > 0 ? this.uploadedBytes / elapsed : 0;
    const remainingBytes = this.file.size - this.uploadedBytes;
    const remainingTime = speed > 0 ? remainingBytes / speed : 0;
    const progress = {
      loaded: this.uploadedBytes,
      total: this.file.size,
      percentage: (this.uploadedBytes / this.file.size) * 100,
      speed,
      remainingTime,
      status: "uploading",
    };
    this.options.onProgress(progress);
  }

  async validateFileBeforeUpload() {
    if (!this.options.enableClientValidation) return;
    const extension = normalizeExtension(getFileExtension(this.file.name));
    const contentType = (this.file.type || "").trim();
    if (this.options.maxFileSize > 0 && this.file.size > this.options.maxFileSize) {
      throw createValidationError(
        `文件大小超过限制: ${formatFileSize(this.options.maxFileSize)}`
      );
    }
    if (this.options.allowedExtensions.length > 0 && extension) {
      if (!this.options.allowedExtensions.includes(extension)) {
        throw createValidationError(`不允许的文件扩展名: ${extension}`);
      }
    }
    if (this.options.allowedContentTypes.length > 0 && contentType) {
      if (!this.options.allowedContentTypes.includes(contentType)) {
        throw createValidationError(`不允许的内容类型: ${contentType}`);
      }
    }
    const mappedContentType = ExtensionContentTypeMap[extension];
    if (mappedContentType && contentType && mappedContentType !== contentType) {
      throw createValidationError(
        `内容类型与扩展名不一致: ${extension} -> ${contentType}`
      );
    }
    if (!this.options.enableMagicNumberValidation) return;
    const header = await readFileHeader(this.file, 512);
    if (!isMagicNumberValid(extension, header)) {
      throw createValidationError(`文件签名与扩展名不一致: ${extension}`);
    }
  }

  handleError(error) {
    if (error?.name === "ValidationError") {
      this.options.onValidationError?.(error.message);
    }
    this.options.onError(error);
  }
}
/**
 * GridFS 断点续传下载器
 */
export class GridFSDownloader {
  constructor(options) {
    this.abortController = null;
    this.options = {
      filename: "download",
      headers: {},
      onProgress: () => {},
      onError: () => {},
      url: "",
      ...options,
    };
  }
  /**
   * 获取文件下载链接 (静态辅助方法)
   * @param fileId 文件 ID
   * @param options 可选配置 (url, headers)
   */
  static getUrl(fileId, options) {
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
  async start(getWritableStream) {
    this.abortController = new AbortController();
    try {
      const startByte = 0;
      const headers = {
        ...this.options.headers,
      };
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
      let writer = null;
      if (getWritableStream) {
        const stream = await getWritableStream(
          this.options.filename,
          totalSize
        );
        if (stream) {
          writer = stream.getWriter();
        }
      }
      const chunks = [];
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
      const blob = new Blob(chunks);
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
   * 获取下载链接 (带鉴权参数)
   */
  getDownloadUrl() {
    return GridFSDownloader.getUrl(this.options.fileId, {
      url: this.options.url,
      headers: this.options.headers,
    });
  }
  /**
   * 使用浏览器原生下载
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

const ExtensionContentTypeMap = {
  ".jpg": "image/jpeg",
  ".jpeg": "image/jpeg",
  ".png": "image/png",
  ".gif": "image/gif",
  ".bmp": "image/bmp",
  ".webp": "image/webp",
  ".tif": "image/tiff",
  ".tiff": "image/tiff",
  ".heic": "image/heic",
  ".heif": "image/heif",
  ".svg": "image/svg+xml",
  ".psd": "image/vnd.adobe.photoshop",
  ".ai": "application/postscript",
  ".pdf": "application/pdf",
  ".mp3": "audio/mpeg",
  ".wav": "audio/wav",
  ".flac": "audio/flac",
  ".ogg": "audio/ogg",
  ".oga": "audio/ogg",
  ".opus": "audio/opus",
  ".aac": "audio/aac",
  ".mp4": "video/mp4",
  ".m4v": "video/x-m4v",
  ".mov": "video/quicktime",
  ".avi": "video/x-msvideo",
  ".mkv": "video/x-matroska",
  ".webm": "video/webm",
  ".flv": "video/x-flv",
  ".zip": "application/zip",
  ".7z": "application/x-7z-compressed",
  ".rar": "application/vnd.rar",
  ".gz": "application/gzip",
  ".tar": "application/x-tar",
  ".apk": "application/vnd.android.package-archive",
  ".jar": "application/java-archive",
  ".war": "application/java-archive",
  ".epub": "application/epub+zip",
  ".mobi": "application/x-mobipocket-ebook",
  ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
  ".xlsx": "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
  ".pptx": "application/vnd.openxmlformats-officedocument.presentationml.presentation",
  ".exe": "application/vnd.microsoft.portable-executable",
  ".dll": "application/vnd.microsoft.portable-executable",
  ".msi": "application/x-msi",
  ".cab": "application/vnd.ms-cab-compressed",
  ".elf": "application/x-elf",
  ".macho": "application/x-mach-binary",
  ".wasm": "application/wasm",
  ".iso": "application/x-iso9660-image",
  ".dmg": "application/x-apple-diskimage",
  ".dwg": "image/vnd.dwg",
  ".dxf": "image/vnd.dxf",
};

const MagicNumberSignatures = {
  ".jpg": [new Uint8Array([0xff, 0xd8, 0xff])],
  ".jpeg": [new Uint8Array([0xff, 0xd8, 0xff])],
  ".png": [new Uint8Array([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a])],
  ".gif": [
    new Uint8Array([0x47, 0x49, 0x46, 0x38, 0x37, 0x61]),
    new Uint8Array([0x47, 0x49, 0x46, 0x38, 0x39, 0x61]),
  ],
  ".bmp": [new Uint8Array([0x42, 0x4d])],
  ".webp": [new Uint8Array([0x52, 0x49, 0x46, 0x46])],
  ".pdf": [new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2d])],
  ".tif": [new Uint8Array([0x49, 0x49, 0x2a, 0x00]), new Uint8Array([0x4d, 0x4d, 0x00, 0x2a])],
  ".tiff": [new Uint8Array([0x49, 0x49, 0x2a, 0x00]), new Uint8Array([0x4d, 0x4d, 0x00, 0x2a])],
  ".mp3": [new Uint8Array([0x49, 0x44, 0x33]), new Uint8Array([0xff, 0xfb])],
  ".wav": [new Uint8Array([0x52, 0x49, 0x46, 0x46])],
  ".flac": [new Uint8Array([0x66, 0x4c, 0x61, 0x43])],
  ".mp4": [new Uint8Array([0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70]), new Uint8Array([0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70])],
  ".m4v": [new Uint8Array([0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70]), new Uint8Array([0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70])],
  ".mov": [new Uint8Array([0x00, 0x00, 0x00, 0x14, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20])],
  ".avi": [new Uint8Array([0x52, 0x49, 0x46, 0x46])],
  ".mkv": [new Uint8Array([0x1a, 0x45, 0xdf, 0xa3])],
  ".webm": [new Uint8Array([0x1a, 0x45, 0xdf, 0xa3])],
  ".flv": [new Uint8Array([0x46, 0x4c, 0x56])],
  ".7z": [new Uint8Array([0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c])],
  ".rar": [new Uint8Array([0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x00]), new Uint8Array([0x52, 0x61, 0x72, 0x21, 0x1a, 0x07, 0x01, 0x00])],
  ".zip": [new Uint8Array([0x50, 0x4b, 0x03, 0x04]), new Uint8Array([0x50, 0x4b, 0x05, 0x06]), new Uint8Array([0x50, 0x4b, 0x07, 0x08])],
  ".gz": [new Uint8Array([0x1f, 0x8b])],
  ".tar": [new Uint8Array([0x75, 0x73, 0x74, 0x61, 0x72])],
  ".apk": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".jar": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".war": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".epub": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".docx": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".xlsx": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".pptx": [new Uint8Array([0x50, 0x4b, 0x03, 0x04])],
  ".mobi": [new Uint8Array([0x42, 0x4f, 0x4f, 0x4b, 0x4d, 0x4f, 0x42, 0x49])],
  ".exe": [new Uint8Array([0x4d, 0x5a])],
  ".dll": [new Uint8Array([0x4d, 0x5a])],
  ".msi": [new Uint8Array([0xd0, 0xcf, 0x11, 0xe0, 0xa1, 0xb1, 0x1a, 0xe1])],
  ".cab": [new Uint8Array([0x4d, 0x53, 0x43, 0x46])],
  ".elf": [new Uint8Array([0x7f, 0x45, 0x4c, 0x46])],
  ".macho": [
    new Uint8Array([0xfe, 0xed, 0xfa, 0xce]),
    new Uint8Array([0xfe, 0xed, 0xfa, 0xcf]),
    new Uint8Array([0xce, 0xfa, 0xed, 0xfe]),
    new Uint8Array([0xcf, 0xfa, 0xed, 0xfe]),
  ],
  ".wasm": [new Uint8Array([0x00, 0x61, 0x73, 0x6d])],
  ".iso": [new Uint8Array([0x43, 0x44, 0x30, 0x30, 0x31])],
  ".dmg": [new Uint8Array([0x78, 0x01, 0x73, 0x0d, 0x62, 0x62, 0x60])],
  ".heic": [new Uint8Array([0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63])],
  ".heif": [new Uint8Array([0x66, 0x74, 0x79, 0x70, 0x68, 0x65, 0x69, 0x63])],
  ".svg": [new Uint8Array([0x3c, 0x73, 0x76, 0x67])],
  ".psd": [new Uint8Array([0x38, 0x42, 0x50, 0x53])],
  ".ai": [new Uint8Array([0x25, 0x50, 0x44, 0x46, 0x2d])],
  ".dwg": [new Uint8Array([0x41, 0x43, 0x31, 0x30])],
  ".dxf": [new Uint8Array([0x30, 0x20])],
  ".ogg": [new Uint8Array([0x4f, 0x67, 0x67, 0x53])],
  ".oga": [new Uint8Array([0x4f, 0x67, 0x67, 0x53])],
  ".opus": [new Uint8Array([0x4f, 0x67, 0x67, 0x53])],
  ".aac": [new Uint8Array([0xff, 0xf1]), new Uint8Array([0xff, 0xf9])],
};

function normalizeExtension(extension) {
  if (!extension) return "";
  const trimmed = extension.trim();
  return trimmed.startsWith(".") ? trimmed.toLowerCase() : `.${trimmed.toLowerCase()}`;
}

function getFileExtension(filename) {
  const lastDot = filename.lastIndexOf(".");
  if (lastDot === -1) return "";
  return filename.slice(lastDot);
}

async function readFileHeader(file, length) {
  const buffer = await file.slice(0, length).arrayBuffer();
  return new Uint8Array(buffer);
}

function isMagicNumberValid(extension, header) {
  if (!extension) return true;
  const signatures = MagicNumberSignatures[extension];
  if (!signatures) return true;
  const headerSlice = header.subarray(0, Math.min(header.length, 512));
  for (const signature of signatures) {
    if (headerSlice.length >= signature.length && compareBytes(headerSlice, signature, 0)) {
      return true;
    }
    if (extension === ".pdf" && indexOfSequence(headerSlice, signature) >= 0) {
      return true;
    }
    if (extension === ".tar" && headerSlice.length >= 265 && compareBytes(headerSlice, signature, 257)) {
      return true;
    }
    if ((extension === ".mp4" || extension === ".m4v" || extension === ".mov") && indexOfSequence(headerSlice, new TextEncoder().encode("ftyp")) >= 4) {
      return true;
    }
    if (extension === ".webp" && headerSlice.length >= 12 && compareBytes(headerSlice, new TextEncoder().encode("WEBP"), 8)) {
      return true;
    }
    if (extension === ".wav" && headerSlice.length >= 12 && compareBytes(headerSlice, new TextEncoder().encode("WAVE"), 8)) {
      return true;
    }
    if (extension === ".avi" && headerSlice.length >= 12 && compareBytes(headerSlice, new TextEncoder().encode("AVI "), 8)) {
      return true;
    }
    if ((extension === ".docx" || extension === ".xlsx" || extension === ".pptx" || extension === ".apk" || extension === ".jar" || extension === ".war" || extension === ".epub") && headerSlice.length >= 4) {
      if (compareBytes(headerSlice, new Uint8Array([0x50, 0x4b, 0x03, 0x04]), 0)) {
        return true;
      }
    }
    if ((extension === ".ogg" || extension === ".oga" || extension === ".opus") && headerSlice.length >= 4) {
      if (compareBytes(headerSlice, new TextEncoder().encode("OggS"), 0)) {
        return true;
      }
    }
    if ((extension === ".heic" || extension === ".heif") && headerSlice.length >= 12) {
      if (compareBytes(headerSlice, new TextEncoder().encode("ftypheic"), 4)) {
        return true;
      }
    }
    if (extension === ".svg" && indexOfSequence(headerSlice, new TextEncoder().encode("<svg")) >= 0) {
      return true;
    }
    if (extension === ".mobi" && headerSlice.length >= 68 && compareBytes(headerSlice, new TextEncoder().encode("BOOKMOBI"), 60)) {
      return true;
    }
    if (extension === ".iso" && headerSlice.length >= 9 && compareBytes(headerSlice, new Uint8Array([0x43, 0x44, 0x30, 0x30, 0x31]), 1)) {
      return true;
    }
    if (extension === ".wasm" && headerSlice.length >= 4 && compareBytes(headerSlice, new TextEncoder().encode("\0asm"), 0)) {
      return true;
    }
  }
  return false;
}

function compareBytes(source, target, offset) {
  if (offset + target.length > source.length) return false;
  for (let i = 0; i < target.length; i++) {
    if (source[offset + i] !== target[i]) return false;
  }
  return true;
}

function indexOfSequence(source, target) {
  if (target.length === 0 || source.length < target.length) return -1;
  for (let i = 0; i <= source.length - target.length; i++) {
    if (compareBytes(source, target, i)) return i;
  }
  return -1;
}

function isHashError(message) {
  const text = message.toLowerCase();
  return text.includes("hash") || text.includes("哈希");
}

function isValidationError(message, status) {
  const text = message.toLowerCase();
  const keywords = ["signature", "content", "type", "extension", "size", "limit", "magic", "签名", "类型", "扩展名", "大小", "校验"];
  if (status && [400, 413, 415].includes(status)) {
    return keywords.some((k) => text.includes(k));
  }
  return keywords.some((k) => text.includes(k));
}

function createValidationError(message) {
  const error = new Error(message);
  error.name = "ValidationError";
  return error;
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
 * 计算文件的 SHA256 哈希（流式处理）
 * @param file 要计算哈希的文件
 * @param onProgress 可选，哈希计算进度回调
 * @returns 返回大写 SHA256 十六进制字符串的 Promise
 */
async function calculateSHA256(file, onProgress) {
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
  let processed = 0;
  if (typeof sha256 !== "undefined") {
    return new Promise((resolve, reject) => {
      const hasher = sha256.create();
      const fileReader = new FileReader();
      fileReader.onload = (e) => {
        try {
          const buffer = e.target?.result;
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
async function calculateSHA256WithWorker(file, onProgress) {
  const workerUrl = new URL("./hash-worker.js", import.meta.url);
  return new Promise((resolve, reject) => {
    const worker = new Worker(workerUrl);
    let settled = false;
    // Throttle progress updates: at most every 100ms or every 1% progress
    let lastProgressTime = 0;
    let lastProgressPercent = 0;
    worker.onmessage = (event) => {
      const message = event.data;
      switch (message.type) {
        case "progress": {
          const now = Date.now();
          const percent = (message.loaded / message.total) * 100;
          if (!onProgress) {
            // do nothing
          } else if (
            now - lastProgressTime > 100 ||
            percent - lastProgressPercent >= 1 ||
            percent === 100
          ) {
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
        case "done":
          settled = true;
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
      if (!settled) {
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
        const copy = value.slice(); // Creates a copy with its own buffer
        worker.postMessage({ type: "chunk", chunk: copy }, [copy.buffer]);
      }
      worker.postMessage({ type: "finalize" });
    })().catch((err) => {
      settled = true;
      worker.terminate();
      reject(err);
    });
  });
}
