using EasilyNET.Raft.Core.Options;
using Microsoft.Extensions.Options;

namespace EasilyNET.Raft.AspNetCore.Options;

/// <summary>
///     <para xml:lang="en">Validates raft options at startup</para>
///     <para xml:lang="zh">启动时验证 Raft 配置</para>
/// </summary>
public sealed class RaftOptionsValidator : IValidateOptions<RaftOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, RaftOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.NodeId))
        {
            return ValidateOptionsResult.Fail("RaftOptions.NodeId is required.");
        }
        if (options.ClusterMembers.Count < 3)
        {
            return ValidateOptionsResult.Fail("RaftOptions.ClusterMembers must contain at least 3 nodes.");
        }
        if (options.ClusterMembers.Distinct().Count() != options.ClusterMembers.Count)
        {
            return ValidateOptionsResult.Fail("RaftOptions.ClusterMembers must not contain duplicate node ids.");
        }
        if (!options.ClusterMembers.Contains(options.NodeId))
        {
            return ValidateOptionsResult.Fail("RaftOptions.NodeId must exist in ClusterMembers.");
        }
        if (options.HeartbeatIntervalMs <= 0)
        {
            return ValidateOptionsResult.Fail("RaftOptions.HeartbeatIntervalMs must be > 0.");
        }
        if (options.ElectionTimeoutMinMs <= options.HeartbeatIntervalMs)
        {
            return ValidateOptionsResult.Fail("RaftOptions.ElectionTimeoutMinMs must be > HeartbeatIntervalMs.");
        }
        if (options.ElectionTimeoutMaxMs <= options.ElectionTimeoutMinMs)
        {
            return ValidateOptionsResult.Fail("RaftOptions.ElectionTimeoutMaxMs must be > ElectionTimeoutMinMs.");
        }
        if (options.MaxEntriesPerAppend < 1)
        {
            return ValidateOptionsResult.Fail("RaftOptions.MaxEntriesPerAppend must be >= 1.");
        }
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (options.SnapshotThreshold < options.MaxEntriesPerAppend)
        {
            return ValidateOptionsResult.Fail("RaftOptions.SnapshotThreshold must be >= MaxEntriesPerAppend.");
        }
        return ValidateOptionsResult.Success;
    }
}