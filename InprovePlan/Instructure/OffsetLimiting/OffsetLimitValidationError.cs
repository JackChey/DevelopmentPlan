namespace Instructure.OffsetLimiting;

/// <summary>
/// Offset/Limit 参数校验错误。
///
/// 设计目的：
/// 1. 与 Pagination 的 PagingValidationError 分离，避免语义混用。
/// 2. 让日志、响应、测试能明确识别错误来自 Offset/Limit 查询模型。
/// 3. 后续如果 Offset/Limit 增加更多规则，不影响标准分页模型。
/// </summary>
/// <param name="Field">发生错误的字段名，例如 Offset、Limit。</param>
/// <param name="Message">面向调用方的错误说明。</param>
public sealed record OffsetLimitValidationError(
    string Field,
    string Message);