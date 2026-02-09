using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.Core.Messages;
using EasilyNET.Raft.Core.Models;
using EasilyNET.Raft.Transport.Grpc.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#pragma warning disable IDE0130
namespace Microsoft.AspNetCore.Builder;

/// <summary>
///     <para xml:lang="en">Raft endpoint mapping extensions</para>
///     <para xml:lang="zh">Raft 端点映射扩展</para>
/// </summary>
public static class RaftEndpointRouteBuilderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Maps raft gRPC and basic management endpoints</para>
    ///     <para xml:lang="zh">映射 Raft gRPC 与基础管理端点</para>
    /// </summary>
    public static IEndpointRouteBuilder MapEasilyRaft(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGrpcService<GrpcRaftService>();
        endpoints.MapGet("/raft/status", (IRaftRuntime runtime) =>
        {
            var state = runtime.GetState();
            return Results.Ok(new
            {
                state.NodeId,
                Role = state.Role.ToString(),
                state.CurrentTerm,
                state.LeaderId,
                state.CommitIndex,
                state.LastApplied,
                LastLogIndex = state.LastLogIndex,
                LastLogTerm = state.LastLogTerm
            });
        });

        endpoints.MapGet("/raft/read-index", async (IRaftRuntime runtime, CancellationToken cancellationToken) =>
        {
            var state = runtime.GetState();
            var response = await runtime.HandleRpcAsync<ReadIndexResponse>(
                new ReadIndexRequest
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm
                },
                cancellationToken).ConfigureAwait(false);

            return response.Success
                ? Results.Ok(new { response.ReadIndex, response.Term, response.LeaderId })
                : Results.Conflict(new { response.ReadIndex, response.Term, response.LeaderId, Message = "not leader" });
        });

        endpoints.MapPost("/raft/members/add/{nodeId}", async (string nodeId, IRaftRuntime runtime, CancellationToken cancellationToken) =>
        {
            var state = runtime.GetState();
            var response = await runtime.HandleRpcAsync<ConfigurationChangeResponse>(
                new ConfigurationChangeRequest
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    ChangeType = ConfigurationChangeType.Add,
                    TargetNodeId = nodeId
                },
                cancellationToken).ConfigureAwait(false);

            return response.Success
                ? Results.Ok(new { response.Success, response.Term })
                : Results.Conflict(new { response.Success, response.Term, response.Reason });
        });

        endpoints.MapPost("/raft/members/remove/{nodeId}", async (string nodeId, IRaftRuntime runtime, CancellationToken cancellationToken) =>
        {
            var state = runtime.GetState();
            var response = await runtime.HandleRpcAsync<ConfigurationChangeResponse>(
                new ConfigurationChangeRequest
                {
                    SourceNodeId = state.NodeId,
                    Term = state.CurrentTerm,
                    ChangeType = ConfigurationChangeType.Remove,
                    TargetNodeId = nodeId
                },
                cancellationToken).ConfigureAwait(false);

            return response.Success
                ? Results.Ok(new { response.Success, response.Term })
                : Results.Conflict(new { response.Success, response.Term, response.Reason });
        });

        return endpoints;
    }
}
