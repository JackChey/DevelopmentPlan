namespace Instructure.Idempotency;

/// <summary>
/// 请求 Hash 生成器。
/// 
/// 用于判断同一个 Idempotency-Key 是否被复用于不同请求参数。
/// </summary>
public interface IRequestHashProvider
{
    string ComputeHash(RequestHashSource source);
}


