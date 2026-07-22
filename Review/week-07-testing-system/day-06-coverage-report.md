# Day 6 覆盖率统计与补弱

## 1. 当日目标

统计当前测试体系的覆盖率，识别覆盖不足区域，并制定补测计划。

## 2. 覆盖率工具说明

计划使用以下工具：

| 工具 | 作用 |
|---|---|
| `coverlet.collector` | 在 `dotnet test` 中采集覆盖率 |
| ReportGenerator | 将 Cobertura XML 转为 HTML/Markdown 报告 |
| CI/CD 覆盖率门禁 | 根据覆盖率阈值阻断低质量提交 |

## 3. 覆盖率生成命令

本次尝试执行：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests\InprovePlan.UnitTests.csproj --collect:"XPlat Code Coverage" --results-directory D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\TestResults\UnitTests
```

真实结果：

```text
数据收集: 找不到友好名称为“XPlat Code Coverage”的 datacollector。
数据收集: 找不到数据收集器“XPlat Code Coverage”
```

结论：当前测试项目尚未接入覆盖率采集器，因此本日不填写覆盖率百分比，避免产生不真实的质量结论。

## 4. 总体覆盖率结果

| 测试项目 | 测试结果 | 覆盖率结果 |
|---|---|---|
| InprovePlan.UnitTests | 41 Passed | 未生成 |
| InprovePlan.IntegrationTests | 24 Passed | 未生成 |
| InprovePlan.ApiTests | 19 Passed | 未生成 |

## 5. 核心模块覆盖率结果

当前无真实覆盖率文件，因此不填写百分比。

| 模块 | 行覆盖率 | 分支覆盖率 | 风险等级 | 是否需要补测 |
|---|---:|---:|---|---|
| AppUsers Validators | 未生成 | 未生成 | 中 | 是 |
| Products Validators | 未生成 | 未生成 | 中 | 是 |
| AppOrders Validators | 未生成 | 未生成 | 中 | 是 |
| AppUser Handlers | 未生成 | 未生成 | 中 | 是 |
| Product Handlers | 未生成 | 未生成 | 中 | 是 |
| AppOrder Handlers | 未生成 | 未生成 | 高 | 是 |
| API Controllers | 未生成 | 未生成 | 高 | 是 |

## 6. 覆盖率不足区域

虽然没有真实覆盖率百分比，但从当前用例结构可以判断以下区域需要补强：

| 区域 | 当前问题 | 建议 |
|---|---|---|
| 失败路径 | 成功路径较多，失败路径较少 | 补重复数据、不存在数据、无权限、参数错误 |
| 鉴权/权限 | API 层未充分覆盖 401/403 | 补未登录、Token 无效、非本人资源访问 |
| 全局异常 | 未见统一异常响应测试 | 补业务异常和未处理异常测试 |
| 幂等边界 | 已有成功路径，缺重复提交和冲突 Body | 补相同 Key 重复提交、相同 Key 不同 Body |
| 缓存边界 | 已验证部分写入/失效，缺过期和穿透 | 补缓存未命中、空值缓存、删除后读取 |

## 7. 补测计划

| 优先级 | 补测内容 | 所属项目 |
|---|---|---|
| P0 | API 401/403 鉴权权限用例 | InprovePlan.ApiTests |
| P0 | AppOrder 幂等重复提交用例 | InprovePlan.ApiTests / IntegrationTests |
| P1 | Handler 失败路径：商品不存在、用户不存在、订单不属于当前用户 | InprovePlan.IntegrationTests |
| P1 | 全局异常响应格式 | InprovePlan.ApiTests |
| P2 | Validator 边界值补齐 | InprovePlan.UnitTests |

## 8. 已补充的测试用例

当前已具备：

| 层级 | 已有用例数 | 说明 |
|---|---:|---|
| UnitTests | 41 | Validator、分页、排序、授权 Behavior |
| IntegrationTests | 24 | AppUser/Product/AppOrder Handler 成功路径和关键协作 |
| ApiTests | 19 | 主要业务 API 成功路径 |

## 9. 补测后的覆盖率变化

当前尚未生成覆盖率报告，因此无补测前后对比数据。

建议接入后执行：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.sln --collect:"XPlat Code Coverage" --results-directory D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\TestResults\Coverage
```

并生成报告：

```powershell
reportgenerator -reports:"D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\TestResults\Coverage\**\coverage.cobertura.xml" -targetdir:"D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\TestResults\CoverageReport" -reporttypes:"Html;MarkdownSummary"
```

## 10. 当日验收结论

测试用例已经具备一定规模，但覆盖率体系尚未完成。当前 Day 6 的真实结论是：测试能通过，覆盖率不能生成。下一步必须先接入 `coverlet.collector`，再根据真实覆盖率报告进行补弱。
