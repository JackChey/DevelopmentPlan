using Prometheus;

namespace InprovePlan.Prometheus.AppMetrics;

/// <summary>
/// 应用自定义指标中心（Prometheus）
///
/// 设计目标：
/// 1. 统一维护“业务/依赖”类计数指标，避免散落在各处硬编码。
/// 2. 指标命名稳定，便于告警规则和仪表盘长期复用。
/// 3. 控制标签基数（Cardinality），防止时序爆炸影响 Prometheus 性能。
///
/// 命名约定：
/// - 全小写 + 下划线
/// - counter 类指标以 _total 结尾
/// - 标签值尽量有限枚举，不使用用户ID/URL原文等高基数字段
/// </summary>
public static class AppCustomMetrics
{
    /// <summary>
    /// Prometheus 查询失败总次数（Counter）
    ///
    /// 指标名：
    /// prometheus_query_fail_total{reason="..."}
    ///
    /// 用途：
    /// - 告警：监控“Prometheus 查询能力”是否异常（网络、超时、解析异常等）
    /// - 排障：按 reason 观察失败类型分布
    ///
    /// 标签：
    /// reason（已做归一化，避免高基数）
    /// </summary>
    public static readonly Counter PrometheusQueryFailTotal = Metrics.CreateCounter(
        name: "prometheus_query_fail_total",
        help: "Count of Prometheus query failures by normalized reason.",
        configuration: new CounterConfiguration
        {
            LabelNames = new[] { "reason" }
        });

    /// <summary>
    /// 未授权访问总次数（401）Counter
    ///
    /// 指标名：
    /// auth_access_unauthorized_total
    ///
    /// 用途：
    /// - 安全告警：401 异常突增
    /// - 运营分析：认证失败趋势（例如 token 过期策略变更影响）
    ///
    /// 注意：
    /// - 建议只在一个入口计数（例如 JwtBearerEvents.OnChallenge），避免重复计数。
    /// </summary>
    public static readonly Counter AuthAccessUnauthorizedTotal = Metrics.CreateCounter(
        name: "auth_access_unauthorized_total",
        help: "Count of unauthorized (401) access events.");

    /// <summary>
    /// 禁止访问总次数（403）Counter
    ///
    /// 指标名：
    /// auth_access_forbidden_total
    ///
    /// 用途：
    /// - 安全告警：403 异常突增
    /// - 排障：权限模型发布后权限拒绝是否异常增加
    ///
    /// 注意：
    /// - 同样建议只在一个稳定入口计数（例如 JwtBearerEvents.OnForbidden）。
    /// </summary>
    public static readonly Counter AuthAccessForbiddenTotal = Metrics.CreateCounter(
        name: "auth_access_forbidden_total",
        help: "Count of forbidden (403) access events.");

    /// <summary>
    /// 归一化 Prometheus 查询失败原因
    ///
    /// 背景：
    /// 直接把原始异常文本作为标签值会导致高基数（Cardinality）问题：
    /// - 时序数量暴涨
    /// - Prometheus 内存和查询性能恶化
    ///
    /// 处理策略：
    /// 将各种原始 reason 映射到有限枚举，保证标签可控。
    ///
    /// 输入示例：
    /// - "prometheus_http_error:503"
    /// - "exception:TaskCanceledException"
    /// - "request_canceled"
    /// - "value_parse_failed"
    ///
    /// 输出示例：
    /// - "prometheus_http_error"
    /// - "exception"
    /// - "request_canceled"
    /// - "value_parse_failed"
    /// - "other"
    /// </summary>
    /// <param name="raw">原始失败原因文本</param>
    /// <returns>归一化后的 reason 标签值</returns>
    public static string NormalizePromReason(string? raw)
    {
        // 空值兜底
        if (string.IsNullOrWhiteSpace(raw))
            return "unknown";

        // HTTP 错误归一化（如 500/503/504 统一归类）
        if (raw.StartsWith("prometheus_http_error", StringComparison.OrdinalIgnoreCase))
            return "prometheus_http_error";

        // 异常类型统一归类，避免每种异常消息都成为新标签
        if (raw.StartsWith("exception:", StringComparison.OrdinalIgnoreCase))
            return "exception";

        // 请求取消（客户端取消/超时中断）
        if (raw.Equals("request_canceled", StringComparison.OrdinalIgnoreCase))
            return "request_canceled";

        // 数值解析失败（Prometheus 返回非期望值）
        if (raw.Equals("value_parse_failed", StringComparison.OrdinalIgnoreCase))
            return "value_parse_failed";

        // 其他未知原因
        return "other";
    }
}
