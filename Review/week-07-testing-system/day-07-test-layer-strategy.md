# Day 7 测试分层策略

## 1. 文档目标

总结 InprovePlan 当前测试体系的分层策略、边界、数据管理方式、Mock/Fake 使用原则、覆盖率门禁和后续演进计划。

## 2. 当前项目测试目标

| 目标 | 说明 |
|---|---|
| 快速发现基础规则错误 | UnitTests 覆盖 Validator、分页、排序、授权 Behavior |
| 验证应用层真实协作 | IntegrationTests 覆盖 Handler 与 MySQL/Redis/Repository/Cache |
| 验证接口真实可用 | ApiTests 覆盖 WebApplicationFactory + HTTP 请求 |
| 形成可进入 CI/CD 的测试基线 | 三类测试均可通过，后续补覆盖率门禁 |

## 3. 测试金字塔策略

```text
          ApiTests
   验证 HTTP、路由、DI、认证、响应契约

      IntegrationTests
   验证 Handler、仓储、MySQL、Redis、幂等、缓存

         UnitTests
   验证参数校验、纯逻辑、边界值、Pipeline 行为
```

原则：

| 层级 | 数量策略 | 速度 | 主要价值 |
|---|---|---|---|
| UnitTests | 多 | 快 | 快速反馈最小规则是否正确 |
| IntegrationTests | 中 | 中 | 验证应用层与真实基础设施协作 |
| ApiTests | 少而关键 | 慢 | 验证业务接口真实可调用 |

## 4. 单元测试策略

UnitTests 只测试最小功能和纯逻辑。

适合：

| 类型 | 示例 |
|---|---|
| 参数合法性 | 用户名不能为空、商品 ID 必须大于 0 |
| 边界值 | 分页大小、金额精度、枚举合法性 |
| 纯业务工具 | 分页结果、排序白名单 |
| Pipeline 行为 | AuthorizationBehavior |

不适合：

| 类型 | 原因 |
|---|---|
| 数据是否存在 | 需要数据库 |
| Redis 是否写入 | 需要外部缓存 |
| API 状态码 | 需要 HTTP 层 |

## 5. 集成测试策略

IntegrationTests 负责验证 UserCase 层 Command/Query Handler 的真实依赖协作。

当前标准做法：

| 事项 | 当前策略 |
|---|---|
| 数据库 | Testcontainers MySQL |
| 缓存 | Testcontainers Redis |
| 清理 | 每个测试前 ResetDatabaseAsync |
| 数据准备 | Builder + TestData + DataSeeder |
| MQ | FakeOrderEventPublisher |
| 当前用户 | FakeCurrentUser |

## 6. API / 端到端测试策略

ApiTests 负责验证从 HTTP 请求到业务执行再到 HTTP 响应的完整链路。

当前覆盖：

| 模块 | 用例数 |
|---|---:|
| Identity | 1 |
| AppUsers | 6 |
| Products | 5 |
| AppOrders | 7 |

忽略范围：

| 类型 | 原因 |
|---|---|
| 指标接口 | 非业务接口 |
| 健康检查 | 可独立放运维/基础设施测试 |
| 控制器内部测试辅助接口 | 非对外业务能力 |

## 7. Mock 与真实依赖边界

| 依赖 | UnitTests | IntegrationTests | ApiTests |
|---|---|---|---|
| 数据库 | 不使用 | 真实 MySQL 容器 | 真实 MySQL 容器 |
| Redis | 不使用 | 真实 Redis 容器 | 真实 Redis 容器 |
| 当前用户 | Fake | Fake | Fake/测试认证 |
| JWT | 不使用 | Fake | FakeJwtService |
| MQ | 不使用 | FakePublisher | FakePublisher |
| Repository | Mock/Fake 或不使用 | 真实 EF Repository | 由真实 DI 注入 |

核心原则：越靠近底层的规则越少 Mock，越靠近外部系统且不可控的依赖越使用测试容器或 Fake。

## 8. 测试数据管理策略

| 类型 | 作用 |
|---|---|
| `TestData` | 存放稳定常量 |
| `Builder` | 构造有效实体和场景 |
| `DataSeeder` | 将测试数据写入数据库 |
| `ResetDatabaseAsync` | 清理数据库和缓存状态 |

命名建议：

| 类型 | 示例 |
|---|---|
| 测试方法 | `Handle_WhenOrderExistsAndCurrentUserOwnsOrder_ShouldReturnOrder` |
| 测试数据 | `ValidUserId`、`ValidProductCode` |
| Builder 方法 | `WithUserId`、`WithProductId` |

## 9. 鉴权、异常、幂等测试策略

| 类型 | 当前状态 | 后续策略 |
|---|---|---|
| 鉴权 | 登录成功已覆盖 | 补 401、Token 无效 |
| 权限 | Unit 层有 AuthorizationBehavior | API/Integration 补非本人资源 |
| 参数校验 | Unit 层较完整 | API 层补代表性 400 |
| 全局异常 | 未形成专门用例 | 补统一异常响应 |
| 幂等 | 已有成功路径 | 补重复提交、冲突 Body、并发 |

## 10. 覆盖率门禁标准

当前覆盖率未接入。建议门禁分阶段推进：

| 阶段 | 标准 |
|---|---|
| 第一阶段 | 能生成覆盖率报告，不设置失败门禁 |
| 第二阶段 | UnitTests 行覆盖率不低于 70% |
| 第三阶段 | 核心 UserCase 行覆盖率不低于 75%，分支覆盖率不低于 60% |
| 第四阶段 | 关键业务模块行覆盖率不低于 80% |

## 11. Flaky Test 处理原则

| 原则 | 说明 |
|---|---|
| 不忽略偶发失败 | 记录并分析根因 |
| 先区分断言失败和清理失败 | 清理失败也会影响 CI 可信度 |
| 容器测试顺序执行 | 减少共享资源竞争 |
| 数据每次 Reset | 避免测试顺序依赖 |
| 连续运行验证 | 修复后至少连续运行 5-10 次 |

## 12. CI/CD 接入计划

建议流水线顺序：

```text
restore
build
unit tests
integration tests
api tests
coverage
publish test results
publish coverage report
```

推荐命令：

```powershell
dotnet restore D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.sln
dotnet build D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.sln --no-restore
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests\InprovePlan.UnitTests.csproj --no-build
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.IntegrationTests\InprovePlan.IntegrationTests.csproj --no-build
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.ApiTests\InprovePlan.ApiTests.csproj --no-build
```

## 13. 当前测试体系不足

| 不足 | 风险 |
|---|---|
| 覆盖率未接入 | 无法量化测试充分性 |
| API 失败路径不足 | 无法证明错误响应一致 |
| 鉴权权限边界不足 | 无法证明安全边界 |
| 幂等重复提交未完全覆盖 | 无法证明重复请求只产生一次副作用 |
| MQ 使用 Fake | 无法验证真实消息中间件投递 |

## 14. 下一阶段改进计划

| 优先级 | 任务 |
|---|---|
| P0 | 接入 coverlet.collector 和 ReportGenerator |
| P0 | 补 API 401/403/400/404 用例 |
| P0 | 补订单幂等重复提交用例 |
| P1 | 补 Handler 失败路径 |
| P1 | 补全局异常响应测试 |
| P2 | 接入 CI/CD 并发布测试报告 |

## 15. 最终验收结论

第 7 周已经建立了可运行的三层测试体系：UnitTests、IntegrationTests、ApiTests。当前测试结果为 41 + 24 + 19 全部通过，能够支撑日常开发回归。生产级角度看，下一阶段最关键的是覆盖率门禁、失败路径、安全边界和 CI/CD 自动化。
