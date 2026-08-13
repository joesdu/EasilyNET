# Social Preview

GitHub 仓库社交预览图（分享到 X / Slack / 飞书等平台时展示的卡片图）的源文件与生成脚本。

| 文件 | 说明 |
| --- | --- |
| `social-preview.html` | 设计源文件，纯 HTML + CSS，无外部依赖（字体使用系统字体，图标为内联 SVG） |
| `Render.ps1` | 调用本机 Edge / Chrome 无头模式，将 HTML 渲染为 PNG |
| `social-preview.png` | 生成产物，1920x640@1.5x（画布 1280x640） |

## 生成

```powershell
./social-preview/Render.ps1
```

参数：

- `-Scale`：设备像素比，默认 `1.5`（输出 1920x960）。`1` => 1280x640，`2` => 2560x1280。
- `-OutFile`：输出路径，默认同目录 `social-preview.png`。
- `-BrowserPath`：手动指定浏览器可执行文件，未指定时按 PATH 与各平台默认安装位置自动探测。

## 上传

GitHub 未提供设置社交预览图的公开 API，需手动上传：

**仓库 → Settings → General → Social preview → Edit → Upload an image**

## 约束

- 比例固定 **2:1**，推荐尺寸 1280x640（最小 640x320）
- 体积上限 **1MB**，`Render.ps1` 超限时会给出警告
- 修改文案 / 配色 / 包列表后需重新运行脚本并一并提交 PNG

## 设计约定

- 主色取 .NET 官方紫 `#512BD4`，渐变延伸至青 `#22D3EE`
- 包分组配色：Core 青 / DI 紫 / Mongo 绿 `#00ED64`（MongoDB 品牌色）/ Bus 橙 `#FF7A2F`（RabbitMQ 品牌色）
- 中文为主标语、英文为副标语，与仓库中英双语文档保持一致
- 新增或移除 NuGet 包时，需同步更新右侧包矩阵与 “N 个 NuGet 包” 计数
