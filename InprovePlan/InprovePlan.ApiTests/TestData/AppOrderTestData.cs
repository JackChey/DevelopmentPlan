using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.TestData;

/// <summary>
/// 璁㈠崟娴嬭瘯鏁版嵁甯搁噺绫汇€?
/// 姝ょ被瀹氫箟浜嗙敤浜庡崟鍏冩祴璇曞拰闆嗘垚娴嬭瘯鐨勬爣鍑嗗寲璁㈠崟鏁版嵁甯搁噺锛岀‘淇濇祴璇曠敤渚嬩箣闂存暟鎹殑涓€鑷存€у拰鍙淮鎶ゆ€с€?
/// 
/// 涓昏鐢ㄩ€旓細
/// 1. 涓?Order 瀹炰綋鍙婂叾鐩稿叧 DTO 鎻愪緵鏈夋晥鐨勯粯璁ゅ€笺€?
/// 2. 鍦?Arrange 闃舵蹇€熸瀯寤烘祴璇曞璞★紝閬垮厤纭紪鐮侀瓟娉曟暟瀛楁垨瀛楃涓层€?
/// 3. 浣滀负鏂█闃舵鐨勯鏈熷€煎弬鑰冿紝鎻愰珮娴嬭瘯浠ｇ爜鐨勫彲璇绘€с€?
/// 
/// 娉ㄦ剰锛?
/// - 鎵€鏈夊瓧娈靛潎涓?const锛岀紪璇戞椂纭畾锛屾€ц兘鏈€浼樸€?
/// - 鏁版嵁绫诲瀷涓庨鍩熸ā鍨嬶紙Domain Model锛変腑鐨勫畾涔変弗鏍煎尮閰嶃€?
/// </summary>
public class AppOrderTestData
{
    /// <summary>
    /// 鏈夋晥鐨勮鍗?ID銆?
    /// 鐢ㄤ簬妯℃嫙宸叉寔涔呭寲鐨勮鍗曚富閿紝閫氬父鐢ㄤ簬鏌ヨ鎴栨洿鏂版搷浣滄祴璇曘€?
    /// </summary>
    public const long ValidOrderId = 100234567;

    /// <summary>
    /// 鏈夋晥鐨勮鍗曠紪鍙枫€?
    /// 涓氬姟灞傞潰鐨勫敮涓€鏍囪瘑绗︼紝閫氬父鐢ㄤ簬澶栭儴灞曠ず鎴栨帴鍙ｄ氦浜掋€?
    /// </summary>
    public const string ValidOrderNo = "NO123456789";

    /// <summary>
    /// 鏈夋晥鐨勫叧鑱斾骇鍝?ID銆?
    /// 鎸囧悜璁㈠崟涓煇涓叿浣撲骇鍝佺殑鏍囪瘑锛岀敤浜庨獙璇佽鍗曢」涓庝骇鍝佺殑鍏宠仈鍏崇郴銆?
    /// </summary>
    public const long ValidProductId = 100001;

    /// <summary>
    /// 鏈夋晥鐨勫叧鑱斾骇鍝佷唬鐮併€?
    /// 鐢ㄤ簬楠岃瘉璁㈠崟涓骇鍝佷唬鐮佺殑姝ｇ‘鎬э紝閫氬父涓?ProductCode 瀛楁瀵瑰簲銆?
    /// </summary>
    public const string ValidProductCode = "ProductCode1001";

    /// <summary>
    /// 鏈夋晥鐨勫叧鑱斾骇鍝佸悕绉般€?
    /// 鐢ㄤ簬楠岃瘉璁㈠崟涓骇鍝佸悕绉扮殑鏄剧ず鎴栧瓨鍌ㄩ€昏緫銆?
    /// </summary>
    public const string ValidProductName = "Production001";

    /// <summary>
    /// 鏈夋晥鐨勮揣甯佺被鍨嬨€?
    /// 瀹氫箟涓?"RMB"锛岀敤浜庢祴璇曡揣甯佸瓧娈电殑鏍煎紡鍖栧拰鏍￠獙閫昏緫銆?
    /// </summary>
    public const string ValidCurrency = "RMB";

    /// <summary>
    /// 鏈夋晥鐨勫晢鍝佸崟浠枫€?
    /// 鍊间负 19.8m锛岀敤浜庢祴璇曚环鏍艰绠椼€佺簿搴﹀鐞嗗強鎬婚噾棰濇帹瀵奸€昏緫銆?
    /// </summary>
    public const decimal ValidUnitPrice = 19.8m;

    /// <summary>
    /// 鏈夋晥鐨勮喘涔版暟閲忋€?
    /// 鍊间负 10锛岀敤浜庢祴璇曟暟閲忔牎楠屽強鎬讳环璁＄畻锛堝崟浠?* 鏁伴噺锛夈€?
    /// </summary>
    public const decimal ValidQuantity = 10;

    /// <summary>
    /// 鏈夋晥鐨勮鍗曠姸鎬併€?
    /// 瀹氫箟涓?Paid锛堝凡鏀粯锛夛紝鐢ㄤ簬娴嬭瘯鐘舵€佹祦杞€佹潈闄愭帶鍒舵垨涓氬姟瑙勫垯鍒嗘敮銆?
    /// </summary>
    public const AppOrderStatus ValidOrderStatus = AppOrderStatus.Paid;

    /// <summary>
    /// 鏈夋晥鐨勫彇娑堟爣璁般€?
    /// 瀹氫箟涓?false锛岃〃绀鸿鍗曟湭琚彇娑堬紝鐢ㄤ簬娴嬭瘯姝ｅ父娴佺▼涓嬬殑璁㈠崟澶勭悊銆?
    /// </summary>
    public const bool ValidCancelled = false;

    /// <summary>
    /// 鏈夋晥鐨勬敹璐у湴鍧€ ID銆?
    /// 鎸囧悜璁㈠崟鍏宠仈鐨勬敹璐у湴鍧€璁板綍锛岀敤浜庢祴璇曞湴鍧€淇℃伅鐨勫姞杞藉拰楠岃瘉銆?
    /// </summary>
    public const long ValidAddressId = 100001;
}


