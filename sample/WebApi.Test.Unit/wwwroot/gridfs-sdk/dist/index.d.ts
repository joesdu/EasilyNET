/**
 * @easilynet/gridfs-sdk - Types
 * Modern GridFS client SDK for chunked file uploads
 */
/**
 * Upload configuration options
 */
interface UploadOptions {
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
interface DownloadOptions {
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
/**
 * Upload progress information
 */
interface UploadProgress {
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
interface DownloadProgress {
    /** Bytes downloaded so far */
    loaded: number;
    /** Total file size in bytes */
    total: number;
    /** Progress percentage (0-100) */
    percentage: number;
    /** Current download speed in bytes/second */
    speed: number;
}
/**
 * Upload phases
 */
type UploadPhase = 'idle' | 'hashing' | 'creating-session' | 'uploading' | 'finalizing' | 'completed' | 'error';
/**
 * Upload state
 */
type UploadState = 'idle' | 'preparing' | 'uploading' | 'paused' | 'completed' | 'cancelled' | 'error';
/**
 * Session creation response
 */
interface CreateSessionResponse {
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
interface UploadChunkResponse {
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
interface SessionInfoResponse {
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
interface FinalizeResponse {
    fileId: string;
    message: string;
}
/**
 * Chunk task for upload queue
 */
interface ChunkTask {
    index: number;
    start: number;
    end: number;
    retries: number;
    uploaded: boolean;
}
/**
 * Hash calculation progress
 */
interface HashProgress {
    loaded: number;
    total: number;
    percentage: number;
}

/**
 * @easilynet/gridfs-sdk - Uploader
 * Chunked file uploader with resumable support
 */

/**
 * GridFS chunked file uploader
 */
declare class GridFSUploader {
    private readonly file;
    private readonly options;
    private readonly apiClient;
    private chunks;
    private sessionId;
    private abortController;
    private startTime;
    private uploadedBytes;
    private elapsedBeforePause;
    private cachedFileHash?;
    private hashPromise;
    private _state;
    private _phase;
    constructor(file: File, options?: UploadOptions);
    /**
     * Get current upload state
     */
    get state(): UploadState;
    /**
     * Get current upload phase
     */
    get phase(): UploadPhase;
    /**
     * Get session ID (available after upload starts)
     */
    get uploadSessionId(): string;
    /**
     * Start the upload
     */
    start(): Promise<string>;
    /**
     * Pause the upload
     */
    pause(): void;
    /**
     * Resume a paused upload
     */
    resume(): Promise<string>;
    /**
     * Cancel the upload
     */
    cancel(): Promise<void>;
    /**
     * Initialize chunk tasks
     */
    private initializeChunks;
    /**
     * Upload all pending chunks with concurrency control
     */
    private uploadChunks;
    /**
     * Upload a single chunk
     */
    private uploadChunk;
    /**
     * Update and emit progress
     */
    private updateProgress;
    /**
     * Emit progress event
     */
    private emitProgress;
    /**
     * Set upload state
     */
    private setState;
    /**
     * Set upload phase
     */
    private setPhase;
}

/**
 * @easilynet/gridfs-sdk - Downloader
 * File downloader with streaming support
 */

/**
 * GridFS file downloader
 */
declare class GridFSDownloader {
    private readonly options;
    private readonly apiClient;
    private abortController;
    private startTime;
    constructor(options: DownloadOptions);
    /**
     * Get the download URL for a file
     */
    static getUrl(fileId: string, options?: {
        baseUrl?: string;
        headers?: Record<string, string>;
    }): string;
    /**
     * Get the download URL for this file
     */
    getDownloadUrl(): string;
    /**
     * Start downloading the file
     * @param getWritableStream Optional function to get a WritableStream for streaming save
     */
    start(getWritableStream?: (filename: string, total: number) => Promise<WritableStream<Uint8Array> | null>): Promise<Blob | void>;
    /**
     * Cancel the download
     */
    cancel(): void;
    /**
     * Download and save using browser's native download
     */
    downloadAndSave(): Promise<void>;
    /**
     * Save a blob to file
     */
    saveBlob(blob: Blob, filename?: string): void;
    /**
     * Emit progress event
     */
    private emitProgress;
}

/**
 * @easilynet/gridfs-sdk - API Client
 * HTTP client for GridFS server communication
 */

/**
 * GridFS API client configuration
 */
interface ApiClientConfig {
    baseUrl: string;
    headers?: Record<string, string>;
}
/**
 * GridFS API client for server communication
 */
declare class GridFSApiClient {
    private readonly baseUrl;
    private readonly headers;
    constructor(config: ApiClientConfig);
    /**
     * Get the API base URL
     */
    get apiBase(): string;
    /**
     * Create a new upload session
     */
    createSession(filename: string, totalSize: number, fileHash?: string, contentType?: string, signal?: AbortSignal): Promise<CreateSessionResponse>;
    /**
     * Upload a chunk of data
     */
    uploadChunk(sessionId: string, chunkNumber: number, chunkHash: string, data: Blob, signal?: AbortSignal): Promise<UploadChunkResponse>;
    /**
     * Get session information
     */
    getSession(sessionId: string, signal?: AbortSignal): Promise<SessionInfoResponse>;
    /**
     * Get missing chunk numbers
     */
    getMissingChunks(sessionId: string, signal?: AbortSignal): Promise<{
        sessionId: string;
        missingChunks: number[];
    }>;
    /**
     * Finalize the upload
     */
    finalize(sessionId: string, fileHash?: string, skipHashValidation?: boolean, signal?: AbortSignal): Promise<FinalizeResponse>;
    /**
     * Cancel an upload session
     */
    cancel(sessionId: string, signal?: AbortSignal): Promise<void>;
    /**
     * Get stream URL for a file (for video/audio playback)
     */
    getStreamUrl(fileId: string): string;
}

/**
 * @easilynet/gridfs-sdk - Utilities
 * Helper functions for file operations
 */

/**
 * Format bytes to human-readable string
 */
declare function formatFileSize(bytes: number): string;
/**
 * Format seconds to human-readable time string
 */
declare function formatTime(seconds: number): string;
/**
 * Calculate SHA256 hash of a Blob using Web Crypto API
 */
declare function calculateBlobHash(blob: Blob): Promise<string>;
/**
 * Convert Uint8Array to hex string
 */
declare function bufferToHex(bytes: Uint8Array): string;
/**
 * Calculate SHA256 hash of a File using Web Worker (non-blocking)
 */
declare function calculateFileHash(file: File, onProgress?: (progress: HashProgress) => void, workerUrl?: string): Promise<string>;
/**
 * Delay execution for specified milliseconds
 */
declare function delay(ms: number): Promise<void>;
/**
 * Retry a function with exponential backoff
 */
declare function retry<T>(fn: () => Promise<T>, maxRetries: number, baseDelay?: number): Promise<T>;

export { type ApiClientConfig, type ChunkTask, type CreateSessionResponse, type DownloadOptions, type DownloadProgress, type FinalizeResponse, GridFSApiClient, GridFSDownloader, GridFSUploader, type HashProgress, type SessionInfoResponse, type UploadChunkResponse, type UploadOptions, type UploadPhase, type UploadProgress, type UploadState, bufferToHex, calculateBlobHash, calculateFileHash, delay, formatFileSize, formatTime, retry };
