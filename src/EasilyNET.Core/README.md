#### EasilyNET.Core

一个高性能、现代化的 .NET 基础工具库，提供丰富的扩展方法和实用工具类，专为 .NET 项目设计。

---

### **核心功能模块**

| 功能                     | 描述                                                                         | 使用文档                           |
| ------------------------ | ---------------------------------------------------------------------------- | ---------------------------------- |
| SimpleEventAggregator    | 简单的事件发布和订阅,用于解耦多个类之间的消息传递                            | [使用文档](./Aggregator/README.md) |
| CoordinateConvert        | 百度坐标(BD09)、国测局坐标(火星坐标,GCJ02)、和 WGS84 坐标系之间的转换        | [使用文档](./Coordinate/README.md) |
| IDCardValidation         | 中国身份证验证,支持 15 位和 18 位身份证号码验证,可计算生日、性别、年龄等信息 | [使用文档](./IDCard/README.md)     |
| Compression & Zip        | 压缩和解压缩帮助类,支持多文件压缩和解压缩                                    | [使用文档](./IO/README.md)         |
| ManagedWebSocketClient   | 托管的 WebSocket 客户端,支持自动重连、心跳和高性能发送队列                   | [使用文档](./WebSocket/README.md)  |
| AsyncLock & AsyncBarrier | 高性能异步锁和异步屏障,支持 FIFO 顺序和取消令牌                              | [使用文档](./Threading/README.md)  |
| Language Extensions      | C# 语法糖扩展,使用 `..` 代替 `Enumerable.Range`                              | [使用文档](./Language/README.md)   |

---

### **基础设施与工具 (Essentials)**

| 名称               | 描述                                                                        |
| ------------------ | --------------------------------------------------------------------------- |
| PooledMemoryStream | 使用 ArrayPool 优化的高性能池化内存流,减少 GC 压力,实现 IBufferWriter<byte> |
| Ulid               | 通用唯一可字典序排序标识符（ULID）,支持时间戳排序和高并发场景               |
| ObjectIdCompat     | 兼容 MongoDB ObjectId 的算法实现,可与 MongoDB 的 ObjectId 互相转换          |
| BusinessException  | 业务异常类,用于处理业务逻辑中的异常信息,包含 HTTP 状态码                    |
| SharedDateTime     | 共享的 DateTime,用于在跨异步上下文中保持 Now 的值一致,支持手动设置和刷新    |

---

### **数值计算 (Numerics)**

| 名称      | 描述                                                               |
| --------- | ------------------------------------------------------------------ |
| BigNumber | 大数运算类,支持大十进制数和有理数运算,包括加、减、乘、除、幂运算等 |

---

### **枚举类型 (Enums)**

| 名称           | 用途               |
| -------------- | ------------------ |
| EGender        | 性别               |
| ENation        | 中国民族           |
| ETimeOverlap   | 时间重合情况       |
| EZodiac        | 生肖               |
| EConstellation | 星座               |
| ECamelCase     | 驼峰命名法转换模式 |

---

### **业务常用数据类型 (InfoItems)**

| 名称          | 用途                                                    |
| ------------- | ------------------------------------------------------- |
| IdNameItem    | 包含 ID 和 Name 字段的基础类                            |
| OperationInfo | 操作信息,包含操作人以及时间和是否完成                   |
| Operator      | 操作人,包含 rid 和名称字段                              |
| ReferenceItem | 通常用来保存关联的业务信息,如 ID 和名称或者其他相关数据 |

---

### **分页 (PageResult)**

| 名称          | 描述                                        |
| ------------- | ------------------------------------------- |
| PageResult<T> | 泛型分页结果类,包含总数和分页数据           |
| PageInfo      | 分页信息类,包含页码、每页数量等分页基础信息 |

---

### **扩展方法 (Extensions)**

提供大量的静态扩展方法,提升开发效率:

#### DateTime 扩展 (DateTimeExtensions)

- 日期时间范围获取：`DayStart`, `DayEnd`, `WeekStart`, `WeekEnd`, `MonthStart`, `MonthEnd`, `YearStart`, `YearEnd`
- 时间戳转换：`MillisecondsSinceEpoch`, `SecondsSinceEpoch`
- 时间重合验证：`TimeOverlap`
- 周数计算：`GetWeekOfYear`, `WeekNoFromPoint`
- DateOnly/TimeOnly 转换

#### String 扩展 (StringExtensions)

- 字符串格式化和验证
- 驼峰命名转换：`ToCamelCase`, `ToPascalCase`
- 字符串掩码处理
- DateTime 转换
- 哈希和加密相关方法
- 正则表达式工具

#### 数组扩展 (ArrayExtensions)

- 数组操作和转换
- 高性能数组处理

#### 数值扩展 (NumberExtensions)

- 数值类型转换
- 浮点数比较（精度控制）
- 数值范围判断

#### Stream 扩展 (StreamExtensions)

- 流操作优化
- 异步读写支持

#### 集合扩展 (IEnumerableExtensions)

- 集合操作增强
- LINQ 扩展

#### 类型扩展 (TypeExtensions)

- 反射相关工具
- 类型判断和转换

#### 对象扩展 (ObjectExtensions)

- 对象深拷贝（基于表达式树），详见 [DeepCopy 使用文档](./DeepCopy/README.md)
- 对象转换

#### 枚举扩展 (EnumExtensions)

- 枚举描述获取
- 枚举转换

#### 随机数扩展 (RandomExtensions)

- 随机数生成增强
