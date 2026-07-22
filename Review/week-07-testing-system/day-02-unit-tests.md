# Day 2 核心服务单元测试

## 1. 当日目标

为项目中的最小业务规则建立单元测试，保证基础输入校验、分页排序规则、授权 Pipeline 行为在不依赖外部环境的情况下可以快速验证。

## 2. 被测核心服务清单

| 模块 | 被测对象 |
|---|---|
| AppUsers | Create/Update/Login/ChangePassword Command Validator |
| AppOrders | Create/ChangeStatus/GetPaged Validator |
| Products | Create/Update/Delete/GetPaged Validator |
| Paging | Pagination、PagedResult |
| Sorting | AppUserSortWhitelist |
| Behaviors | AuthorizationBehavior |

## 3. 单元测试范围说明

单元测试只验证“不需要真实外部环境也能判断”的规则。

适合放在 UnitTests 的内容：

| 类型 | 示例 |
|---|---|
| 参数格式校验 | 用户名不能为空、邮箱格式错误、密码长度不足 |
| 数值边界校验 | 商品 ID 必须大于 0、数量不能为 0、分页大小不能越界 |
| 枚举合法性 | 订单状态、商品状态必须是合法枚举值 |
| 纯计算逻辑 | 分页 Skip 计算、空集合处理 |
| 纯内存授权逻辑 | 当前用户为空时抛 Unauthorized，用户无效时抛 Forbidden |

不适合放在 UnitTests 的内容：

| 类型 | 应放位置 |
|---|---|
| 商品编号是否已存在 | IntegrationTests |
| 用户手机号是否已存在 | IntegrationTests |
| Redis 缓存是否写入 | IntegrationTests / ApiTests |
| API 返回状态码是否正确 | ApiTests |

## 4. Mock / Fake 依赖说明

当前 UnitTests 中主要使用轻量 TestDouble，而不是启动真实环境。

| 依赖 | 处理方式 | 原因 |
|---|---|---|
| 当前用户 | FakeCurrentUser | AuthorizationBehavior 只需要用户身份状态 |
| Handler next 委托 | 内存委托 | 验证 Pipeline 是否继续向后执行 |
| 数据库/Redis | 不使用 | 单元测试保持快速、稳定、无外部依赖 |

## 5. 单元测试用例列表

| 编号 | 被测类 | 被测方法 | 测试场景 | 期望结果 | 是否通过 |
|---|---|---|---|---|---|
| UT-001 | CreateAppUserCommandValidator | Validate | 命令有效 | 校验通过 | Pass |
| UT-002 | CreateAppUserCommandValidator | Validate | UserName 为空 | 返回用户名校验错误 | Pass |
| UT-003 | CreateAppUserCommandValidator | Validate | Password 长度不足 | 返回密码校验错误 | Pass |
| UT-004 | CreateAppUserCommandValidator | Validate | Email 格式错误 | 返回邮箱校验错误 | Pass |
| UT-005 | UpdateAppUserCommandValidator | Validate | 命令有效 | 校验通过 | Pass |
| UT-006 | UpdateAppUserCommandValidator | Validate | Id 非法 | 返回 Id 校验错误 | Pass |
| UT-007 | UpdateAppUserCommandValidator | Validate | Email 格式错误 | 返回邮箱校验错误 | Pass |
| UT-008 | LoginAppUserCommandValidator | Validate | 命令有效 | 校验通过 | Pass |
| UT-009 | LoginAppUserCommandValidator | Validate | UserName 为空 | 返回用户名校验错误 | Pass |
| UT-010 | LoginAppUserCommandValidator | Validate | Password 为空 | 返回密码校验错误 | Pass |
| UT-011 | ChangeAppUserPasswordCommandValidator | Validate | 命令有效 | 校验通过 | Pass |
| UT-012 | ChangeAppUserPasswordCommandValidator | Validate | 确认密码不一致 | 返回确认密码校验错误 | Pass |
| UT-013 | ChangeAppUserPasswordCommandValidator | Validate | 新旧密码相同 | 返回新密码校验错误 | Pass |
| UT-014 | AppOrderValidator | Create | 命令有效 | 校验通过 | Pass |
| UT-015 | AppOrderValidator | Create | ProductId 非法 | 返回 ProductId 校验错误 | Pass |
| UT-016 | AppOrderValidator | Create | Quantity 为 0 | 返回 Quantity 校验错误 | Pass |
| UT-017 | AppOrderValidator | Create | Quantity 小数位非法 | 返回 Quantity 校验错误 | Pass |
| UT-018 | AppOrderValidator | ChangeStatus | 状态非法 | 返回 OrderStatus 校验错误 | Pass |
| UT-019 | AppOrderValidator | GetPaged | StartTime 大于 EndTime | 返回日期范围错误 | Pass |
| UT-020 | ProductValidator | Create | 命令有效 | 校验通过 | Pass |
| UT-021 | ProductValidator | Create | ProductCode 超长 | 返回 ProductCode 校验错误 | Pass |
| UT-022 | ProductValidator | Create | Currency 长度非法 | 返回 Currency 校验错误 | Pass |
| UT-023 | ProductValidator | Update | ProductStatus 非法 | 返回 ProductStatus 校验错误 | Pass |
| UT-024 | ProductValidator | Delete | Id 非法 | 返回 Id 校验错误 | Pass |
| UT-025 | ProductValidator | GetPaged | 查询参数有效 | 校验通过 | Pass |
| UT-026 | PagedResult | Create | 正常分页结果 | 返回正确分页数据 | Pass |
| UT-027 | PagedResult | Create | Items 为 null | 返回空集合 | Pass |
| UT-028 | Pagination | Validate | 分页参数有效 | 校验通过 | Pass |
| UT-029 | Pagination | Validate | PageIndex 非法 | 返回分页错误 | Pass |
| UT-030 | Pagination | Validate | PageSize 非法 | 返回分页错误 | Pass |
| UT-031 | Pagination | GetSkipCount | PageIndex/PageSize 有效 | 返回正确 Skip | Pass |
| UT-032 | AppUserSortWhitelist | Validate | 排序字段允许 | 校验通过 | Pass |
| UT-033 | AppUserSortWhitelist | Validate | 排序字段不允许 | 校验失败 | Pass |
| UT-034 | AppUserSortWhitelist | Validate | 排序方向非法 | 校验失败 | Pass |
| UT-035 | AuthorizationBehavior | Handle | 请求无授权属性 | 跳过授权继续执行 | Pass |
| UT-036 | AuthorizationBehavior | Handle | 当前用户为空 | 抛 Unauthorized | Pass |
| UT-037 | AuthorizationBehavior | Handle | 当前用户无效 | 抛 Forbidden | Pass |
| UT-038 | AuthorizationBehavior | Handle | 当前用户有效 | 继续执行 | Pass |

说明：由于部分 `[Theory]` 包含多组 InlineData，测试方法数量为 38，实际测试用例执行数为 41。

## 6. 关键测试代码说明

单元测试使用 Arrange / Act / Assert 结构：

| 阶段 | 作用 |
|---|---|
| Arrange | 准备命令、查询、Fake 依赖和预期数据 |
| Act | 调用被测方法，例如 Validator.Validate 或 Behavior.Handle |
| Assert | 使用 FluentAssertions 判断结果是否符合预期 |

典型断言方式：

```csharp
result.IsValid.Should().BeFalse();
result.Errors.Should().Contain(x => x.PropertyName == "UserName");
```

## 7. 测试运行结果

运行命令：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests\InprovePlan.UnitTests.csproj
```

结果：

| 总数 | 通过 | 失败 | 跳过 | 总耗时 |
|---:|---:|---:|---:|---:|
| 41 | 41 | 0 | 0 | 约 8.56 秒 |

## 8. 未覆盖逻辑说明

| 未覆盖内容 | 原因 | 建议放置位置 |
|---|---|---|
| 用户名是否已存在 | 需要数据库 | IntegrationTests |
| 商品编号是否已存在 | 需要数据库 | IntegrationTests |
| 订单创建后是否写入数据库 | 需要 EF Core 和 MySQL | IntegrationTests |
| 缓存读写和失效 | 需要 Redis | IntegrationTests / ApiTests |
| API 状态码和响应结构 | 需要 WebApplicationFactory | ApiTests |

## 9. 风险与后续补测计划

| 风险 | 建议 |
|---|---|
| Validator 边界值仍可继续补充 | 对长度、精度、枚举边界继续补等价类和边界值 |
| AuthorizationBehavior 只覆盖核心路径 | 后续补角色、权限策略、多 Attribute 组合 |
| UnitTests 没有覆盖所有公共工具类 | 后续按变更频率和风险补充 |

## 10. 当日验收结论

UnitTests 已形成可快速运行的基础质量防线。当前 41 个测试全部通过，适合作为开发阶段的第一层回归测试。
