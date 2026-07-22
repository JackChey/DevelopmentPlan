# Day 5 Flaky Test 修复记录

## 1. 当日目标

记录测试项目建设过程中出现过的不稳定问题、根因、修复方式和防复发规则，保证测试体系不仅能跑通，而且能稳定重复运行。

## 2. Flaky Test 定义

Flaky Test 是指业务代码没有变化，但测试在不同时间、不同顺序或不同机器上出现偶发失败的测试。

常见原因：

| 类型 | 示例 |
|---|---|
| 生命周期问题 | Fixture 未初始化就使用 Container |
| 共享状态污染 | 数据库、Redis 未清理 |
| 资源释放顺序 | WebApplicationFactory 先释放，缓存/连接仍在使用 |
| 容器依赖不稳定 | Docker 未启动、镜像拉取失败、端口冲突 |
| 并发执行问题 | 多个测试同时清理同一 Redis 或数据库 |

## 3. 发现的不稳定测试

| 测试/阶段 | 不稳定表现 | 当前状态 |
|---|---|---|
| Redis Fixture 初始化 | `Could not find resource 'RedisContainer'` | 已修复 |
| CacheEnvelope 泛型读取 | `InvalidCastException` | 已修复 |
| ApiTests 集合清理 | 用例通过但 cleanup 偶发异常 | 当前重跑通过，需继续观察 |
| 覆盖率采集 | 找不到 `XPlat Code Coverage` datacollector | 未接入，非测试失败 |

## 4. 不稳定原因分析

### 4.1 Redis Fixture 未初始化

表现：

```text
Could not find resource 'RedisContainer'. Please create the resource by calling StartAsync(CancellationToken) or CreateAsync(CancellationToken).
```

根因：

测试类或 `CustomWebApplicationFactory` 中直接 `new RedisTestFixture()`，不会自动触发 xUnit 的 `InitializeAsync()`。xUnit 只会管理通过 `IClassFixture`、`ICollectionFixture` 注入的 Fixture 生命周期。

修复方式：

| 错误做法 | 正确做法 |
|---|---|
| 手动 `new RedisTestFixture()` 后直接使用 | 让 xUnit 注入 Fixture，或在自定义 Factory 中显式调用初始化 |
| Fixture 和 WebApplicationFactory 生命周期分散 | 统一由测试集合或 Factory 管理生命周期 |

### 4.2 CacheEnvelope 泛型不一致

表现：

```text
Unable to cast object of type 'CacheEnvelope<AppOrderDto>' to type 'CacheEnvelope<AppOrder>'
```

根因：

相同 Redis Key 第一次写入的是 `AppOrderDto`，后续使用同一个 Key 读取 `AppOrder`。Redis 里只有字符串，但测试缓存封装层会按泛型类型反序列化并转换 Envelope，泛型类型不一致会导致强制转换失败。

修复方式：

| 规则 | 说明 |
|---|---|
| 同一个缓存 Key 只对应一个数据模型 | 例如订单详情 Key 固定存 `AppOrderDto` |
| 测试读取缓存使用 `GetAsync<AppOrderDto>` | 不再用 `GetOrSetAsync<AppOrder>` 去验证已存在缓存 |
| 回源逻辑只在 Handler 内测试 | 测试断言缓存结果，不重复模拟 Handler 的回源逻辑 |

### 4.3 ApiTests 集合清理偶发异常

表现：

第一次运行时，19 个 API 用例全部通过，但集合 cleanup 阶段出现异常并返回失败码；详细重跑后 19/19 Pass。

可能原因：

| 原因 | 说明 |
|---|---|
| Dispose 顺序问题 | WebApplicationFactory、Redis、FusionCache、ConnectionMultiplexer 释放顺序不稳定 |
| 容器释放时仍有后台任务 | 缓存 backplane、事件日志或 Redis 连接仍在清理 |
| 外部 Docker 状态波动 | 容器启动/停止过程中存在瞬时状态 |

建议修复：

| 建议 | 说明 |
|---|---|
| 明确 Factory Dispose 顺序 | 先释放 ASP.NET Core Host，再释放 Redis，再释放 MySQL |
| 避免测试中保留旧 AppCache 引用 | 每次 Reset 后使用当前 Fixture 中的缓存实例 |
| 连续运行验证 | 对 ApiTests 连续运行 5-10 次，确认 cleanup 不再偶发失败 |

## 5. 修复方案

| 问题 | 修复方案 |
|---|---|
| Fixture 手动 new 后未初始化 | 改为 xUnit Fixture 注入，或在 Factory 初始化阶段显式 await 初始化 |
| MySQL/SQL Server Builder 混用 | 使用 `Testcontainers.MySql.MySqlBuilder` 和 `Testcontainers.MySql` 包 |
| Redis Key 类型混用 | 同一 Key 固定 DTO 类型，新增只读 `GetAsync<T>` 验证缓存 |
| 数据污染 | 每个测试前调用 MySQL 和 Redis Reset |
| 外部 MQ 不稳定 | 使用 FakeOrderEventPublisher 替代真实 MQ |

## 6. 修复前后对比

| 项目 | 修复前 | 修复后 |
|---|---|---|
| IntegrationTests | Redis 容器可能未初始化 | 24 个测试全部通过 |
| ApiTests | 依赖替换和释放顺序存在风险 | 详细重跑 19 个测试全部通过 |
| 缓存测试 | 泛型类型不一致导致转换异常 | 使用 `GetAsync<AppOrderDto>` 验证缓存 |

## 7. 连续运行验证结果

当前已执行：

| 项目 | 验证结果 |
|---|---|
| InprovePlan.UnitTests | 41 Passed |
| InprovePlan.IntegrationTests | 24 Passed |
| InprovePlan.ApiTests | 19 Passed |

ApiTests 曾出现一次 cleanup 阶段异常，详细重跑通过。该问题建议进入后续连续运行验证清单。

## 8. 防止再次出现的规则

| 规则 | 说明 |
|---|---|
| Fixture 生命周期只交给 xUnit 或统一 Factory 管理 | 避免手动 new 后忘记初始化 |
| 每个测试独立 Reset | 数据库和 Redis 都必须清理 |
| 同一 Redis Key 固定泛型模型 | 禁止一个 Key 同时存 Entity 和 DTO |
| 外部系统默认用测试容器或 Fake | 不依赖开发机本地服务 |
| API 测试不并行抢共享状态 | 对共享数据库/Redis 的测试使用 Collection 控制 |

## 9. 当前仍存在的风险

| 风险 | 影响 | 建议 |
|---|---|---|
| ApiTests cleanup 偶发异常未连续压测 | CI 中可能偶发失败 | 连续运行 10 次并观察 |
| 覆盖率 collector 未接入 | 无法建立质量门禁 | Day 6 接入 coverlet |
| 幂等并发场景未覆盖 | 不能证明重复提交只写一次 | 补并发/重复 Key 用例 |

## 10. 当日验收结论

主要不稳定问题已经明确根因并形成处理规则。当前测试项目均可通过，但 ApiTests cleanup 偶发异常和覆盖率采集缺口需要在后续 CI/CD 接入前继续处理。
