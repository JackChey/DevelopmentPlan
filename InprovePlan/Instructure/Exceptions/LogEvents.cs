namespace Instructure.Exceptions;

/// <summary>
/// 日志事件名称字典（固定事件名中心）
///
/// 设计目的：
/// 1) 统一 event 字段取值，避免手写字符串导致拼写不一致。
/// 2) 让 Prometheus/ELK/Seq/Grafana 查询口径稳定。
/// 3) 便于跨团队协作，日志可读性和可维护性更好。
///
/// 命名规范建议：
/// - 全小写
/// - 点分层（domain.object.action）
/// - 语义明确、尽量短
/// - 结果态优先（started/completed/failed）
///
/// 例如：
/// - http.request.started
/// - auth.access.unauthorized
/// - biz.validation.failed
/// - infra.db.failed
/// </summary>
public static class LogEvents
{
    // =========================
    // HTTP 请求生命周期事件
    // =========================

    /// <summary>
    /// 请求开始（进入请求管道）
    /// 常用于统计入口流量与链路起点
    /// </summary>
    public const string HttpRequestStarted = "http.request.started";

    /// <summary>
    /// 请求完成（正常返回）
    /// 常用于统计成功请求、耗时分位数、接口 SLA
    /// </summary>
    public const string HttpRequestCompleted = "http.request.completed";

    /// <summary>
    /// 请求失败（发生未处理异常或明确失败）
    /// 常用于错误率统计和异常追踪
    /// </summary>
    public const string HttpRequestFailed = "http.request.failed";


    // =========================
    // 鉴权/授权事件
    // =========================

    /// <summary>
    /// 未认证（通常对应 401）
    /// 场景：缺 token、token 过期、签名不合法
    /// </summary>
    public const string AuthUnauthorized = "auth.access.unauthorized";

    /// <summary>
    /// 已认证但无权限（通常对应 403）
    /// 场景：角色不足、策略不通过
    /// </summary>
    public const string AuthForbidden = "auth.access.forbidden";


    // =========================
    // 业务层（可预期失败）事件
    // =========================

    /// <summary>
    /// 业务校验失败（参数合法但不满足业务规则）
    /// 场景：用户名违规、库存不足、状态不允许
    /// </summary>
    public const string BizValidationFailed = "biz.validation.failed";

    /// <summary>
    /// 业务冲突（常映射 409）
    /// 场景：重复提交、并发版本冲突
    /// </summary>
    public const string BizConflict = "biz.conflict";

    // =========================
    // 基础设施层事件
    // =========================

    /// <summary>
    /// 数据库调用失败
    /// 场景：连接失败、超时、SQL 异常
    /// </summary>
    public const string InfraDbFailed = "infra.db.failed";

    /// <summary>
    /// 外部 HTTP 依赖失败
    /// 场景：下游服务 5xx、超时、网络异常
    /// </summary>
    public const string InfraHttpFailed = "infra.http.failed";

    /// <summary>
    /// 系统抛出异常但是被处理
    /// </summary>
    public const string ExceptionHandled = "exception.handled";

   

    /// <summary>
    /// 系统抛出异常但是未被处理
    /// </summary>
    public const string ExceptionUnhandled = "exception.unhandled";

    /// <summary>
    /// 未命中缓存
    /// </summary>
    public const string CacheMiss = "cache.miss";

    /// <summary>
    /// 写入缓存
    /// </summary>
    public const string CacheSet = "cache.set";

    /// <summary>
    /// 命中缓存
    /// </summary>
    public const string CacheHit = "cache.hit";

    /// <summary>
    /// 缓存加载完成
    /// </summary>
    public const string CacheLoaded = "cache.loaded";

    /// <summary>
    /// 缓存失败
    /// </summary>
    public const string CacheFailed = "cache.failed";

    /// <summary>
    /// 缓存删除
    /// </summary>
    public const string CacheRemove = "cache.remove";

    /// <summary>
    /// 返回旧缓存
    /// </summary>
    public const string CacheFailSafe = "cache.failsafe";

    /// <summary>
    /// L1缓存命中
    /// </summary>
    public const string CacheMemoryHit = "cache.memory.hit";

    /// <summary>
    /// L1缓存缺失
    /// </summary>
    public const string CacheMemoryMiss = "cache.memory.miss";

    /// <summary>
    /// L2缓存命中
    /// </summary>
    public const string CacheDistributeHit = "cache.distribute.hit";

    /// <summary>
    /// L2缓存缺失
    /// </summary>
    public const string CacheDistributeMiss = "cache.distribute.miss";
}