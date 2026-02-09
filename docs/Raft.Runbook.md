# Raft 运维 Runbook（最小）

## 1. Leader 抖动

- 检查 `ElectionTimeoutMinMs/MaxMs` 与 `HeartbeatIntervalMs` 是否满足超时梯度
- 检查节点间 RTT 与丢包率，必要时提升选举窗口

## 2. 提交停滞

- 检查多数派连通性与节点健康
- 观察 `raft_election_count`、`raft_leader_changes_total` 是否异常升高

## 3. 快照频繁安装

- 检查 `SnapshotThreshold` 设置是否过低
- 检查慢节点磁盘/网络，确认是否长期落后

## 4. 启动失败

- 优先排查 `RaftOptions` 与 `RaftFileStorageOptions` 配置校验
- 检查存储目录权限与文件锁竞争

## 5. 健康探针

- `easilynet_raft_liveness`: 运行时是否完成初始化
- `easilynet_raft_readiness`: 节点是否可服务（Leader 或已知 Leader）
