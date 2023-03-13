﻿#### EasilyNET.Core

- 业务类型库,用于一些业务方面的类,如分页,以及业务中常用的一些数据类型和枚举类型

- 枚举类型:

| 名称           | 用途     |
|--------------|--------|
| EGender      | 性别     |
| ENation      | 中国民族   |
| ETimeOverlap | 时间重合情况 |

- 业务常用数据类型.

| 名称            | 用途                             |
|---------------|--------------------------------|
| IdNameItem    | 包含 ID 和 Name 字段的基础类            |
| OperationInfo | 操作信息,包含操作人以及时间和是否完成            |
| Operator      | 操作人,包含 rid 和名称字段               |
| ReferenceItem | 通常用来保存关联的业务信息,如 ID 和名称或者其他相关数据 |

- 其他的还有分页信息等.