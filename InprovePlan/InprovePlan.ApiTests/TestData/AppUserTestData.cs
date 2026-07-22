using InprovePlan.Domain.Entities;

namespace InprovePlan.ApiTests.TestData;

/// <summary>
/// 鐢ㄦ埛娴嬭瘯鏁版嵁甯搁噺绫汇€?
/// 姝ょ被瀹氫箟浜嗙敤浜庡崟鍏冩祴璇曞拰闆嗘垚娴嬭瘯鐨勬爣鍑嗗寲鐢ㄦ埛鏁版嵁甯搁噺锛岀‘淇濇祴璇曠敤渚嬩箣闂存暟鎹殑涓€鑷存€у拰鍙淮鎶ゆ€с€?
/// 
/// 涓昏鐢ㄩ€旓細
/// 1. 涓?AppUser 瀹炰綋鍙婂叾鐩稿叧 DTO锛堝娉ㄥ唽銆佺櫥褰曘€佹洿鏂板懡浠わ級鎻愪緵鏈夋晥鐨勯粯璁ゅ€笺€?
/// 2. 鍦?Arrange 闃舵蹇€熸瀯寤烘祴璇曞璞★紝閬垮厤纭紪鐮侀瓟娉曟暟瀛楁垨瀛楃涓层€?
/// 3. 浣滀负鏂█闃舵鐨勯鏈熷€煎弬鑰冿紝鎻愰珮娴嬭瘯浠ｇ爜鐨勫彲璇绘€с€?
/// 
/// 娉ㄦ剰锛?
/// - 鎵€鏈夊瓧娈靛潎涓?const锛岀紪璇戞椂纭畾锛屾€ц兘鏈€浼樸€?
/// - 鏁版嵁绫诲瀷涓庨鍩熸ā鍨嬶紙Domain Model锛変腑鐨勫畾涔変弗鏍煎尮閰嶃€?
/// - 鏁忔劅淇℃伅锛堝瀵嗙爜銆侀偖绠便€佹墜鏈哄彿锛変粎鐢ㄤ簬娴嬭瘯鐜锛屼弗绂佸湪鐢熶骇浠ｇ爜涓娇鐢ㄧ湡瀹炵敤鎴锋暟鎹€?
/// </summary>
public class AppUserTestData
{
    /// <summary>
    /// 鏈夋晥鐨勭敤鎴?ID銆?
    /// 鐢ㄤ簬妯℃嫙宸叉寔涔呭寲鐨勭敤鎴蜂富閿紝閫氬父鐢ㄤ簬鏌ヨ銆佹洿鏂版垨鍒犻櫎鎿嶄綔娴嬭瘯銆?
    /// </summary>
    public const long ValidUserId = 100001;

    /// <summary>
    /// 鏈夋晥鐨勭敤鎴峰悕銆?
    /// 鐢ㄤ簬娴嬭瘯鐢ㄦ埛鍚嶇О鐨勬樉绀恒€佸瓨鍌ㄥ強鍞竴鎬ф牎楠岄€昏緫銆?
    /// </summary>
    public const string ValidUserName = "Jack";

    /// <summary>
    /// 鏈夋晥鐨勭數瀛愰偖浠跺湴鍧€銆?
    /// 鐢ㄤ簬娴嬭瘯閭鏍煎紡鏍￠獙銆佸敮涓€鎬х害鏉熷強閫氱煡鍙戦€侀€昏緫銆?
    /// </summary>
    public const string ValidEmail = "18273940218@163.com";

    /// <summary>
    /// 鏈夋晥鐨勬墜鏈哄彿鐮併€?
    /// 鐢ㄤ簬娴嬭瘯鎵嬫満鍙锋牸寮忔牎楠屻€佸敮涓€鎬х害鏉熷強鐭俊楠岃瘉閫昏緫銆?
    /// </summary>
    public const string ValidPhoneNumber = "18273940218";

    /// <summary>
    /// 鏈夋晥鐨勬槑鏂囧瘑鐮併€?
    /// 鐢ㄤ簬娴嬭瘯瀵嗙爜鍝堝笇銆佸己搴︽牎楠屽強鐧诲綍楠岃瘉閫昏緫銆?
    /// 娉ㄦ剰锛氬湪瀹為檯瀛樺偍鍓嶅繀椤荤粡杩囧搱甯屽鐞嗭紝姝ゅ浠呬綔涓烘祴璇曡緭鍏ュ€笺€?
    /// </summary>
    public const string ValidPassword = "123456";

    /// <summary>
    /// 鏈夋晥鐨勬€у埆鏋氫妇鍊笺€?
    /// 瀹氫箟涓?Secret锛堜繚瀵嗭級锛岀敤浜庢祴璇曟€у埆瀛楁鐨勯粯璁ゅ€兼垨闅愮淇濇姢閫昏緫銆?
    /// </summary>
    public const AppUserSex ValidSex = AppUserSex.Secret;

    /// <summary>
    /// 鏈夋晥鐨勭敤鎴风姸鎬佹灇涓惧€笺€?
    /// 瀹氫箟涓?Enable锛堝惎鐢級锛岀敤浜庢祴璇曟甯告椿璺冪敤鎴风殑涓氬姟閫昏緫鍒嗘敮銆?
    /// </summary>
    public const AppUserStatus ValidUserStatus = AppUserStatus.Enable;

    /// <summary>
    /// 鏈夋晥鐨勫垹闄ゆ爣璁般€?
    /// 瀹氫箟涓?false锛岃〃绀虹敤鎴锋湭琚€昏緫鍒犻櫎锛岀敤浜庢祴璇曟甯告祦绋嬩笅鐨勭敤鎴锋暟鎹鐞嗐€?
    /// </summary>
    public const bool ValidIsDeleted = false;
}


