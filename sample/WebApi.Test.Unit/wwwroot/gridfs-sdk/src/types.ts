/**
 * @easilynet/gridfs-sdk - Types
 * Modern GridFS client SDK for chunked file uploads
 */

// ============================================================================
// Configuration Types
// ============================================================================

/**
 * Upload configuration options
 */
export interface UploadOptions {
  /** Server base URL (e.g., 'https://api.example.com'), defaults to current origin */
  baseUrl?: string;
  /** Chunk size in bytes, defaults to server-provided value */
  chunkSize?: number;
  /** Maximum concurrent chunk uploads, defaults to hardware concurrency or 4 */
  maxConcurrent?: number;
  /** Number of retry attempts for failed chunks, defaults to 3 */
  retryCount?: number;
  /** Retry delay in milliseconds, defaults to 1000 */
  retryDelay?: number;
  /** Whether to verify hash on server after upload, defaults to true */
  verifyOnServer?: boolean;
  /** Additional HTTP headers */
  headers?: Record<string, string>;
  /** File metadata to store */
  metadata?: Record<string, unknown>;
  /** Progress callback */
  onProgress?: (progress: UploadProgress) => void;
  /** Error callback */
  onError?: (error: Error) => void;
  /** Completion callback */
  onComplete?: (fileId: string) => void;
  /** State change callback */
  onStateChange?: (state: UploadState) => void;
}

/**
 * Download configuration options
 */
export interface DownloadOptions {
  /** Server base URL */
  baseUrl?: string;
  /** File ID to download */
  fileId: string;
  /** Filename for saving (auto-detected from server if not provided) */
  filename?: string;
  /** Additional HTTP headers */
  headers?: Record<string, string>;
  /** Progress callback */
  onProgress?: (progress: DownloadProgress) => void;
  /** Error callback */
  onError?: (error: Error) => void;
}

// ============================================================================
// Progress Types
// ============================================================================

/**
 * Upload progress information
 */
export interface UploadProgress {
  /** Bytes uploaded so far */
  loaded: number;
  /** Total file size in bytes */
  total: number;
  /** Progress percentage (0-100) */
  percentage: number;
  /** Current upload speed in bytes/second */
  speed: number;
  /** Estimated remaining time in seconds */
  remainingTime: number;
  /** Current upload phase */
  phase: UploadPhase;
  /** Hash calculation progress (0-100) when phase is 'hashing' */
  hashProgress?: number;
  /** Number of chunks uploaded */
  chunksUploaded: number;
  /** Total number of chunks */
  totalChunks: number;
}

/**
 * Download progress information
 */
export interface DownloadProgress {
  /** Bytes downloaded so far */
  loaded: number;
  /** Total file size in bytes */
  total: number;
  /** Progress percentage (0-100) */
  percentage: number;
  /** Current download speed in bytes/second */
  speed: number;
}

// ============================================================================
// State Types
// ============================================================================

/**
 * Upload phases
 */
export type UploadPhase = 
  | 'idle'
  | 'hashing'
  | 'creating-session'
  | 'uploading'
  | 'finalizing'
  | 'completed'
  | 'error';

/**
 * Upload state
 */
export type UploadState = 
  | 'idle'
  | 'preparing'
  | 'uploading'
  | 'paused'
  | 'completed'
  | 'cancelled'
  | 'error';

/**
 * Internal upload state (includes all possible states during upload flow)
 */
export type InternalUploadState = UploadState;

// ============================================================================
// API Response Types
// ============================================================================

/**
 * Session creation response
 */
export interface CreateSessionResponse {
  sessionId: string;
  filename: string;
  totalSize: number;
  chunkSize: number;
  expiresAt: string;
  status: string;
  fileId?: string;
}

/**
 * Chunk upload response
 */
export interface UploadChunkResponse {
  sessionId: string;
  chunkNumber: number;
  uploadedSize: number;
  totalSize: number;
  progress: number;
  uploadedChunks: number;
}

/**
 * Session info response
 */
export interface SessionInfoResponse {
  sessionId: string;
  filename: string;
  totalSize: number;
  uploadedSize: number;
  chunkSize: number;
  progress: number;
  uploadedChunks: number[];
  missingChunks: number[];
  status: string;
  createdAt: string;
  updatedAt: string;
  expiresAt: string;
}

/**
 * Finalize upload response
 */
export interface FinalizeResponse {
  fileId: string;
  message: string;
}

// ============================================================================
// Internal Types
// ============================================================================

/**
 * Chunk task for upload queue
 */
export interface ChunkTask {
  index: number;
  start: number;
  end: number;
  retries: number;
  uploaded: boolean;
}

/**
 * Hash calculation progress
 */
export interface HashProgress {
  loaded: number;
  total: number;
  percentage: number;
}

/**
 * Hash worker message types
 */
export type HashWorkerMessage =
  | { type: 'start'; size: number }
  | { type: 'chunk'; chunk: Uint8Array }
  | { type: 'finalize' }
  | { type: 'ready'; impl: string }
  | { type: 'progress'; loaded: number; total: number }
  | { type: 'done'; hash: string }
  | { type: 'error'; error: string };
