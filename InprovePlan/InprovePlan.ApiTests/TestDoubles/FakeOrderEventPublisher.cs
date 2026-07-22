using InprovePlan.ShareKernel.Contracts;
using InprovePlan.ShareKernel.Messaging;

namespace InprovePlan.ApiTests.TestDoubles;

/// <summary>
/// 浼€犵殑璁㈠崟浜嬩欢鍙戝竷鍣ㄥ疄鐜帮紝鐢ㄤ簬娴嬭瘯鎴栧紑鍙戠幆澧冦€?/// 瀹炵幇浜?IOrderEventPublisher 鎺ュ彛锛屼笉瀹為檯鍙戦€佹秷鎭埌娑堟伅闃熷垪锛岃€屾槸灏嗗彂甯冪殑浜嬩欢瀛樺偍鍦ㄥ唴瀛樺垪琛ㄤ腑锛屼互渚垮悗缁獙璇併€?/// </summary>
internal sealed class FakeOrderEventPublisher : IOrderEventPublisher
{
    /// <summary>
    /// 鍐呴儴瀛樺偍宸插彂甯冧簨浠剁殑鍒楄〃銆?    /// 鐢ㄤ簬璁板綍鎵€鏈夐€氳繃 PublishOrderStatusChangedAsync 鏂规硶鍙戝竷鐨勪簨浠躲€?    /// </summary>
    private readonly List<OrderStatusChangedEvent> _events = [];

    /// <summary>
    /// 鑾峰彇宸插彂甯冧簨浠剁殑鍙鍒楄〃銆?    /// 鍏佽澶栭儴娴嬭瘯浠ｇ爜妫€鏌ュ凡鍙戝竷鐨勪簨浠跺唴瀹癸紝浣嗛槻姝㈢洿鎺ヤ慨鏀瑰唴閮ㄩ泦鍚堛€?    /// </summary>
    public IReadOnlyList<OrderStatusChangedEvent> Events => _events;

    /// <summary>
    /// 鍙戝竷璁㈠崟鐘舵€佸彉鏇翠簨浠躲€?    /// 鍦ㄤ吉閫犲疄鐜颁腑锛屼粎灏嗕簨浠舵坊鍔犲埌鍐呴儴鍒楄〃锛屽苟绔嬪嵆杩斿洖宸插畬鎴愮殑浠诲姟锛屾ā鎷熷紓姝ユ搷浣滀絾涓嶆墽琛屽疄闄呯殑 network I/O銆?    /// </summary>
    /// <param name="event">瑕佸彂甯冪殑璁㈠崟鐘舵€佸彉鏇翠簨浠跺璞°€?/param>
    /// <param name="cancellationToken">鍙栨秷浠ょ墝锛屽湪姝ゅ疄鐜颁腑鏈浣跨敤锛屽洜涓烘搷浣滄槸鍚屾涓旂灛鏃剁殑銆?/param>
    /// <returns>涓€涓〃绀烘搷浣滃凡瀹屾垚鐨勪换鍔°€?/returns>
    public Task PublishOrderStatusChangedAsync(
        OrderStatusChangedEvent @event,
        CancellationToken cancellationToken = default)
    {
        // 灏嗕簨浠舵坊鍔犲埌鍐呴儴鍒楄〃涓互渚涘悗缁柇瑷€鎴栭獙璇?        _events.Add(@event);

        // 杩斿洖宸插畬鎴愮殑浠诲姟锛岀鍚堝紓姝ユ柟娉曠鍚嶈姹傦紝浣嗘棤闇€鐪熸寮傛鎵ц
        return Task.CompletedTask;
    }
}


