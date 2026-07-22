# 第 7 周测试用例总表

## 1. 用例统计

| 测试项目 | 测试层级 | 测试方法/标记数 | 实际执行用例数 | 当前结果 |
|---|---|---:|---:|---|
| InprovePlan.UnitTests | Unit | 38 | 41 | Pass |
| InprovePlan.IntegrationTests | Integration | 24 | 24 | Pass |
| InprovePlan.ApiTests | API | 19 | 19 | Pass |
| 合计 | - | 81 | 84 | Pass |

说明：UnitTests 中部分 `[Theory]` 带多组 InlineData，因此实际执行用例数大于测试方法数。

## 2. 单元测试用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| UT-001 | Unit | AppUsers | 创建用户参数有效 | 合法用户数据 | 校验通过 | 校验通过 | Pass | Validator |
| UT-002 | Unit | AppUsers | 用户名为空 | Empty UserName | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-003 | Unit | AppUsers | 密码过短 | 短密码 | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-004 | Unit | AppUsers | 邮箱非法 | 非法 Email | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-005 | Unit | AppUsers | 更新用户参数有效 | 合法更新数据 | 校验通过 | 校验通过 | Pass | Validator |
| UT-006 | Unit | AppUsers | 登录参数有效 | 合法用户名密码 | 校验通过 | 校验通过 | Pass | Validator |
| UT-007 | Unit | AppUsers | 修改密码参数有效 | 新旧密码合法 | 校验通过 | 校验通过 | Pass | Validator |
| UT-008 | Unit | Products | 创建商品参数有效 | 合法商品数据 | 校验通过 | 校验通过 | Pass | Validator |
| UT-009 | Unit | Products | 商品编码超长 | 超长 ProductCode | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-010 | Unit | Products | 币种长度非法 | 非法 Currency | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-011 | Unit | Products | 删除商品 ID 非法 | Id <= 0 | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-012 | Unit | AppOrders | 创建订单参数有效 | 合法订单数据 | 校验通过 | 校验通过 | Pass | Validator |
| UT-013 | Unit | AppOrders | ProductId 非法 | ProductId <= 0 | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-014 | Unit | AppOrders | Quantity 为 0 | Quantity = 0 | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-015 | Unit | AppOrders | 状态非法 | 非法 OrderStatus | 返回校验错误 | 返回校验错误 | Pass | Validator |
| UT-016 | Unit | Paging | 分页参数有效 | PageIndex/PageSize 合法 | 校验通过 | 校验通过 | Pass | Pure logic |
| UT-017 | Unit | Paging | 分页参数非法 | PageIndex/PageSize 非法 | 返回校验错误 | 返回校验错误 | Pass | Theory |
| UT-018 | Unit | Sorting | 排序字段白名单 | 字段合法/非法 | 返回对应结果 | 返回对应结果 | Pass | Pure logic |
| UT-019 | Unit | Behaviors | 无授权属性 | Request 无 Attribute | 跳过授权 | 跳过授权 | Pass | Pipeline |
| UT-020 | Unit | Behaviors | 当前用户为空 | CurrentUser missing | 抛 Unauthorized | 抛 Unauthorized | Pass | Pipeline |
| UT-021 | Unit | Behaviors | 当前用户无效 | CurrentUser invalid | 抛 Forbidden | 抛 Forbidden | Pass | Pipeline |
| UT-022 | Unit | Behaviors | 当前用户有效 | CurrentUser valid | 继续执行 | 继续执行 | Pass | Pipeline |

## 3. 集成测试用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| INT-001 | Integration | AppUsers | 创建用户 | 用户名不存在 | 创建成功 | 创建成功 | Pass | MySQL |
| INT-002 | Integration | AppUsers | 更新用户 | 用户存在 | 更新成功 | 更新成功 | Pass | MySQL |
| INT-003 | Integration | AppUsers | 删除用户 | 用户存在 | 软删除 | 软删除 | Pass | MySQL |
| INT-004 | Integration | AppUsers | 修改密码 | 旧密码正确 | Hash 更新 | Hash 更新 | Pass | PasswordHasher |
| INT-005 | Integration | AppUsers | 登录 | 密码正确 | 返回 Token | 返回 Token | Pass | Jwt fake |
| INT-006 | Integration | AppUsers | 按 ID 查询 | 用户存在 | 返回用户 | 返回用户 | Pass | Query |
| INT-007 | Integration | AppUsers | 分页查询 | 用户匹配条件 | 返回分页 | 返回分页 | Pass | Query |
| INT-008 | Integration | Products | 创建商品 | 编码不存在 | 创建成功 | 创建成功 | Pass | MySQL |
| INT-009 | Integration | Products | 更新商品 | 商品存在 | 更新成功 | 更新成功 | Pass | MySQL |
| INT-010 | Integration | Products | 删除商品 | 商品存在 | 状态置 Void | 状态置 Void | Pass | MySQL |
| INT-011 | Integration | Products | 按 ID 查询 | 商品存在 | 返回商品 | 返回商品 | Pass | Query |
| INT-012 | Integration | Products | 分页查询 | 商品匹配条件 | 返回分页 | 返回分页 | Pass | Query |
| INT-013 | Integration | AppOrders | 创建订单 | 商品和用户存在 | 创建成功 | 创建成功 | Pass | MySQL |
| INT-014 | Integration | AppOrders | 幂等创建订单 | 有幂等上下文 | 创建成功 | 创建成功 | Pass | Redis |
| INT-015 | Integration | AppOrders | 更新订单 | 当前用户拥有订单 | 更新并删除缓存 | 更新并删除缓存 | Pass | Redis |
| INT-016 | Integration | AppOrders | 删除订单 | 当前用户拥有订单 | 删除并删除缓存 | 删除并删除缓存 | Pass | Redis |
| INT-017 | Integration | AppOrders | 修改状态 | 订单存在 | 状态更新 | 状态更新 | Pass | Redis |
| INT-018 | Integration | AppOrders | 修改状态并发事件 | 订单存在 | 发布事件 | 发布事件 | Pass | Fake MQ |
| INT-019 | Integration | AppOrders | 按 ID 查询 | 用户拥有订单 | 返回订单并验证缓存 | 返回订单并验证缓存 | Pass | Redis |
| INT-020 | Integration | AppOrders | 分页查询 | 订单匹配条件 | 返回分页 | 返回分页 | Pass | Query |
| INT-021 | Integration | AppOrders | 查询订单测试接口 | 订单存在 | 返回订单集合 | 返回订单集合 | Pass | Query |
| INT-022 | Integration | AppOrders | Tracking 查询 | 订单存在 | 返回订单集合 | 返回订单集合 | Pass | EF Tracking |
| INT-023 | Integration | AppOrders | NoTracking 查询 | 订单存在 | 返回订单集合 | 返回订单集合 | Pass | EF NoTracking |
| INT-024 | Integration | AppOrders | 慢 SQL 无索引查询 | 用户有订单 | 返回用户订单 | 返回用户订单 | Pass | Query |

## 4. 鉴权测试用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| SEC-001 | Unit | AuthorizationBehavior | 请求无授权属性 | 无 Attribute | 继续执行 | 继续执行 | Pass | 已覆盖 |
| SEC-002 | Unit | AuthorizationBehavior | 当前用户为空 | CurrentUser missing | Unauthorized | Unauthorized | Pass | 已覆盖 |
| SEC-003 | Unit | AuthorizationBehavior | 当前用户无效 | CurrentUser invalid | Forbidden | Forbidden | Pass | 已覆盖 |
| SEC-004 | Unit | AuthorizationBehavior | 当前用户有效 | CurrentUser valid | 继续执行 | 继续执行 | Pass | 已覆盖 |
| SEC-005 | API | Identity | 登录成功 | 用户名密码正确 | 返回 Token | 返回 Token | Pass | 已覆盖 |
| SEC-006 | API | 受保护接口 | 未登录访问 | 无 Token | 401 | 未执行 | Todo | 待补充 |
| SEC-007 | API | 受保护接口 | 无权限访问 | 非授权用户 | 403 | 未执行 | Todo | 待补充 |

## 5. 异常测试用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| ERR-001 | API | GlobalException | 参数错误 | 非法请求体 | 统一 400 响应 | 未执行 | Todo | 待补充 |
| ERR-002 | API | GlobalException | 业务异常 | 触发业务错误 | 统一错误响应 | 未执行 | Todo | 待补充 |
| ERR-003 | API | GlobalException | 未处理异常 | 模拟异常 | 不泄露堆栈 | 未执行 | Todo | 待补充 |

## 6. 幂等测试用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| IDEMP-001 | Integration | AppOrders | 幂等创建订单 | 商品和用户存在 | 创建成功 | 创建成功 | Pass | 已覆盖 |
| IDEMP-002 | Integration | AppOrders | 幂等改状态并发布事件 | 订单存在 | 发布事件并更新状态 | 发布事件并更新状态 | Pass | 已覆盖 |
| IDEMP-003 | API | AppOrders | 幂等创建订单 | 请求有效 | 创建成功 | 创建成功 | Pass | 已覆盖 |
| IDEMP-004 | API | AppOrders | 重复提交订单 | 相同 Idempotency-Key | 只创建一次 | 未执行 | Todo | 待补充 |
| IDEMP-005 | API | AppOrders | 同 Key 不同 Body | Key 相同请求体不同 | 返回冲突或约定错误 | 未执行 | Todo | 待补充 |

## 7. Flaky Test 验证用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| FLAKY-001 | Integration | Redis Fixture | Redis 初始化 | 使用 xUnit Fixture | 容器可用 | 容器可用 | Pass | 已修复 |
| FLAKY-002 | Integration | Cache | 泛型一致性 | 同 Key 读取 DTO | 不抛转换异常 | 不抛异常 | Pass | 已修复 |
| FLAKY-003 | API | Cleanup | 集合清理 | 运行 ApiTests | 正常释放 | 重跑通过 | Watch | 需连续验证 |

## 8. 覆盖率补测用例

| 编号 | 测试层级 | 模块 | 场景 | 输入条件 | 期望结果 | 实际结果 | 状态 | 备注 |
|---|---|---|---|---|---|---|---|---|
| COV-001 | All | Coverage | 生成覆盖率报告 | `--collect:"XPlat Code Coverage"` | 生成 cobertura.xml | collector 未找到 | Todo | 需接入 coverlet |
| COV-002 | Unit | Validators | 补边界值 | 长度、精度、枚举 | 提升分支覆盖 | 未执行 | Todo | 待覆盖率接入后执行 |
| COV-003 | API | Auth/Exception | 补失败路径 | 401/403/400/500 | 提升关键路径覆盖 | 未执行 | Todo | 待补充 |

## 9. 未完成用例

| 优先级 | 用例 |
|---|---|
| P0 | 未登录访问受保护接口返回 401 |
| P0 | 普通用户访问非本人订单返回 403 或业务约定错误 |
| P0 | 相同 Idempotency-Key 重复提交只创建一条订单 |
| P1 | 商品不存在时创建订单失败 |
| P1 | 用户不存在时创建订单失败 |
| P1 | 全局异常统一响应格式 |
| P2 | 覆盖率报告生成和门禁 |

## 10. 用例结论

当前已执行测试共 84 个，全部通过。现有测试体系已经覆盖主要成功路径和基础规则，但生产级完整性仍需要继续补失败路径、安全边界、幂等边界和覆盖率门禁。
