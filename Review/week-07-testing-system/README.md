# 第 7 周测试体系文档索引

## 1. 文档目标

本目录用于记录 InprovePlan 项目第 7 周测试体系建设成果，覆盖单元测试、Handler 集成测试、API 集成测试、测试数据、测试环境、测试运行结果、覆盖率现状、风险与后续改进计划。

本周涉及的测试项目如下：

| 测试项目 | 路径 | 测试层级 | 当前结果 |
|---|---|---|---|
| InprovePlan.UnitTests | `D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests` | 单元测试 | 41 Passed |
| InprovePlan.IntegrationTests | `D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.IntegrationTests` | Handler 集成测试 | 24 Passed |
| InprovePlan.ApiTests | `D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.ApiTests` | API 集成测试 | 19 Passed |

## 2. 文档清单

| 文档 | 作用 |
|---|---|
| `day-01-test-project-setup.md` | 记录测试工程搭建、依赖包、项目职责边界和运行命令 |
| `day-02-unit-tests.md` | 记录单元测试范围、用例、Fake/Mock 边界和运行结果 |
| `day-03-integration-tests.md` | 记录 Handler 集成测试方案、MySQL/Redis 容器、数据初始化和清理 |
| `day-04-api-auth-exception-idempotency-tests.md` | 记录 API 测试、鉴权、异常、幂等和 HTTP 断言策略 |
| `day-05-flaky-test-fix.md` | 记录测试过程中发现的不稳定问题和修复经验 |
| `day-06-coverage-report.md` | 记录覆盖率采集现状、未接入原因和后续接入方案 |
| `day-07-test-layer-strategy.md` | 总结测试分层策略、边界、门禁和后续演进 |
| `test-case-list.md` | 汇总本周测试用例总表 |
| `week-07-acceptance-summary.md` | 第 7 周测试体系验收总结 |

## 3. 当前验收结论

当前三个测试项目均能独立运行通过：

```powershell
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.UnitTests\InprovePlan.UnitTests.csproj
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.IntegrationTests\InprovePlan.IntegrationTests.csproj
dotnet test D:\Code\InprovePlan\DevelopmentPlan\InprovePlan\InprovePlan.ApiTests\InprovePlan.ApiTests.csproj
```

覆盖率采集尚未正式接入。执行 `--collect:"XPlat Code Coverage"` 时提示当前测试工程找不到对应 datacollector，因此本周覆盖率文档只记录真实现状和接入方案，不填写虚假的覆盖率百分比。
