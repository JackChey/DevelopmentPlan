using Instructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InprovePlan.ApiTests.Infrastructure;

/// <summary>
/// 娴嬭瘯鏁版嵁搴撲笂涓嬫枃宸ュ巶绫伙紝鐢ㄤ簬鍦ㄦ祴璇曠幆澧冧腑鍒涘缓 AppDbContext 瀹炰緥銆?
/// 璇ョ被涓洪潤鎬佸唴閮ㄧ被锛屾彁渚涚粺涓€鐨勬柟娉曟潵閰嶇疆鍜屽垵濮嬪寲 DbContext锛屾敮鎸佸姩鎬佹敞鍏ユ嫤鎴櫒浠ュ寮烘祴璇曠伒娲绘€с€?
/// </summary>
internal static class TestDbContextFactory
{
    /// <summary>
    /// 鍒涘缓骞惰繑鍥炰竴涓厤缃ソ鐨?AppDbContext 瀹炰緥銆?
    /// 璇ユ柟娉曚娇鐢ㄦ寚瀹氱殑杩炴帴瀛楃涓查厤缃?MySQL 鏁版嵁搴撴彁渚涜€咃紝骞跺彲閫夊湴娣诲姞 EF Core 鎷︽埅鍣ㄣ€?
    /// 
    /// 涓昏鐢ㄩ€旓細
    /// 1. 鍦ㄩ泦鎴愭祴璇曚腑蹇€熸瀯寤烘寚鍚戞祴璇曟暟鎹簱鐨勪笂涓嬫枃銆?
    /// 2. 閫氳繃 interceptors 鍙傛暟娉ㄥ叆鑷畾涔夋嫤鎴櫒锛堝鏃ュ織璁板綍銆佹€ц兘鐩戞帶鎴栨暟鎹慨鏀规嫤鎴級锛屼互渚垮湪娴嬭瘯涓獙璇佺壒瀹氳涓恒€?
    /// 
    /// 閰嶇疆缁嗚妭锛?
    /// - 浣跨敤 UseMySql 鎵╁睍鏂规硶閰嶇疆鏁版嵁搴撴彁渚涜€呫€?
    /// - 浣跨敤 ServerVersion.AutoDetect 鑷姩妫€娴?MySQL 鏈嶅姟鍣ㄧ増鏈紝纭繚鍏煎鎬с€?
    /// - 濡傛灉鎻愪緵浜嗘嫤鎴櫒鏁扮粍锛屽垯灏嗗叾娣诲姞鍒?DbContext 閫夐」涓€?
    /// </summary>
    /// <param name="connectionString">
    /// 鏁版嵁搴撹繛鎺ュ瓧绗︿覆锛岄€氬父鐢辨祴璇曞す鍏凤紙濡?MySqlTestFixture锛夋彁渚涳紝鎸囧悜 Docker 瀹瑰櫒涓殑娴嬭瘯鏁版嵁搴撱€?
    /// </param>
    /// <param name="interceptors">
    /// 鍙€夌殑 EF Core 鎷︽埅鍣ㄦ暟缁勩€?
    /// 杩欎簺鎷︽埅鍣ㄥ皢鍦?DbContext 鐢熷懡鍛ㄦ湡涓粙鍏ュ悇绉嶄簨浠讹紙濡傚懡浠ゆ墽琛屻€佷繚瀛樻洿鏀圭瓑锛夈€?
    /// 鑻ユ湭鎻愪緵鎴栨暟缁勪负绌猴紝鍒欎笉娣诲姞浠讳綍鎷︽埅鍣ㄣ€?
    /// </param>
    /// <returns>
    /// 杩斿洖涓€涓凡閰嶇疆濂芥暟鎹簱杩炴帴鍜屽彲閫夋嫤鎴櫒鐨?AppDbContext 瀹炰緥銆?
    /// </returns>
    public static AppDbContext Create(
        string connectionString,
        params IInterceptor[] interceptors)
    {
        // 鍒涘缓 DbContextOptionsBuilder 瀹炰緥锛岀敤浜庨厤缃?AppDbContext 鐨勯€夐」
        var builder = new DbContextOptionsBuilder<AppDbContext>()
            // 閰嶇疆浣跨敤 MySQL 鏁版嵁搴撴彁渚涜€?
            // ServerVersion.AutoDetect 浼氭牴鎹繛鎺ュ瓧绗︿覆鑷姩鎺ㄦ柇 MySQL 鐗堟湰锛岄伩鍏嶇‖缂栫爜鐗堟湰鍙?
            .UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        // 妫€鏌ユ槸鍚︽彁渚涗簡鎷︽埅鍣?
        if (interceptors.Length > 0)
        {
            // 灏嗘彁渚涚殑鎷︽埅鍣ㄦ坊鍔犲埌 DbContext 閰嶇疆涓?
            // 鎷︽埅鍣ㄥ彲鐢ㄤ簬鐩戝惉 SQL 鎵ц銆佷慨鏀瑰疄浣撶姸鎬佹垨杩涜鍏朵粬妯垏鍏虫敞鐐圭殑澶勭悊
            builder.AddInterceptors(interceptors);
        }

        // 浣跨敤閰嶇疆濂界殑閫夐」鍒涘缓骞惰繑鍥?AppDbContext 瀹炰緥
        return new AppDbContext(builder.Options);
    }
}


