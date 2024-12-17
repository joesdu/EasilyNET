#### EasilyNET.Core

| 功能                  | 描述                                                                  | 使用文档                                  |
| --------------------- | --------------------------------------------------------------------- | ----------------------------------------- |
| SimpleEventAggregator | 简单的事件发布和订阅,用于解耦多个类之间的消息传递                     | [使用文档](.\Aggregator\README.md)        |
| CoordinateConvert     | 百度坐标(BD09)、国测局坐标(火星坐标,GCJ02)、和 WGS84 坐标系之间的转换 | [使用文档](.\CoordinateConvert\README.md) |
| DeepCopy              | 基于表达式树和 Reflection 的 DeepCopy                                 | [使用文档](.\DeepCopy\README.md)          |
| IDCardValidation      | 中国身份证验证,支持 15 位和 18 位身份证号码验证                       | [使用文档](.\IDCard\README.md)            |
| Compression           | 压缩帮助类                                                            | [使用文档](.\IO\README.md)                |

> 其他一些内容

-   业务类型库,用于一些业务方面的类,如分页,以及业务中常用的一些数据类型和枚举类型
-   以及大量的静态扩展函数

-   枚举类型:

| 名称         | 用途         |
| ------------ | ------------ |
| EGender      | 性别         |
| ENation      | 中国民族     |
| ETimeOverlap | 时间重合情况 |
| EZodiac      | 生肖         |

-   业务常用数据类型.

| 名称          | 用途                                                    |
| ------------- | ------------------------------------------------------- |
| IdNameItem    | 包含 ID 和 Name 字段的基础类                            |
| OperationInfo | 操作信息,包含操作人以及时间和是否完成                   |
| Operator      | 操作人,包含 rid 和名称字段                              |
| ReferenceItem | 通常用来保存关联的业务信息,如 ID 和名称或者其他相关数据 |

-   其他的还有分页信息等.

### **ChangeLog:**

-   新增基于表达式树和 Reflection 的 DeepCopy, BigNumber
-   添加异步锁
-   新增雪花 ID 算法.以及一些扩展方法.
-   新增 Ini 文件帮助类.
-   一些基础库,如数据类型,一些公共静态方法,工具函数.包含数组,日期,字符串,中国农历,拼音,身份证验证等功能
-   将 EasilyNET.Extensions 库合并到 Core 中.
-   新增百度坐标(BD09)、国测局坐标(火星坐标,GCJ02)、和 WGS84 坐标系之间的转换
