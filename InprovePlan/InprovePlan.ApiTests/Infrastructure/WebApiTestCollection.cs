namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// API 娴嬭瘯闆嗗悎銆?
/// 鎵€鏈?API 娴嬭瘯绫诲叡浜悓涓€涓?CustomWebApplicationFactory锛?
/// 浠庤€屽鐢?MySQL / Redis 瀹瑰櫒鍜屾祴璇曠増 WebHost銆?
/// 姣忎釜娴嬭瘯鏂规硶寮€濮嬪墠閫氳繃 ResetDatabaseAsync 娓呯悊鏁版嵁銆?
/// </summary>
[CollectionDefinition(Name)]
public sealed class WebApiTestCollection
    : ICollectionFixture<CustomWebApplicationFactory>
{
    /// <summary>
    /// 娴嬭瘯闆嗗悎鐨勫敮涓€鍚嶇О鏍囪瘑銆?
    /// 鍦ㄦ祴璇曠被涓婁娇鐢?[Collection(Name)] 鐗规€ф椂锛岄渶寮曠敤姝ゅ父閲忎互鍔犲叆璇ラ泦鍚堛€?
    /// </summary>
    public const string Name = "webapi-integration-tests";
}


