# Day 3 Handler 集成测试

## 1. 当日目标

为 `InprovePlan.UserCase` 中的 Command 和 Query Handler 建立集成测试，验证应用层与 EF Core、MySQL、Redis、缓存键、幂等组件、当前用户、事件发布替身之间的协作是否正确。

## 2. 集成测试方案

当前集成测试采用“真实基础设施 + 可控替身”的方式：

| 组件 | 测试方式 |
|---|---|
| MySQL | Testcontainers 启动真实 MySQL |
| Redis | Testcontainers 启动真实 Redis |
| DbContext | 通过测试 Fixture 创建真实 EF Core DbContext |
| Repository | 使用真实 EF Repository |
| 当前用户 | FakeCurrentUser |
| 密码哈希 | Fixture 提供测试密码哈希器 |
| ID 生成器 | Fixture 提供测试 ID 生成器 |
| MQ 发布 | FakeOrderEventPublisher |

这种方式比纯 Mock 更接近生产环境，同时又避免依赖开发机已有数据库。

## 3. Fixture / Collection 配置

| 文件 | 作用 |
|---|---|
| `Infrastructure/MySqlTestFixture.cs` | 启动 MySQL 容器，创建连接字符串，提供 `CreateDbContext()` 和 `ResetDatabaseAsync()` |
| `Infrastructure/RedisTestFixture.cs` | 启动 Redis 容器，创建 AppCache、CacheKeyBuilder，提供 Redis 清理能力 |
| `Infrastructure/IntegrationTestCollection.cs` | 将 MySQL/Redis Fixture 绑定到同一个 xUnit Collection，统一管理生命周期 |

`IntegrationTestCollection` 的意义是让多个测试类共享同一组 MySQL/Redis 容器，避免每个测试类重复启动容器，同时让 xUnit 知道这些测试存在共享状态，需要按集合规则管理生命周期。

## 4. 测试环境配置

| 配置项 | 当前做法 |
|---|---|
| 数据库 | `Testcontainers.MySql` |
| 缓存 | `Testcontainers.Redis` |
| 数据清理 | MySQL 使用 Respawn，Redis 使用清库/清 Key |
| 测试数据 | Builders + TestData + DataSeeders |
| 外部 MQ | FakeOrderEventPublisher |

## 5. 测试数据库策略

每个测试用例执行前调用：

```csharp
await _mysqlFixture.ResetDatabaseAsync();
await _redisFixture.ResetDatabaseAsync();
```

作用：

| 动作 | 意义 |
|---|---|
| 重置 MySQL | 保证测试之间没有脏数据互相影响 |
| 重置 Redis | 保证缓存命中、缓存失效、幂等 Key 不被其他测试污染 |
| 每个测试自行 Seed | 测试场景清晰，可读性高 |

## 6. 测试数据初始化与清理

当前使用三类对象共同构造测试场景：

| 类型 | 作用 |
|---|---|
| `TestData` | 存放稳定的测试常量，例如合法用户 ID、商品编码 |
| `Builder` | 构造符合业务规则的实体，例如 AppUserBuilder、ProductBuilder、AppOrderBuilder |
| `DataSeeder` | 将实体写入测试数据库 |

这种拆分的价值是：测试用例关注业务场景，Builder 关注对象构造，Seeder 关注落库。

## 7. Handler 集成测试用例列表

| 编号 | 模块 | 被测场景 | 期望结果 | 是否通过 |
|---|---|---|---|---|
| INT-001 | AppUser | 创建用户 | 写入用户并返回成功 | Pass |
| INT-002 | AppUser | 更新用户 | 用户信息被更新 | Pass |
| INT-003 | AppUser | 删除用户 | 用户被软删除 | Pass |
| INT-004 | AppUser | 修改密码 | 密码 Hash 更新 | Pass |
| INT-005 | AppUser | 登录 | 返回 AccessToken | Pass |
| INT-006 | AppUser | 根据 ID 查询用户 | 返回用户详情 | Pass |
| INT-007 | AppUser | 分页查询用户 | 返回分页数据 | Pass |
| INT-008 | Product | 创建商品 | 写入商品并返回成功 | Pass |
| INT-009 | Product | 更新商品 | 商品信息被更新 | Pass |
| INT-010 | Product | 删除商品 | 商品状态置为 Void | Pass |
| INT-011 | Product | 根据 ID 查询商品 | 返回商品详情 | Pass |
| INT-012 | Product | 分页查询商品 | 返回分页数据 | Pass |
| INT-013 | AppOrder | 创建订单 | 写入订单并返回成功 | Pass |
| INT-014 | AppOrder | 幂等创建订单 | 相同幂等条件下业务正确执行 | Pass |
| INT-015 | AppOrder | 更新订单 | 更新订单并删除缓存 | Pass |
| INT-016 | AppOrder | 删除订单 | 删除订单并删除缓存 | Pass |
| INT-017 | AppOrder | 修改订单状态 | 状态更新并删除缓存 | Pass |
| INT-018 | AppOrder | 幂等修改状态并发布事件 | 状态更新且 FakePublisher 捕获事件 | Pass |
| INT-019 | AppOrder | 根据 ID 查询订单 | 返回订单详情并验证缓存 | Pass |
| INT-020 | AppOrder | 分页查询订单 | 返回分页订单 | Pass |
| INT-021 | AppOrder | 查询订单测试接口 | 返回订单集合并避免明显 N+1 | Pass |
| INT-022 | AppOrder | Tracking 查询 | 返回订单集合 | Pass |
| INT-023 | AppOrder | NoTracking 查询 | 返回订单集合 | Pass |
| INT-024 | AppOrder | 慢 SQL 无索引查询测试 | 返回用户订单 | Pass |

## 8. 测试运行结果

运行命令：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.IntegrationTests\InprovePlan.IntegrationTests.csproj
```

结果：

| 总数 | 通过 | 失败 | 跳过 | 总耗时 |
|---:|---:|---:|---:|---:|
| 24 | 24 | 0 | 0 | 约 4 秒测试执行时间 |

说明：命令总耗时会包含项目编译、Docker 容器启动和销毁时间，实际机器上可能明显高于测试执行时间。

## 9. 与生产环境的差异说明

| 差异 | 当前做法 | 风险 |
|---|---|---|
| MQ 未连接真实 RabbitMQ | 使用 FakeOrderEventPublisher | 只能验证“是否调用发布”，不能验证真实 MQ 投递 |
| 当前用户非真实认证链路 | 使用 FakeCurrentUser | 不能覆盖 Token 解析和认证中间件 |
| Redis 是测试容器 | 与生产 Redis 版本/配置可能不同 | 需保证测试镜像版本与生产接近 |
| 数据库每次重置 | 保证测试隔离 | 与生产数据累积场景不同 |

## 10. 当日验收结论

IntegrationTests 已覆盖主要 Command/Query Handler 的成功路径和关键协作路径。当前 24 个测试全部通过，适合作为应用层回归测试。下一步应补充失败路径，例如重复用户名、商品不存在、订单不属于当前用户、缓存穿透等异常场景。
