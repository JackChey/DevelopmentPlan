# Day 4 API、鉴权、异常与幂等测试

## 1. 当日目标

通过 `WebApplicationFactory` 和 `HttpClient` 验证 InprovePlan 后端业务 API 的真实 HTTP 行为，包括请求序列化、路由、模型绑定、依赖注入、数据库、Redis、缓存、幂等和响应结构。

## 2. API 测试范围

当前只测试业务接口，忽略指标、健康检查、测试辅助接口等非核心业务接口。

| 模块 | 当前覆盖 |
|---|---|
| Identity | 登录 |
| AppUsers | 创建、更新、修改密码、删除、根据 ID 查询、分页查询 |
| Products | 创建、更新、删除、根据 ID 查询、分页查询 |
| AppOrders | 创建、幂等创建、更新、修改状态、删除、根据 ID 查询、分页查询 |

## 3. WebApplicationFactory / TestServer 配置

当前 ApiTests 使用 `CustomWebApplicationFactory` 启动测试版 Web 应用。

核心职责：

| 能力 | 说明 |
|---|---|
| 创建 HttpClient | 通过真实 HTTP 请求访问 API |
| 替换数据库连接 | 将生产数据库替换为 Testcontainers MySQL |
| 替换 Redis | 将生产 Redis 替换为 Testcontainers Redis |
| 替换外部依赖 | 使用 FakeCurrentUser、FakeJwtService、FakeOrderEventPublisher 等测试替身 |
| 管理测试生命周期 | 初始化容器、重置数据库/缓存、释放资源 |

## 4. 鉴权测试范围

当前鉴权链路主要通过测试替身保证业务接口可调用，已覆盖登录成功场景，但未充分覆盖未登录、Token 无效、权限不足等失败路径。

| 编号 | 类型 | 场景 | 当前状态 |
|---|---|---|---|
| SEC-001 | 登录 | 用户名密码正确 | 已覆盖 |
| SEC-002 | 鉴权 | 无 Token 访问受保护接口 | 待补充 |
| SEC-003 | 鉴权 | Token 无效 | 待补充 |
| SEC-004 | 权限 | 当前用户访问非本人订单 | 待补充 |

## 5. 参数校验测试范围

当前 API 测试主要覆盖成功路径，参数校验失败更多由 UnitTests 中的 Validator 测试保证。

生产级建议：

| 场景 | 建议 |
|---|---|
| 请求体缺少必要字段 | API Tests 补 400 响应断言 |
| 路由 ID 非法 | API Tests 补 400/404 断言 |
| 枚举值非法 | UnitTests + API Tests 各保留一类代表用例 |
| 分页参数非法 | UnitTests 覆盖规则，API Tests 覆盖响应格式 |

## 6. 全局异常测试范围

当前未看到专门针对全局异常中间件或统一异常响应格式的 API 用例。

建议后续补充：

| 编号 | 场景 | 期望 |
|---|---|---|
| ERR-001 | 业务异常 | 返回统一业务错误结构 |
| ERR-002 | 未处理异常 | 返回统一错误结构，不泄露堆栈 |
| ERR-003 | 资源不存在 | 返回 404 或业务约定状态 |
| ERR-004 | 请求参数错误 | 返回 400 和字段级错误信息 |

## 7. 幂等测试范围

当前已覆盖订单幂等创建接口。

| 编号 | 类型 | 场景 | 请求条件 | 期望结果 | 是否通过 |
|---|---|---|---|---|---|
| IDEMP-001 | 幂等 | 创建订单 | Product/User 存在并使用幂等接口 | 创建订单成功 | Pass |

生产级后续建议：

| 场景 | 建议 |
|---|---|
| 相同 Idempotency-Key 重复提交 | 断言只创建一条订单 |
| 相同 Key 不同 Body | 断言返回冲突或业务约定错误 |
| 幂等处理中并发请求 | 断言不会重复写入 |
| 幂等结果缓存过期 | 断言过期后行为符合设计 |

## 8. API 集成测试用例列表

| 编号 | 接口模块 | 场景 | 期望结果 | 是否通过 |
|---|---|---|---|---|
| API-001 | Identity | 登录成功 | 返回 AccessToken | Pass |
| API-002 | AppUsers | 创建用户 | 返回用户数据 | Pass |
| API-003 | AppUsers | 更新用户 | 返回更新后用户 | Pass |
| API-004 | AppUsers | 修改密码 | 返回成功 | Pass |
| API-005 | AppUsers | 删除用户 | 用户删除成功 | Pass |
| API-006 | AppUsers | 根据 ID 查询 | 返回用户详情 | Pass |
| API-007 | AppUsers | 分页查询 | 返回分页用户 | Pass |
| API-008 | Products | 创建商品 | 返回商品数据 | Pass |
| API-009 | Products | 更新商品 | 返回更新后商品 | Pass |
| API-010 | Products | 删除商品 | 商品置为 Void | Pass |
| API-011 | Products | 根据 ID 查询 | 返回商品详情 | Pass |
| API-012 | Products | 分页查询 | 返回分页商品 | Pass |
| API-013 | AppOrders | 创建订单 | 返回订单数据 | Pass |
| API-014 | AppOrders | 幂等创建订单 | 返回订单数据 | Pass |
| API-015 | AppOrders | 更新订单 | 返回更新结果 | Pass |
| API-016 | AppOrders | 修改状态 | 返回状态变更结果 | Pass |
| API-017 | AppOrders | 删除订单 | 返回删除结果 | Pass |
| API-018 | AppOrders | 根据 ID 查询 | 返回订单详情 | Pass |
| API-019 | AppOrders | 分页查询 | 返回分页订单 | Pass |

## 9. 测试运行结果

运行命令：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.ApiTests\InprovePlan.ApiTests.csproj
```

结果：

| 总数 | 通过 | 失败 | 跳过 | 总耗时 |
|---:|---:|---:|---:|---:|
| 19 | 19 | 0 | 0 | 约 44.15 秒 |

说明：曾出现一次测试集合清理阶段异常导致进程返回失败码，但详细重跑后 19 个测试全部通过。该问题已记录到 Day 5 的 flaky 风险。

## 10. 当日验收结论

ApiTests 已经覆盖当前主要业务接口的成功路径，并验证了 WebApplicationFactory、HttpClient、MySQL、Redis、DI 替换和响应契约。生产级角度看，下一步重点是补齐鉴权失败、权限失败、异常响应、参数校验失败和幂等重复提交边界。
