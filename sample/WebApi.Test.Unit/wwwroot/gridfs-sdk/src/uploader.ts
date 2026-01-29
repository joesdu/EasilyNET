/**
 * @easilynet/gridfs-sdk - Uploader
 * Chunked file uploader with resumable support
 */

import type {
  UploadOptions,
  UploadProgress,
  UploadPhase,
  UploadState,
  ChunkTask,
} from './types';
import { GridFSApiClient } from './api-client';
import { calculateFileHash, calculateBlobHash, retry } from './utils';

/**
 * Default upload options
 */
const DEFAULT_OPTIONS: Required<Omit<UploadOptions, 'onProgress' | 'onError' | 'onComplete' | 'onStateChange'>> = {
  baseUrl: '',
  chunkSize: 1024 * 1024, // 1MB default, will be overridden by server
  maxConcurrent: Math.min(6, Math.max(2, typeof navigator !== 'undefined' ? navigator.hardwareConcurrency ?? 4 : 4)),
  retryCount: 3,
  retryDelay: 1000,
  verifyOnServer: true,
  headers: {},
  metadata: {},
};

/**
 * GridFS chunked file uploader
 */
export class GridFSUploader {
  private readonly file: File;
  private readonly options: Required<UploadOptions>;
  private readonly apiClient: GridFSApiClient;

  private chunks: ChunkTask[] = [];
  private sessionId = '';
  private abortController: AbortController | null = null;
  private startTime = 0;
  private uploadedBytes = 0;
  private elapsedBeforePause = 0;
  private cachedFileHash?: string;
  private hashPromise: Promise<string> | null = null;

  private _state: UploadState = 'idle';
  private _phase: UploadPhase = 'idle';

  constructor(file: File, options: UploadOptions = {}) {
    this.file = file;
    this.options = {
      ...DEFAULT_OPTIONS,
      onProgress: () => {},
      onError: () => {},
      onComplete: () => {},
      onStateChange: () => {},
      ...options,
    };

    this.apiClient = new GridFSApiClient({
      baseUrl: this.options.baseUrl,
      headers: this.options.headers,
    });
  }

  /**
   * Get current upload state
   */
  get state(): UploadState {
    return this._state;
  }

  /**
   * Get current upload phase
   */
  get phase(): UploadPhase {
    return this._phase;
  }

  /**
   * Get session ID (available after upload starts)
   */
  get uploadSessionId(): string {
    return this.sessionId;
  }

  /**
   * Start the upload
   */
  async start(): Promise<string> {
    if (this._state === 'paused') {
      return this.resume();
    }

    this.setState('preparing');
    this.setPhase('hashing');
    this.abortController = new AbortController();
    this.startTime = performance.now();
    this.elapsedBeforePause = 0;
    this.uploadedBytes = 0;

    try {
      // Calculate file hash for deduplication
      this.hashPromise = calculateFileHash(this.file, (hashProgress) => {
        this.emitProgress({
          loaded: 0,
          total: this.file.size,
          percentage: 0,
          speed: 0,
          remainingTime: 0,
          phase: 'hashing',
          hashProgress: hashProgress.percentage,
          chunksUploaded: 0,
          totalChunks: 0,
        });
      });

      const fileHash = this.cachedFileHash ?? await this.hashPromise.catch((e) => {
        console.warn('Hash calculation failed:', e);
        return undefined;
      });

      if (fileHash) {
        this.cachedFileHash = fileHash;
      }

      if ((this._state as UploadState) === 'paused' || (this._state as UploadState) === 'cancelled') {
        return this.sessionId;
      }

      // Create upload session
      this.setPhase('creating-session');
      const sessionInfo = await this.apiClient.createSession(
        this.file.name,
        this.file.size,
        fileHash,
        this.file.type || undefined,
        this.abortController.signal
      );

      this.sessionId = sessionInfo.sessionId;

      // Check for instant upload (deduplication)
      if (sessionInfo.status === 'Completed' && sessionInfo.fileId) {
        this.setPhase('completed');
        this.setState('completed');
        this.emitProgress({
          loaded: this.file.size,
          total: this.file.size,
          percentage: 100,
          speed: 0,
          remainingTime: 0,
          phase: 'completed',
          chunksUploaded: 0,
          totalChunks: 0,
        });
        this.options.onComplete(sessionInfo.fileId);
        return sessionInfo.fileId;
      }

      // Use server-provided chunk size
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }

      // Initialize chunks
      this.initializeChunks();

      // Start uploading
      this.setState('uploading');
      this.setPhase('uploading');
      await this.uploadChunks();

      if ((this._state as UploadState) === 'paused' || (this._state as UploadState) === 'cancelled') {
        return this.sessionId;
      }

      // Finalize upload
      this.setPhase('finalizing');
      const finalHash = this.cachedFileHash ?? await this.hashPromise?.catch(() => undefined);
      const result = await this.apiClient.finalize(
        this.sessionId,
        finalHash,
        !this.options.verifyOnServer,
        this.abortController.signal
      );

      this.setPhase('completed');
      this.setState('completed');
      this.options.onComplete(result.fileId);
      return result.fileId;

    } catch (error) {
      this.setPhase('error');
      this.setState('error');
      const err = error instanceof Error ? error : new Error(String(error));
      this.options.onError(err);
      throw err;
    }
  }

  /**
   * Pause the upload
   */
  pause(): void {
    if (this._state !== 'uploading') return;

    this.setState('paused');
    this.elapsedBeforePause += performance.now() - this.startTime;
    this.abortController?.abort();
  }

  /**
   * Resume a paused upload
   */
  async resume(): Promise<string> {
    if (this._state !== 'paused') {
      throw new Error('Cannot resume: upload is not paused');
    }

    if (!this.sessionId) {
      throw new Error('Cannot resume: no session ID');
    }

    this.setState('uploading');
    this.setPhase('uploading');
    this.abortController = new AbortController();
    this.startTime = performance.now();

    try {
      // Get session info from server
      const sessionInfo = await this.apiClient.getSession(this.sessionId, this.abortController.signal);

      // Check if already completed
      if (sessionInfo.status === 'Completed') {
        this.setPhase('completed');
        this.setState('completed');
        // Session doesn't have fileId in response, need to finalize
        const result = await this.apiClient.finalize(this.sessionId);
        this.options.onComplete(result.fileId);
        return result.fileId;
      }

      // Update chunk size if needed
      if (sessionInfo.chunkSize) {
        this.options.chunkSize = sessionInfo.chunkSize;
      }

      // Reinitialize chunks if needed
      if (this.chunks.length === 0) {
        this.initializeChunks();
      }

      // Mark uploaded chunks
      const uploadedSet = new Set(sessionInfo.uploadedChunks ?? []);
      let uploadedBytes = 0;
      for (const chunk of this.chunks) {
        if (uploadedSet.has(chunk.index)) {
          chunk.uploaded = true;
          uploadedBytes += chunk.end - chunk.start;
        }
      }
      this.uploadedBytes = uploadedBytes;

      // Continue uploading
      await this.uploadChunks();

      if (this._state === 'paused' || this._state === 'cancelled') {
        return this.sessionId;
      }

      // Finalize
      this.setPhase('finalizing');
      const finalHash = this.cachedFileHash ?? await this.hashPromise?.catch(() => undefined);
      const result = await this.apiClient.finalize(
        this.sessionId,
        finalHash,
        !this.options.verifyOnServer,
        this.abortController.signal
      );

      this.setPhase('completed');
      this.setState('completed');
      this.options.onComplete(result.fileId);
      return result.fileId;

    } catch (error) {
      this.setPhase('error');
      this.setState('error');
      const err = error instanceof Error ? error : new Error(String(error));
      this.options.onError(err);
      throw err;
    }
  }

  /**
   * Cancel the upload
   */
  async cancel(): Promise<void> {
    this.setState('cancelled');
    this.abortController?.abort();

    if (this.sessionId) {
      try {
        await this.apiClient.cancel(this.sessionId);
      } catch {
        // Ignore cancel errors
      }
    }
  }

  /**
   * Initialize chunk tasks
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
   * Upload all pending chunks with concurrency control
   */
  private async uploadChunks(): Promise<void> {
    const pendingChunks = this.chunks.filter((c) => !c.uploaded);
    const queue = [...pendingChunks];
    const executing: Promise<void>[] = [];

    while (queue.length > 0 || executing.length > 0) {
      if (this._state === 'paused' || this._state === 'cancelled') break;

      // Fill up to max concurrent
      while (executing.length < this.options.maxConcurrent && queue.length > 0) {
        const chunk = queue.shift()!;
        const promise = this.uploadChunk(chunk)
          .then(() => {
            const index = executing.indexOf(promise);
            if (index > -1) executing.splice(index, 1);
          })
          .catch((error) => {
            const index = executing.indexOf(promise);
            if (index > -1) executing.splice(index, 1);

            const isAbort = error instanceof DOMException && error.name === 'AbortError';
            if (this._state === 'paused' && isAbort) {
              return;
            }

            // Retry logic
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

    // Wait for all remaining
    if (this._state !== 'paused' && this._state !== 'cancelled' && executing.length > 0) {
      await Promise.all(executing);
    }
  }

  /**
   * Upload a single chunk
   */
  private async uploadChunk(chunk: ChunkTask): Promise<void> {
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
  private updateProgress(): void {
    const elapsed = (this.elapsedBeforePause + (performance.now() - this.startTime)) / 1000;
    const speed = elapsed > 0 ? this.uploadedBytes / elapsed : 0;
    const remainingBytes = this.file.size - this.uploadedBytes;
    const remainingTime = speed > 0 ? remainingBytes / speed : 0;

    this.emitProgress({
      loaded: this.uploadedBytes,
      total: this.file.size,
      percentage: (this.uploadedBytes / this.file.size) * 100,
      speed,
      remainingTime,
      phase: this._phase,
      chunksUploaded: this.chunks.filter((c) => c.uploaded).length,
      totalChunks: this.chunks.length,
    });
  }

  /**
   * Emit progress event
   */
  private emitProgress(progress: UploadProgress): void {
    this.options.onProgress(progress);
  }

  /**
   * Set upload state
   */
  private setState(state: UploadState): void {
    this._state = state;
    this.options.onStateChange(state);
  }

  /**
   * Set upload phase
   */
  private setPhase(phase: UploadPhase): void {
    this._phase = phase;
  }
}
