using InprovePlan.Domain.Entities;
using Instructure.Interfaces.Jwt;

namespace InprovePlan.ApiTests.TestDoubles;

/// <summary>
/// 浼€犵殑 JWT 鏈嶅姟瀹炵幇锛岀敤浜庢祴璇曟垨寮€鍙戠幆澧冦€?/// 瀹炵幇浜?IJwtService 鎺ュ彛锛屾彁渚涚畝鍖栫殑璁块棶浠ょ墝鑾峰彇閫昏緫锛屼笉杩涜鐪熷疄鐨?JWT 鐢熸垚鎴栭獙璇併€?/// </summary>
internal sealed class FakeJwtService : IJwtService
{
    /// <summary>
    /// 妯℃嫙鐨勮闂护鐗屽瓧绗︿覆銆?    /// 榛樿鍊间负 "test-access-token"锛屽彲鏍规嵁娴嬭瘯闇€姹傝繘琛屼慨鏀广€?    /// </summary>
    public string? AccessToken { get; set; } = "test-access-token";

    /// <summary>
    /// 鑾峰彇鎸囧畾搴旂敤鐢ㄦ埛鐨勮闂护鐗屻€?    /// 鍦ㄤ吉閫犲疄鐜颁腑锛屽拷鐣ョ敤鎴峰弬鏁帮紝鐩存帴杩斿洖棰勮鐨?AccessToken 灞炴€у€笺€?    /// </summary>
    /// <param name="appUser">搴旂敤鐢ㄦ埛瀵硅薄锛屽湪姝ゅ疄鐜颁腑鏈浣跨敤銆?/param>
    /// <returns>棰勮鐨勬ā鎷熻闂护鐗屽瓧绗︿覆銆?/returns>
    public string? GetAccessToken(AppUser appUser)
    {
        return AccessToken;
    }
}


