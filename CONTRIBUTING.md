# 贡献指南 (Contributing Guide)

感谢您对 EasilyNET 项目的关注!我们欢迎任何形式的贡献,包括但不限于提交问题、改进文档、修复 bug 或添加新功能。

<details>
<summary style="font-size: 14px">English</summary>

Thank you for your interest in the EasilyNET project! We welcome all forms of contributions, including but not limited to submitting issues, improving documentation, fixing bugs, or adding new features.

</details>

## 📋 目录 (Table of Contents)

- [行为准则](#行为准则-code-of-conduct)
- [开始之前](#开始之前-before-you-start)
- [开发环境设置](#开发环境设置-development-environment-setup)
- [开发流程](#开发流程-development-workflow)
- [代码规范](#代码规范-coding-standards)
- [提交消息规范](#提交消息规范-commit-message-guidelines)
- [拉取请求流程](#拉取请求流程-pull-request-process)
- [测试指南](#测试指南-testing-guidelines)
- [文档贡献](#文档贡献-documentation-contributions)
- [报告问题](#报告问题-reporting-issues)
- [获取帮助](#获取帮助-getting-help)

## 📜 行为准则 (Code of Conduct)

本项目采用 [Contributor Covenant 行为准则](CODE_OF_CONDUCT.md)。参与本项目即表示您同意遵守其条款。

<details>
<summary style="font-size: 14px">English</summary>

This project adopts the [Contributor Covenant Code of Conduct](CODE_OF_CONDUCT.md). By participating in this project, you agree to abide by its terms.

</details>

## 🚀 开始之前 (Before You Start)

在开始贡献之前,请确保:

1. **阅读相关文档**: 熟悉项目的 [README.md](README.md)、[CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) 和 [SECURITY.md](SECURITY.md)
2. **搜索现有问题**: 检查是否已有相关的 [Issue](https://github.com/joesdu/EasilyNET/issues) 或 [Pull Request](https://github.com/joesdu/EasilyNET/pulls)
3. **讨论重大更改**: 对于重大功能或架构变更,请先创建 Issue 进行讨论

<details>
<summary style="font-size: 14px">English</summary>

Before you start contributing, please ensure:

1. **Read the documentation**: Familiarize yourself with [README.md](README.md), [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), and [SECURITY.md](SECURITY.md)
2. **Search existing issues**: Check if there's already a related [Issue](https://github.com/joesdu/EasilyNET/issues) or [Pull Request](https://github.com/joesdu/EasilyNET/pulls)
3. **Discuss major changes**: For significant features or architectural changes, please create an Issue for discussion first

</details>

## 🛠️ 开发环境设置 (Development Environment Setup)

### 前置要求 (Prerequisites)

- **.NET SDK**: 安装最新版本的 .NET SDK (当前支持 .NET 8, .NET 9, .NET 10)
  - 下载地址: https://dotnet.microsoft.com/download
- **Git**: 用于版本控制
- **IDE**: 推荐使用 Visual Studio、Rider 或 Visual Studio Code
- **Docker**: 如需运行测试服务 (MongoDB, RabbitMQ 等)

<details>
<summary style="font-size: 14px">English</summary>

### Prerequisites

- **.NET SDK**: Install the latest .NET SDK (currently supports .NET 8, .NET 9, .NET 10)
  - Download: https://dotnet.microsoft.com/download
- **Git**: For version control
- **IDE**: Visual Studio, Rider, or Visual Studio Code recommended
- **Docker**: Required for running test services (MongoDB, RabbitMQ, etc.)

</details>

### 克隆仓库 (Clone Repository)

```bash
# Fork 本仓库到你的 GitHub 账户,然后克隆你的 Fork
git clone https://github.com/YOUR_USERNAME/EasilyNET.git
cd EasilyNET

# 配置 Git 不区分大小写 (推荐)
git config core.ignorecase false

# 添加上游仓库
git remote add upstream https://github.com/joesdu/EasilyNET.git
```

<details>
<summary style="font-size: 14px">English</summary>

```bash
# Fork this repository to your GitHub account, then clone your fork
git clone https://github.com/YOUR_USERNAME/EasilyNET.git
cd EasilyNET

# Configure Git case sensitivity (recommended)
git config core.ignorecase false

# Add upstream repository
git remote add upstream https://github.com/joesdu/EasilyNET.git
```

</details>

### 安装依赖 (Install Dependencies)

```bash
# 恢复 NuGet 包
dotnet restore
```

### 启动开发服务 (Start Development Services)

```bash
# 启动 MongoDB 副本集集群 (可选,如需测试 MongoDB 功能)
docker compose -f docker-compose.mongo.rs.yml up -d

# 启动基础服务 (Garnet, RabbitMQ, AspireDashboard)
docker compose -f docker-compose.basic.service.yml up -d
```

<details>
<summary style="font-size: 14px">English</summary>

```bash
# Start MongoDB replica set cluster (optional, for MongoDB testing)
docker compose -f docker-compose.mongo.rs.yml up -d

# Start basic services (Garnet, RabbitMQ, AspireDashboard)
docker compose -f docker-compose.basic.service.yml up -d
```

</details>

## 🔄 开发流程 (Development Workflow)

### 1. 创建分支 (Create a Branch)

```bash
# 从最新的 main 分支创建新分支
git checkout main
git pull upstream main
git checkout -b feature/your-feature-name
# 或者 (or)
git checkout -b fix/your-bug-fix
```

### 2. 进行更改 (Make Changes)

- 遵循项目的代码规范
- 编写清晰的代码注释 (如有必要)
- 为新功能添加测试
- 确保所有测试通过

### 3. 构建和测试 (Build and Test)

```bash
# 构建项目
dotnet build

# 或使用提供的脚本 (or use the provided script)
.\Build.ps1

# 运行测试
dotnet test

# 或使用提供的脚本 (or use the provided script)
.\Test.ps1
```

### 4. 提交更改 (Commit Changes)

```bash
# 添加更改的文件
git add .

# 提交更改 (遵循提交消息规范)
git commit -m "✨ Add new feature"
```

### 5. 推送到远程仓库 (Push to Remote)

```bash
git push origin feature/your-feature-name
```

### 6. 创建拉取请求 (Create Pull Request)

1. 访问你的 Fork 仓库
2. 点击 "New Pull Request"
3. 填写 PR 标题和描述
4. 等待维护者审查

<details>
<summary style="font-size: 14px">English</summary>

### 1. Create a Branch

```bash
# Create a new branch from the latest main branch
git checkout main
git pull upstream main
git checkout -b feature/your-feature-name
# or
git checkout -b fix/your-bug-fix
```

### 2. Make Changes

- Follow the project's coding standards
- Write clear code comments (if necessary)
- Add tests for new features
- Ensure all tests pass

### 3. Build and Test

```bash
# Build the project
dotnet build

# or use the provided script
.\Build.ps1

# Run tests
dotnet test

# or use the provided script
.\Test.ps1
```

### 4. Commit Changes

```bash
# Add changed files
git add .

# Commit changes (follow commit message guidelines)
git commit -m "✨ Add new feature"
```

### 5. Push to Remote

```bash
git push origin feature/your-feature-name
```

### 6. Create Pull Request

1. Go to your forked repository
2. Click "New Pull Request"
3. Fill in the PR title and description
4. Wait for maintainer review

</details>

## 📏 代码规范 (Coding Standards)

### C# 代码规范 (C# Coding Standards)

- 遵循 [C# 编码约定](https://docs.microsoft.com/zh-cn/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 个空格进行缩进
- 使用有意义的变量和方法名
- 保持方法简短且专注于单一职责
- 添加 XML 文档注释用于公共 API

### 命名约定 (Naming Conventions)

- **类、接口、方法**: PascalCase (例如: `UserService`, `IRepository`)
- **私有字段**: camelCase 或 _camelCase (例如: `userName` 或 `_userName`)
- **常量**: PascalCase 或 UPPER_CASE (例如: `MaxValue` 或 `MAX_VALUE`)
- **参数**: camelCase (例如: `userId`)

### 代码组织 (Code Organization)

- 相关功能放在同一个命名空间
- 使用文件夹结构组织代码
- 每个文件只包含一个公共类型

<details>
<summary style="font-size: 14px">English</summary>

### C# Coding Standards

- Follow [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use 4 spaces for indentation
- Use meaningful variable and method names
- Keep methods short and focused on a single responsibility
- Add XML documentation comments for public APIs

### Naming Conventions

- **Classes, Interfaces, Methods**: PascalCase (e.g., `UserService`, `IRepository`)
- **Private fields**: camelCase or _camelCase (e.g., `userName` or `_userName`)
- **Constants**: PascalCase or UPPER_CASE (e.g., `MaxValue` or `MAX_VALUE`)
- **Parameters**: camelCase (e.g., `userId`)

### Code Organization

- Place related functionality in the same namespace
- Use folder structure to organize code
- Only one public type per file

</details>

## 💬 提交消息规范 (Commit Message Guidelines)

本项目使用 [Gitemoji](gitemoji.md) 规范来编写提交消息。

### 格式 (Format)

```
<emoji> <type>: <subject>

[optional body]

[optional footer]
```

### 常用 Emoji 示例 (Common Emoji Examples)

- ✨ `:sparkles:` - 引入新功能 (Introduce new features)
- 🐛 `:bug:` - 修复错误 (Fix bugs)
- 📝 `:memo:` - 添加或更新文档 (Add or update documentation)
- 🎨 `:art:` - 改进代码结构/格式 (Improve code structure/format)
- ⚡️ `:zap:` - 提高性能 (Improve performance)
- ♻️ `:recycle:` - 重构代码 (Refactor code)
- ✅ `:white_check_mark:` - 添加或更新测试 (Add or update tests)
- 🔒️ `:lock:` - 修复安全问题 (Fix security issues)
- ⬆️ `:arrow_up:` - 升级依赖项 (Upgrade dependencies)
- ⬇️ `:arrow_down:` - 降级依赖项 (Downgrade dependencies)

完整列表请参考 [gitemoji.md](gitemoji.md)。

### 示例 (Examples)

```bash
✨ feat: Add support for DateOnly and TimeOnly in MongoDB
🐛 fix: Resolve connection timeout issue in RabbitMQ
📝 docs: Update README with new usage examples
♻️ refactor: Simplify dependency injection logic
✅ test: Add unit tests for encryption module
```

<details>
<summary style="font-size: 14px">English</summary>

This project uses the [Gitemoji](gitemoji.md) convention for commit messages.

### Format

```
<emoji> <type>: <subject>

[optional body]

[optional footer]
```

For a complete list, please refer to [gitemoji.md](gitemoji.md).

</details>

## 🔀 拉取请求流程 (Pull Request Process)

### PR 标题 (PR Title)

使用与提交消息相同的格式:

```
✨ feat: Add new authentication module
```

### PR 描述 (PR Description)

一个好的 PR 描述应包含:

1. **更改内容**: 简要说明你做了什么更改
2. **更改原因**: 解释为什么需要这些更改
3. **测试方法**: 说明如何测试这些更改
4. **相关 Issue**: 如果有相关 Issue,请引用它们 (例如: `Closes #123`)
5. **截图/示例**: 对于 UI 更改,请提供截图

### 模板示例 (Template Example)

```markdown
## 更改内容 (Changes)

- 添加了对 DateOnly 和 TimeOnly 的 MongoDB 支持
- 更新了相关文档

## 更改原因 (Motivation)

.NET 6+ 引入了 DateOnly 和 TimeOnly 类型,需要在 MongoDB 驱动中提供支持。

## 测试方法 (Testing)

- [ ] 添加了单元测试
- [ ] 所有现有测试通过
- [ ] 手动测试了序列化和反序列化

## 相关 Issue (Related Issues)

Closes #123

## 截图 (Screenshots)

(如适用)
```

### 审查过程 (Review Process)

1. **自动检查**: CI/CD 流程会自动运行测试和代码检查
2. **代码审查**: 维护者会审查你的代码并提供反馈
3. **修改完善**: 根据反馈进行修改
4. **合并**: 审查通过后,维护者会合并你的 PR

### 注意事项 (Notes)

- 保持 PR 范围小且专注
- 确保 PR 只包含相关的更改
- 及时响应审查意见
- 保持 PR 分支与主分支同步

<details>
<summary style="font-size: 14px">English</summary>

### PR Title

Use the same format as commit messages:

```
✨ feat: Add new authentication module
```

### PR Description

A good PR description should include:

1. **Changes**: Briefly explain what changes you made
2. **Motivation**: Explain why these changes are needed
3. **Testing**: Describe how to test these changes
4. **Related Issues**: Reference related issues (e.g., `Closes #123`)
5. **Screenshots/Examples**: Provide screenshots for UI changes

### Review Process

1. **Automated checks**: CI/CD pipeline will automatically run tests and code checks
2. **Code review**: Maintainers will review your code and provide feedback
3. **Revisions**: Make changes based on feedback
4. **Merge**: After approval, maintainers will merge your PR

### Notes

- Keep PR scope small and focused
- Ensure PR only contains relevant changes
- Respond to review comments promptly
- Keep PR branch synchronized with main branch

</details>

## 🧪 测试指南 (Testing Guidelines)

### 编写测试 (Writing Tests)

- 为所有新功能编写单元测试
- 确保测试覆盖边界情况
- 使用描述性的测试方法名
- 遵循 AAA 模式 (Arrange, Act, Assert)

### 运行测试 (Running Tests)

```bash
# 运行所有测试
dotnet test

# 运行特定项目的测试
dotnet test test/EasilyNET.Test.Unit/EasilyNET.Test.Unit.csproj

# 使用 PowerShell 脚本
.\Test.ps1
```

### 测试覆盖率 (Test Coverage)

- 努力保持较高的测试覆盖率
- 重点关注核心功能和公共 API
- 不必强求 100% 覆盖率,但要确保关键路径被测试

<details>
<summary style="font-size: 14px">English</summary>

### Writing Tests

- Write unit tests for all new features
- Ensure tests cover edge cases
- Use descriptive test method names
- Follow the AAA pattern (Arrange, Act, Assert)

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific project
dotnet test test/EasilyNET.Test.Unit/EasilyNET.Test.Unit.csproj

# Use PowerShell script
.\Test.ps1
```

### Test Coverage

- Strive for high test coverage
- Focus on core functionality and public APIs
- Don't aim for 100% coverage, but ensure critical paths are tested

</details>

## 📚 文档贡献 (Documentation Contributions)

文档与代码同样重要!我们欢迎以下文档贡献:

- 改进现有文档的清晰度
- 修复文档中的错误或过时信息
- 添加使用示例和教程
- 翻译文档 (中英文)
- 添加代码注释

### 文档位置 (Documentation Locations)

- **项目 README**: 各个项目目录下的 `README.md`
- **主 README**: 根目录的 `README.md`
- **API 文档**: XML 注释和代码内文档
- **使用示例**: `sample/` 目录下的示例项目

<details>
<summary style="font-size: 14px">English</summary>

Documentation is as important as code! We welcome the following documentation contributions:

- Improve clarity of existing documentation
- Fix errors or outdated information in documentation
- Add usage examples and tutorials
- Translate documentation (Chinese and English)
- Add code comments

### Documentation Locations

- **Project README**: `README.md` in each project directory
- **Main README**: `README.md` in root directory
- **API Documentation**: XML comments and in-code documentation
- **Usage Examples**: Sample projects in `sample/` directory

</details>

## 🐛 报告问题 (Reporting Issues)

### 安全问题 (Security Issues)

如果发现安全漏洞,请**不要**公开创建 Issue。请参阅 [SECURITY.md](SECURITY.md) 了解如何负责任地报告安全问题。

### Bug 报告 (Bug Reports)

创建 Bug 报告时,请包含:

1. **清晰的标题**: 简要描述问题
2. **复现步骤**: 详细说明如何重现问题
3. **期望行为**: 说明应该发生什么
4. **实际行为**: 说明实际发生了什么
5. **环境信息**: 操作系统、.NET 版本、相关包版本
6. **代码示例**: 如可能,提供最小可复现示例
7. **错误消息**: 完整的错误消息和堆栈跟踪

### 功能请求 (Feature Requests)

提交功能请求时,请说明:

1. **问题描述**: 你想解决什么问题?
2. **建议方案**: 你建议如何实现?
3. **备选方案**: 是否考虑过其他方案?
4. **使用场景**: 这个功能在什么场景下使用?

<details>
<summary style="font-size: 14px">English</summary>

### Security Issues

If you discover a security vulnerability, please **do not** create a public Issue. Refer to [SECURITY.md](SECURITY.md) for how to report security issues responsibly.

### Bug Reports

When creating a bug report, please include:

1. **Clear title**: Briefly describe the issue
2. **Reproduction steps**: Detailed instructions on how to reproduce the issue
3. **Expected behavior**: Describe what should happen
4. **Actual behavior**: Describe what actually happened
5. **Environment information**: OS, .NET version, relevant package versions
6. **Code example**: If possible, provide a minimal reproducible example
7. **Error messages**: Complete error messages and stack traces

### Feature Requests

When submitting a feature request, please describe:

1. **Problem description**: What problem do you want to solve?
2. **Proposed solution**: How do you suggest implementing it?
3. **Alternative solutions**: Have you considered other approaches?
4. **Use cases**: In what scenarios would this feature be used?

</details>

## 💡 获取帮助 (Getting Help)

如果你有任何问题或需要帮助:

1. **查看文档**: 先查看 [README.md](README.md) 和各项目的文档
2. **搜索 Issue**: 查看是否有类似的问题已被讨论
3. **创建 Issue**: 如果找不到答案,创建一个新的 Issue
4. **联系维护者**: 通过 [dygood@outlook.com](mailto:dygood@outlook.com) 联系

<details>
<summary style="font-size: 14px">English</summary>

If you have any questions or need help:

1. **Check documentation**: First check [README.md](README.md) and project documentation
2. **Search issues**: See if similar issues have been discussed
3. **Create an issue**: If you can't find an answer, create a new Issue
4. **Contact maintainers**: Contact via [dygood@outlook.com](mailto:dygood@outlook.com)

</details>

## 🙏 致谢 (Acknowledgments)

感谢所有为 EasilyNET 项目做出贡献的开发者!

你的贡献让这个项目变得更好! 🎉

<details>
<summary style="font-size: 14px">English</summary>

Thank you to all developers who have contributed to the EasilyNET project!

Your contributions make this project better! 🎉

</details>

---

## 📄 许可证 (License)

通过向本项目贡献代码,你同意你的贡献将使用与本项目相同的 [MIT License](LICENSE) 进行许可。

<details>
<summary style="font-size: 14px">English</summary>

By contributing code to this project, you agree that your contributions will be licensed under the same [MIT License](LICENSE) as the project.

</details>
