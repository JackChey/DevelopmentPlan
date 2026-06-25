using System.Diagnostics;

namespace Instructure.SystemLogs
{
    /// <summary>
    /// 
    /// </summary>
    public class AppLog
    {
        /// <summary>
        /// 日志产生时间（UTC，ISO8601），用于时序分析与对齐多系统时间线
        /// </summary>
        public DateTimeOffset OccurrenceTime { get; set; }

        /// <summary>
        /// 日志级别（Trace/Debug/Information/Warning/Error/Fatal），用于过滤和告警分层
        /// </summary>
        public string Level { get; set; } = string.Empty;

        /// <summary>
        /// 日志信息
        /// </summary>
        public string? Msg { get; set; } = string.Empty;

        /// <summary>
        /// 服务名（微服务/应用名），用于区分日志来源系统
        /// </summary>
        public string Service { get; set; } = string.Empty;

        /// <summary>
        /// 环境标识（development/staging/production），避免跨环境误判
        /// </summary>
        public string Env { get; set; } = string.Empty;

        /// <summary>
        /// 服务版本（如 1.0.0 或 Git SHA），用于定位“哪个版本引入问题”
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// 实例标识（容器名/主机名/Pod ID），用于定位具体故障实例
        /// </summary>
        public string Instance { get; set; } = string.Empty;

        /// <summary>
        /// 分布式追踪主键,贯穿一次请求在多服务中的全链路
        /// </summary>
        public string? TraceId { get; set; }

        /// <summary>
        /// 当前调用片段ID,配合 TraceId 还原链路中的单个步骤
        /// </summary>
        public string? SpanId { get; set; }

    }
}
