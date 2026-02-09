# ADR-0003: PreVote 与 ReadIndex 默认策略

## 状态

Accepted

## 背景

生产网络抖动和节点抖动下，频繁 term 膨胀与非线性一致读是主要风险。

## 决策

- `EnablePreVote` 默认开启。
- 读路径默认走 `ReadIndex`，并要求 Leader 在当前任期内完成多数派确认后才返回成功读。

## 影响

- 优点：降低无效选举与 stale read 风险。
- 代价：读延迟受一次确认往返影响。
