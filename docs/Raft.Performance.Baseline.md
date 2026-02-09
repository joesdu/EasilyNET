# Raft 性能基线与回归对比（初版）

## 1. 指标口径

- 吞吐：`ops/s`
- 延迟：`P50/P99` 写延迟（ms）
- 恢复：节点重启恢复到可服务时长（s）
- 复制：`raft_replication_lag` 峰值与稳态值

## 2. 测试维度

- 节点规模：`3 / 5`
- 命令大小：`256B / 1KB / 4KB`
- 批量参数：`MaxEntriesPerAppend` 多组配置
- 刷盘策略：`Always / Batch / Adaptive`

## 3. 执行与记录

1. 运行单元与模拟基线：`dotnet test -c Debug --no-build --filter "FullyQualifiedName~Raft"`
2. 运行集成基线：`dotnet test test/EasilyNET.Raft.IntegrationTests -c Debug`
3. 输出以下字段到基线记录：
   - commit 推进速率
   - append latency P50/P99
   - snapshot install 耗时
   - replication lag 峰值

## 4. 回归判定（建议）

- 吞吐下降 > 10% 判定为回归
- P99 延迟上升 > 20% 判定为回归
- 恢复时间上升 > 20% 判定为回归

## 5. 基线快照（待填充）

| 日期 | 节点数 | 命令大小 | fsync | ops/s | P50(ms) | P99(ms) | 恢复(s) | 备注 |
| ---- | ------ | -------- | ----- | ----- | ------- | ------- | ------- | ---- |
| TBD  | 3      | 1KB      | Batch | TBD   | TBD     | TBD     | TBD     | 初版 |
