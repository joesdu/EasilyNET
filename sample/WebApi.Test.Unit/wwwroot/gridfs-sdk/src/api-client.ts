/**
 * @easilynet/gridfs-sdk - API Client
 * HTTP client for GridFS server communication
 */

import type {
  CreateSessionResponse,
  UploadChunkResponse,
  SessionInfoResponse,
  FinalizeResponse,
} from './types';

/**
 * GridFS API client configuration
 */
export interface ApiClientConfig {
  baseUrl: string;
  headers?: Record<string, string>;
}

/**
 * GridFS API client for server communication
 */
export class GridFSApiClient {
  private readonly baseUrl: string;
  private readonly headers: Record<string, string>;

  constructor(config: ApiClientConfig) {
    this.baseUrl = config.baseUrl.replace(/\/+$/, '');
    this.headers = config.headers ?? {};
  }

  /**
   * Get the API base URL
   */
  get apiBase(): string {
    return `${this.baseUrl}/api/GridFS`;
  }

  /**
   * Create a new upload session
   */
  async createSession(
    filename: string,
    totalSize: number,
    fileHash?: string,
    contentType?: string,
    signal?: AbortSignal
  ): Promise<CreateSessionResponse> {
    const params = new URLSearchParams({
      filename,
      totalSize: totalSize.toString(),
    });

    if (contentType) {
      params.append('contentType', contentType);
    }
    if (fileHash) {
      params.append('fileHash', fileHash);
    }

    const response = await fetch(`${this.apiBase}/CreateSession?${params}`, {
      method: 'POST',
      headers: this.headers,
      signal,
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
  async uploadChunk(
    sessionId: string,
    chunkNumber: number,
    chunkHash: string,
    data: Blob,
    signal?: AbortSignal
  ): Promise<UploadChunkResponse> {
    const params = new URLSearchParams({
      sessionId,
      chunkNumber: chunkNumber.toString(),
      chunkHash,
    });

    const response = await fetch(`${this.apiBase}/UploadChunk?${params}`, {
      method: 'POST',
      headers: {
        ...this.headers,
        'Content-Type': 'application/octet-stream',
      },
      body: data,
      signal,
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
  async getSession(sessionId: string, signal?: AbortSignal): Promise<SessionInfoResponse> {
    const response = await fetch(`${this.apiBase}/Session/${sessionId}`, {
      method: 'GET',
      headers: this.headers,
      signal,
    });

    if (!response.ok) {
      throw new Error(`Failed to get session: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Get missing chunk numbers
   */
  async getMissingChunks(sessionId: string, signal?: AbortSignal): Promise<{ sessionId: string; missingChunks: number[] }> {
    const response = await fetch(`${this.apiBase}/MissingChunks/${sessionId}`, {
      method: 'GET',
      headers: this.headers,
      signal,
    });

    if (!response.ok) {
      throw new Error(`Failed to get missing chunks: ${response.status} ${response.statusText}`);
    }

    return response.json();
  }

  /**
   * Finalize the upload
   */
  async finalize(
    sessionId: string,
    fileHash?: string,
    skipHashValidation?: boolean,
    signal?: AbortSignal
  ): Promise<FinalizeResponse> {
    const params = new URLSearchParams();
    if (fileHash) {
      params.append('fileHash', fileHash);
    }
    if (skipHashValidation) {
      params.append('skipHashValidation', 'true');
    }

    const queryString = params.toString();
    const url = queryString
      ? `${this.apiBase}/Finalize/${sessionId}?${queryString}`
      : `${this.apiBase}/Finalize/${sessionId}`;

    const response = await fetch(url, {
      method: 'POST',
      headers: {
        ...this.headers,
        'Content-Type': 'application/json',
      },
      signal,
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
  async cancel(sessionId: string, signal?: AbortSignal): Promise<void> {
    const response = await fetch(`${this.apiBase}/Cancel/${sessionId}`, {
      method: 'DELETE',
      headers: this.headers,
      signal,
    });

    if (!response.ok) {
      throw new Error(`Failed to cancel upload: ${response.status} ${response.statusText}`);
    }
  }

  /**
   * Get stream URL for a file (for video/audio playback)
   */
  getStreamUrl(fileId: string): string {
    let url = `${this.apiBase}/StreamRange/${fileId}`;

    // Append auth token if present
    const auth = this.headers['Authorization'] ?? this.headers['authorization'];
    if (auth) {
      const token = auth.replace(/^Bearer\s+/i, '');
      url += `?access_token=${encodeURIComponent(token)}`;
    }

    return url;
  }
}
