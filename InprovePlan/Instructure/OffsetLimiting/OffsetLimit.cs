namespace Instructure.OffsetLimiting;

/// <summary>
/// Offset/Limit 查询模型。
///
/// 与 Pagination 的区别：
/// - Pagination 表达“第几页 + 每页多少条”。
/// - OffsetLimit 表达“跳过多少条 + 取多少条”。
///
/// 适用场景：
/// 1. 精确区间读取，例如“取第 100 到 200 条”。
/// 2. 游标、批处理、后台任务等不天然按页展示的查询。
/// 3. 需要直接映射数据库 Skip/Take 的场景。
///
/// 生产约束：
/// 1. Offset 从 0 开始。
/// 2. Limit 必须大于 0。
/// 3. Limit 必须有最大值限制，防止一次读取过多数据。
/// 4. Offset/Limit 只描述取数窗口，不负责排序。
/// 5. 使用 OffsetLimit 时必须搭配稳定排序，否则多次查询可能出现重复或漏数据。
/// </summary>
public sealed class OffsetLimit
{
    /// <summary>
    /// 默认偏移量。
    ///
    /// Offset 从 0 开始：
    /// - Offset = 0 表示从第 1 条记录开始取。
    /// - Offset = 99 表示跳过前 99 条，从第 100 条记录开始取。
    /// </summary>
    public const int DefaultOffset = 0;

    /// <summary>
    /// 默认取数条数。
    ///
    /// 当调用方没有显式传入 Limit 时使用。
    /// </summary>
    public const int DefaultLimit = 20;

    /// <summary>
    /// 最大取数条数。
    ///
    /// 生产环境必须限制 Limit：
    /// - 防止一次请求拉取大量数据。
    /// - 防止数据库排序、扫描、网络传输压力过高。
    /// - 防止接口响应体过大。
    /// </summary>
    public const int MaxLimit = 100;

    /// <summary>
    /// 需要跳过的记录数。
    ///
    /// 约束：
    /// - 必须大于等于 0。
    /// - 不允许负数。
    ///
    /// 示例：
    /// - Offset = 0, Limit = 10：取第 1 到第 10 条。
    /// - Offset = 99, Limit = 101：取第 100 到第 200 条。
    /// </summary>
    public int Offset { get; init; } = DefaultOffset;

    /// <summary>
    /// 需要取出的记录数。
    ///
    /// 约束：
    /// - 必须大于等于 1。
    /// - 必须小于等于 MaxLimit。
    /// </summary>
    public int Limit { get; init; } = DefaultLimit;

    /// <summary>
    /// 校验 Offset/Limit 参数。
    ///
    /// 返回错误集合而不是直接抛异常：
    /// - 方便 Controller / UseCase 汇总错误。
    /// - 方便测试断言具体字段。
    /// - 方便统一转换为 ApiResponse。
    /// </summary>
    /// <returns>参数错误集合；如果为空，表示参数合法。</returns>
    public IReadOnlyList<OffsetLimitValidationError> Validate()
    {
        var errors = new List<OffsetLimitValidationError>();

        if (Offset < 0)
        {
            errors.Add(new OffsetLimitValidationError(
                Field: nameof(Offset),
                Message: "offset 必须大于等于 0。"));
        }

        if (Limit < 1)
        {
            errors.Add(new OffsetLimitValidationError(
                Field: nameof(Limit),
                Message: "limit 必须大于等于 1。"));
        }

        if (Limit > MaxLimit)
        {
            errors.Add(new OffsetLimitValidationError(
                Field: nameof(Limit),
                Message: $"limit 不能大于 {MaxLimit}。"));
        }

        return errors;
    }

    /// <summary>
    /// 当前 Offset/Limit 参数是否有效。
    ///
    /// true：没有校验错误。
    /// false：至少存在一个校验错误。
    /// </summary>
    public bool IsValid => Validate().Count == 0;
}
