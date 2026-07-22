# Day 1 测试工程搭建

## 1. 当日目标

搭建 InprovePlan 项目的生产级测试工程基础结构，形成清晰的测试分层：

| 测试项目 | 目标 |
|---|---|
| InprovePlan.UnitTests | 验证最小业务规则、参数校验、纯逻辑组件，不依赖数据库、Redis、Web 容器 |
| InprovePlan.IntegrationTests | 验证 UserCase 层 Command/Query Handler 与 MySQL、Redis、仓储、缓存、幂等逻辑的协作 |
| InprovePlan.ApiTests | 通过 WebApplicationFactory 和 HttpClient 验证业务 API 的真实 HTTP 行为 |

## 2. 测试工程结构

```text
InprovePlan.UnitTests/
  AppOrders/
  AppUsers/
  Behaviors/
  Builders/
  Extensions/
  Paging/
  Products/
  Sorting/
  TestData/
  TestDoubles/

InprovePlan.IntegrationTests/
  AppOrders/
  AppUsers/
  Builders/
  DataSeeders/
  Helpers/
  Idempotency/
  Infrastructure/
  Products/
  TestData/
  TestDoubles/

InprovePlan.ApiTests/
  AppOrders/
  AppUsers/
  Clients/
  Contracts/
  Identity/
  Infrastructure/
  Products/
  TestData/
  TestDoubles/
```

## 3. 使用的测试框架

当前统一使用 xUnit v3 作为测试框架。

选择原因：

| 框架/工具 | 作用 | 说明 |
|---|---|---|
| xUnit v3 | 测试执行框架 | 使用 `[Fact]` / `[Theory]` 标记测试用例，支持异步测试和 Fixture 生命周期 |
| FluentAssertions | 断言库 | 使用 `Should().Be(...)`、`Should().NotBeNull()` 提升断言可读性 |
| Microsoft.NET.Test.Sdk | .NET 测试 SDK | 让 `dotnet test` 可以发现并运行测试 |
| xunit.runner.visualstudio | 测试运行适配器 | 支持 Visual Studio、Rider、dotnet test 发现 xUnit 测试 |

## 4. NuGet 包清单

### InprovePlan.UnitTests

| 包 | 用途 |
|---|---|
| `xunit.v3` | 单元测试框架 |
| `xunit.runner.visualstudio` | 测试发现和运行 |
| `FluentAssertions` | 可读性更强的断言 |
| `Moq` | Mock 外部依赖 |
| `Microsoft.NET.Test.Sdk` | 测试 SDK |

### InprovePlan.IntegrationTests

| 包 | 用途 |
|---|---|
| `xunit.v3` | 集成测试框架 |
| `FluentAssertions` | 断言 |
| `Testcontainers.MySql` | 启动真实 MySQL 测试容器 |
| `Testcontainers.Redis` | 启动真实 Redis 测试容器 |
| `Respawn` | 重置数据库状态 |
| `Microsoft.NET.Test.Sdk` | 测试 SDK |

### InprovePlan.ApiTests

| 包 | 用途 |
|---|---|
| `xunit.v3` | API 测试框架 |
| `FluentAssertions` | HTTP 响应断言 |
| `Microsoft.AspNetCore.Mvc.Testing` | 提供 WebApplicationFactory |
| `Testcontainers.MySql` | API 测试数据库容器 |
| `Testcontainers.Redis` | API 测试 Redis 容器 |
| `Respawn` | 数据库清理 |
| `Microsoft.NET.Test.Sdk` | 测试 SDK |

## 5. 单元测试工程配置

单元测试工程引用 `InprovePlan.UserCase`、`InprovePlan.Domain`、`InprovePlan.ShareKernel` 等业务代码项目。

它不启动 MySQL、Redis、WebApplicationFactory，也不访问真实网络。测试重点是：

| 类型 | 示例 |
|---|---|
| 参数合法性 | 用户名不能为空、商品 ID 必须大于 0、分页参数必须合法 |
| 纯业务规则 | 排序字段白名单、分页计算 |
| Pipeline 行为 | AuthorizationBehavior 鉴权前置逻辑 |

## 6. 集成测试工程配置

集成测试工程使用 Testcontainers 启动 MySQL 和 Redis，使用 Respawn 在每个测试前重置数据库。

核心基础设施：

| 文件/目录 | 作用 |
|---|---|
| `Infrastructure/MySqlTestFixture.cs` | 管理 MySQL 容器生命周期、DbContext 创建、数据库重置 |
| `Infrastructure/RedisTestFixture.cs` | 管理 Redis 容器、缓存对象、Redis 清理 |
| `Infrastructure/IntegrationTestCollection.cs` | 将共享 Fixture 绑定到同一测试集合，避免重复启动容器 |
| `DataSeeders/` | 初始化测试所需业务数据 |
| `Builders/` | 构建符合业务规则的实体对象 |
| `TestDoubles/` | 提供 FakeCurrentUser、FakeOrderEventPublisher 等替身 |

## 7. 本地运行命令

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests\InprovePlan.UnitTests.csproj
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.IntegrationTests\InprovePlan.IntegrationTests.csproj
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.ApiTests\InprovePlan.ApiTests.csproj
```

## 8. 首次运行结果

| 测试项目 | 测试总数 | 通过 | 失败 | 结论 |
|---|---:|---:|---:|---|
| InprovePlan.UnitTests | 41 | 41 | 0 | Pass |
| InprovePlan.IntegrationTests | 24 | 24 | 0 | Pass |
| InprovePlan.ApiTests | 19 | 19 | 0 | Pass |

说明：ApiTests 曾出现一次测试集合清理阶段异常，但详细重跑后 19 个业务接口用例全部通过，当前作为 flaky 风险记录到 Day 5。

## 9. 当前问题与后续补充

| 问题 | 影响 | 后续处理 |
|---|---|---|
| 覆盖率采集未接入 | 无法给出真实覆盖率百分比 | 引入 `coverlet.collector` 和 ReportGenerator |
| API 鉴权失败/403 用例较少 | 对安全边界验证不足 | 补未登录、无权限、Token 异常用例 |
| 全局异常响应用例不足 | 无法确认异常格式完全一致 | 补充异常触发接口或测试专用替身 |
| CI/CD 尚未接入 | 无法自动阻断失败测试 | 后续在流水线中执行三类测试 |

## 10. 当日验收结论

测试工程分层、依赖包、基础设施和运行命令已经搭建完成。当前三个测试项目均可以独立通过，已经具备进入后续用例补齐、覆盖率门禁和 CI/CD 接入的基础。
