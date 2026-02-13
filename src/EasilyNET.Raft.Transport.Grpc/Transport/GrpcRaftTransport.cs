using System.Collections.Concurrent;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Transport.Grpc.Options;
using EasilyNET.Raft.Transport.Grpc.Protos;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;

namespace EasilyNET.Raft.Transport.Grpc.Transport;

/// <summary>
///     <para xml:lang="en">gRPC transport adapter for outbound raft messages</para>
///     <para xml:lang="zh">Raft 出站 gRPC 传输适配器</para>
/// </summary>
public sealed class GrpcRaftTransport(IOptions<RaftGrpcOptions> options) : IRaftTransport
{
    // ReSharper disable once CollectionNeverQueried.Local
    private readonly ConcurrentDictionary<string, GrpcChannel> _channels = [];
    private readonly ConcurrentDictionary<string, RaftRpc.RaftRpcClient> _clients = [];
    private readonly RaftGrpcOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _peerGates = [];

    /// <inheritdoc />
    public async Task<RaftMessage?> SendAsync(string targetNodeId, RaftMessage message, CancellationToken cancellationToken = default)
    {
        var client = GetClient(targetNodeId);
        var gate = GetPeerGate(targetNodeId);
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            switch (message)
            {
                case RequestVoteRequest voteRequest:
                    return await SendWithRetryAsync(ct => client.RequestVoteAsync(GrpcRaftMessageMapper.ToRpc(voteRequest), cancellationToken: ct).ResponseAsync,
                                   GrpcRaftMessageMapper.FromRpc,
                                   cancellationToken)
                               .ConfigureAwait(false);
                case AppendEntriesRequest appendEntriesRequest:
                    return await SendWithRetryAsync(ct => client.AppendEntriesAsync(GrpcRaftMessageMapper.ToRpc(appendEntriesRequest), cancellationToken: ct).ResponseAsync,
                                   GrpcRaftMessageMapper.FromRpc,
                                   cancellationToken)
                               .ConfigureAwait(false);
                case InstallSnapshotRequest snapshotRequest:
                    // ReSharper disable once InvertIf
                    if (snapshotRequest.SnapshotData.Length > Math.Max(1024, _options.SnapshotChunkBytes))
                    {
                        await SendWithRetryAsync(ct => SendSnapshotInChunksAsync(client, snapshotRequest, ct),
                                cancellationToken)
                            .ConfigureAwait(false);
                        return new InstallSnapshotResponse
                        {
                            SourceNodeId = targetNodeId,
                            Term = snapshotRequest.Term,
                            Success = true
                        };
                    }
                    return await SendWithRetryAsync(ct => client.InstallSnapshotAsync(GrpcRaftMessageMapper.ToRpc(snapshotRequest), cancellationToken: ct).ResponseAsync,
                                   GrpcRaftMessageMapper.FromRpc,
                                   cancellationToken)
                               .ConfigureAwait(false);
                default:
                    return null;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private RaftRpc.RaftRpcClient GetClient(string targetNodeId)
    {
        return _clients.GetOrAdd(targetNodeId, nodeId =>
        {
            if (!_options.PeerEndpoints.TryGetValue(nodeId, out var endpoint))
            {
                throw new InvalidOperationException($"Raft peer endpoint for node '{nodeId}' is not configured.");
            }
            var channel = GrpcChannel.ForAddress(endpoint);
            _channels[nodeId] = channel;
            return new(channel);
        });
    }

    private SemaphoreSlim GetPeerGate(string targetNodeId)
    {
        return _peerGates.GetOrAdd(targetNodeId, _ => new(Math.Max(1, _options.MaxInFlightPerPeer), Math.Max(1, _options.MaxInFlightPerPeer)));
    }

    private async Task SendWithRetryAsync(Func<CancellationToken, Task> execute, CancellationToken cancellationToken)
    {
        for (var attempt = 0;; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(1, _options.RequestTimeoutMs)));
            try
            {
                await execute(timeoutCts.Token).ConfigureAwait(false);
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < Math.Max(0, _options.MaxRetryAttempts))
            {
                var delayMs = Math.Max(1, _options.RetryBackoffMs) * (attempt + 1);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task<RaftMessage> SendWithRetryAsync<TResponse>(Func<CancellationToken, Task<TResponse>> execute, Func<TResponse, RaftMessage> map, CancellationToken cancellationToken)
    {
        for (var attempt = 0;; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(Math.Max(1, _options.RequestTimeoutMs)));
            try
            {
                var response = await execute(timeoutCts.Token).ConfigureAwait(false);
                return map(response);
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < Math.Max(0, _options.MaxRetryAttempts))
            {
                var delayMs = Math.Max(1, _options.RetryBackoffMs) * (attempt + 1);
                await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static bool IsTransient(Exception ex) => ex is TimeoutException or OperationCanceledException or RpcException { StatusCode: StatusCode.DeadlineExceeded or StatusCode.Unavailable or StatusCode.Internal };

    private async Task SendSnapshotInChunksAsync(RaftRpc.RaftRpcClient client, InstallSnapshotRequest request, CancellationToken cancellationToken)
    {
        using var call = client.InstallSnapshotStream(cancellationToken: cancellationToken);
        var chunkSize = Math.Max(1024, _options.SnapshotChunkBytes);
        var offset = 0;
        while (offset < request.SnapshotData.Length)
        {
            var remaining = request.SnapshotData.Length - offset;
            var size = Math.Min(chunkSize, remaining);
            var chunk = new byte[size];
            Buffer.BlockCopy(request.SnapshotData, offset, chunk, 0, size);
            await call.RequestStream.WriteAsync(new()
            {
                SourceNodeId = request.SourceNodeId,
                Term = request.Term,
                LeaderId = request.LeaderId,
                LastIncludedIndex = request.LastIncludedIndex,
                LastIncludedTerm = request.LastIncludedTerm,
                ChunkData = ByteString.CopyFrom(chunk),
                Offset = offset,
                IsLastChunk = offset + size >= request.SnapshotData.Length
            }, cancellationToken).ConfigureAwait(false);
            offset += size;
        }
        await call.RequestStream.CompleteAsync().ConfigureAwait(false);
        _ = await call.ResponseAsync.ConfigureAwait(false);
    }
}