using Instructure.IResult;

namespace InprovePlan.ApiTests.TestDoubles;

/// <summary>
/// 浼€犵殑瀵嗙爜鍝堝笇鍣ㄥ疄鐜帮紝鐢ㄤ簬娴嬭瘯鎴栧紑鍙戠幆澧冦€?
/// 瀹炵幇浜?IPasswordHasher 鎺ュ彛锛屾彁渚涚畝鍖栫殑瀵嗙爜鍝堝笇鍜岄獙璇侀€昏緫锛屼笉浣跨敤鐪熷疄鐨勫姞瀵嗙畻娉曘€?
/// </summary>
internal class FakePasswordHasher : IPasswordHasher
{
    /// <summary>
    /// 瀵规槑鏂囧瘑鐮佽繘琛屸€滃搱甯屸€濆鐞嗐€?
    /// 鍦ㄤ吉閫犲疄鐜颁腑锛屼粎绠€鍗曞湴鍦ㄥ瘑鐮佸墠娣诲姞 "Hash:" 鍓嶇紑锛屼笉杩涜浠讳綍瀹為檯鐨勫姞瀵嗘垨鍔犵洂鎿嶄綔銆?
    /// </summary>
    /// <param name="password">闇€瑕佸搱甯屽鐞嗙殑鏄庢枃瀵嗙爜銆?/param>
    /// <returns>妯℃嫙鐨勫搱甯屽瓧绗︿覆锛屾牸寮忎负 "Hash:{password}"銆?/returns>
    public string Hash(string password)
    {
        return $"Hash:{password}";
    }

    /// <summary>
    /// 楠岃瘉鎻愪緵鐨勫瘑鐮佹槸鍚︿笌瀛樺偍鐨勫搱甯屽€煎尮閰嶃€?
    /// 閫氳繃閲嶆柊璁＄畻杈撳叆瀵嗙爜鐨勬ā鎷熷搱甯屽€硷紝骞朵笌瀛樺偍鐨勫搱甯屽€艰繘琛屽瓧绗︿覆姣旇緝鏉ュ畬鎴愰獙璇併€?
    /// </summary>
    /// <param name="passwordHash">瀛樺偍鍦ㄦ暟鎹簱鎴栫郴缁熶腑鐨勫搱甯屽瘑鐮佸瓧绗︿覆銆?/param>
    /// <param name="password">鐢ㄦ埛杈撳叆鐨勫緟楠岃瘉鏄庢枃瀵嗙爜銆?/param>
    /// <returns>濡傛灉瀵嗙爜鍖归厤鍒欒繑鍥?PasswordVerifyResult.Success锛屽惁鍒欒繑鍥?PasswordVerifyResult.Failed銆?/returns>
    public PasswordVerifyResult Verify(string passwordHash, string password)
    {
        // 閲嶆柊璁＄畻杈撳叆瀵嗙爜鐨勬ā鎷熷搱甯屽€硷紝骞朵笌瀛樺偍鐨勫搱甯屽€艰繘琛屾瘮杈?
        return passwordHash == Hash(password)
            ? PasswordVerifyResult.Success
            : PasswordVerifyResult.Failed;
    }
}


