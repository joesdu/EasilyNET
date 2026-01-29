# @easilynet/gridfs-sdk

Modern GridFS client SDK for chunked file uploads with resumable support.

## Features

- 📦 **Chunked Upload**: Large file support with configurable chunk sizes
- ⏸️ **Resumable**: Pause and resume uploads at any time
- 🚀 **Instant Upload**: Automatic deduplication via SHA256 hash
- 📊 **Progress Tracking**: Real-time upload/download progress
- 🔄 **Auto Retry**: Automatic retry with exponential backoff
- 🎯 **TypeScript**: Full TypeScript support with type definitions
- 🌐 **Modern**: ESM and CommonJS support

## Installation

```bash
npm install @easilynet/gridfs-sdk
```

## Usage

### Upload

```typescript
import { GridFSUploader, formatFileSize, formatTime } from '@easilynet/gridfs-sdk';

const file = document.querySelector('input[type="file"]').files[0];

const uploader = new GridFSUploader(file, {
  baseUrl: 'https://api.example.com',
  maxConcurrent: 3,
  onProgress: (progress) => {
    console.log(`Progress: ${progress.percentage.toFixed(2)}%`);
    console.log(`Speed: ${formatFileSize(progress.speed)}/s`);
    console.log(`Remaining: ${formatTime(progress.remainingTime)}`);
  },
  onComplete: (fileId) => {
    console.log('Upload complete! File ID:', fileId);
  },
  onError: (error) => {
    console.error('Upload failed:', error);
  },
});

// Start upload
await uploader.start();

// Pause/Resume
uploader.pause();
await uploader.resume();

// Cancel
await uploader.cancel();
```

### Download

```typescript
import { GridFSDownloader } from '@easilynet/gridfs-sdk';

const downloader = new GridFSDownloader({
  baseUrl: 'https://api.example.com',
  fileId: '507f1f77bcf86cd799439011',
  onProgress: (progress) => {
    console.log(`Downloaded: ${progress.percentage.toFixed(2)}%`);
  },
});

// Download and save
await downloader.downloadAndSave();

// Or get as Blob
const blob = await downloader.start();
```

### Video/Audio Streaming

```typescript
import { GridFSDownloader } from '@easilynet/gridfs-sdk';

// Get streaming URL for video player
const videoUrl = GridFSDownloader.getUrl('507f1f77bcf86cd799439011', {
  baseUrl: 'https://api.example.com',
});

// Use in video element
document.querySelector('video').src = videoUrl;
```

## API Reference

### GridFSUploader

#### Constructor Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `baseUrl` | `string` | `''` | Server base URL |
| `chunkSize` | `number` | Server-provided | Chunk size in bytes |
| `maxConcurrent` | `number` | `4` | Max concurrent uploads |
| `retryCount` | `number` | `3` | Retry attempts |
| `verifyOnServer` | `boolean` | `true` | Server-side hash verification |
| `headers` | `Record<string, string>` | `{}` | Additional HTTP headers |
| `onProgress` | `function` | - | Progress callback |
| `onComplete` | `function` | - | Completion callback |
| `onError` | `function` | - | Error callback |

#### Methods

- `start(): Promise<string>` - Start upload, returns file ID
- `pause(): void` - Pause upload
- `resume(): Promise<string>` - Resume upload
- `cancel(): Promise<void>` - Cancel upload

### GridFSDownloader

#### Constructor Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `baseUrl` | `string` | `''` | Server base URL |
| `fileId` | `string` | Required | File ID to download |
| `filename` | `string` | Auto-detected | Save filename |
| `headers` | `Record<string, string>` | `{}` | Additional HTTP headers |
| `onProgress` | `function` | - | Progress callback |
| `onError` | `function` | - | Error callback |

#### Methods

- `start(): Promise<Blob | void>` - Start download
- `cancel(): void` - Cancel download
- `downloadAndSave(): Promise<void>` - Download and save to disk
- `getDownloadUrl(): string` - Get streaming URL

## License

MIT
