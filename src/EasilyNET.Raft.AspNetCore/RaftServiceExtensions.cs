using EasilyNET.Raft.AspNetCore.Health;
using EasilyNET.Raft.AspNetCore.Observability;
using EasilyNET.Raft.AspNetCore.Options;
using EasilyNET.Raft.AspNetCore.Runtime;
using EasilyNET.Raft.AspNetCore.Services;
using EasilyNET.Raft.AspNetCore.Transport;
using EasilyNET.Raft.Core.Abstractions;
using EasilyNET.Raft.Core.Options;
using EasilyNET.Raft.Core.StateMachine;
using EasilyNET.Raft.Storage.File.Options;
using EasilyNET.Raft.Storage.File.Stores;
using EasilyNET.Raft.Transport.Grpc.Abstractions;
using EasilyNET.Raft.Transport.Grpc.Options;
using EasilyNET.Raft.Transport.Grpc.Transport;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Raft service registration extensions</para>
///     <para xml:lang="zh">Raft 服务注册扩展</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public static class RaftServiceExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Adds raft services with file storage and gRPC transport</para>
    ///     <para xml:lang="zh">注册 Raft 服务（文件存储 + gRPC 传输）</para>
    /// </summary>
    public static IServiceCollection AddEasilyRaft(
        this IServiceCollection services,
        Action<RaftOptions> configureRaft,
        Action<RaftFileStorageOptions>? configureStorage = null,
        Action<RaftGrpcOptions>? configureGrpc = null)
    {
        services.AddOptions<RaftOptions>()
                .Configure(configureRaft)
                .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<RaftOptions>, RaftOptionsValidator>());
        services.AddOptions<RaftFileStorageOptions>();
        if (configureStorage is not null)
        {
            services.Configure(configureStorage);
        }
        services.AddOptions<RaftGrpcOptions>();
        if (configureGrpc is not null)
        {
            services.Configure(configureGrpc);
        }
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<RaftFileStorageOptions>>().Value);
        services.TryAddSingleton<ILogStore, FileLogStore>();
        services.TryAddSingleton<IStateStore, FileStateStore>();
        services.TryAddSingleton<ISnapshotStore, FileSnapshotStore>();
        services.TryAddSingleton<IStateMachine, NoopStateMachine>();
        services.TryAddSingleton<RaftMetrics>();
        services.TryAddSingleton<IRaftTransport, GrpcRaftTransport>();
        services.TryAddSingleton<IRaftRuntime, RaftRuntime>();
        services.TryAddSingleton<IRaftRpcMessageHandler, RaftRpcMessageHandler>();
        services.TryAddSingleton<RaftHostedService>();
        services.TryAddSingleton<IRaftTimerControl>(sp => sp.GetRequiredService<RaftHostedService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<RaftHostedService>());
        services.AddGrpc();
        services.AddHealthChecks()
                .AddCheck<RaftHealthCheck>("easilynet_raft")
                .AddCheck<RaftLivenessHealthCheck>("easilynet_raft_liveness")
                .AddCheck<RaftReadinessHealthCheck>("easilynet_raft_readiness");
        return services;
    }
}