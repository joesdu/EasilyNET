/**
 * @easilynet/gridfs-sdk - Downloader
 * File downloader with streaming support
 */

import type { DownloadOptions, DownloadProgress } from './types';
import { GridFSApiClient } from './api-client';

/**
 * Default download options
 */
const DEFAULT_OPTIONS: Required<Omit<DownloadOptions, 'onProgress' | 'onError'>> = {
  baseUrl: '',
  fileId: '',
  filename: '',
  headers: {},
};

/**
 * GridFS file downloader
 */
export class GridFSDownloader {
  private readonly options: Required<DownloadOptions>;
  private readonly apiClient: GridFSApiClient;
  private abortController: AbortController | null = null;
  private startTime = 0;

  constructor(options: DownloadOptions) {
    this.options = {
      ...DEFAULT_OPTIONS,
      onProgress: () => {},
      onError: () => {},
      ...options,
    };

    this.apiClient = new GridFSApiClient({
      baseUrl: this.options.baseUrl,
      headers: this.options.headers,
    });
  }

  /**
   * Get the download URL for a file
   */
  static getUrl(fileId: string, options?: { baseUrl?: string; headers?: Record<string, string> }): string {
    const client = new GridFSApiClient({
      baseUrl: options?.baseUrl ?? '',
      headers: options?.headers,
    });
    return client.getStreamUrl(fileId);
  }

  /**
   * Get the download URL for this file
   */
  getDownloadUrl(): string {
    return this.apiClient.getStreamUrl(this.options.fileId);
  }

  /**
   * Start downloading the file
   * @param getWritableStream Optional function to get a WritableStream for streaming save
   */
  async start(
    getWritableStream?: (filename: string, total: number) => Promise<WritableStream<Uint8Array> | null>
  ): Promise<Blob | void> {
    this.abortController = new AbortController();
    this.startTime = performance.now();

    try {
      const url = this.getDownloadUrl();
      const response = await fetch(url, {
        headers: this.options.headers,
        signal: this.abortController.signal,
      });

      if (!response.ok && response.status !== 206) {
        throw new Error(`Download failed: ${response.status} ${response.statusText}`);
      }

      // Extract filename from Content-Disposition header
      const contentDisposition = response.headers.get('Content-Disposition');
      if (contentDisposition && !this.options.filename) {
        const filenameMatch = contentDisposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)["']?/i);
        if (filenameMatch?.[1]) {
          this.options.filename = decodeURIComponent(filenameMatch[1]);
        }
      }

      // Get total size
      const contentRange = response.headers.get('Content-Range');
      const totalSize = contentRange
        ? parseInt(contentRange.split('/')[1])
        : parseInt(response.headers.get('Content-Length') ?? '0');

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error('Cannot read response stream');
      }

      // Get writable stream if provided
      let writer: WritableStreamDefaultWriter<Uint8Array> | null = null;
      if (getWritableStream) {
        const stream = await getWritableStream(this.options.filename, totalSize);
        if (stream) {
          writer = stream.getWriter();
        }
      }

      const chunks: Uint8Array[] = [];
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
  cancel(): void {
    this.abortController?.abort();
  }

  /**
   * Download and save using browser's native download
   */
  async downloadAndSave(): Promise<void> {
    const url = this.getDownloadUrl();
    const a = document.createElement('a');
    a.style.display = 'none';
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
  saveBlob(blob: Blob, filename?: string): void {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename ?? this.options.filename ?? 'download';
    a.click();
    URL.revokeObjectURL(url);
  }

  /**
   * Emit progress event
   */
  private emitProgress(loaded: number, total: number): void {
    const elapsed = (performance.now() - this.startTime) / 1000;
    const speed = elapsed > 0 ? loaded / elapsed : 0;

    const progress: DownloadProgress = {
      loaded,
      total,
      percentage: total > 0 ? (loaded / total) * 100 : 0,
      speed,
    };

    this.options.onProgress(progress);
  }
}
