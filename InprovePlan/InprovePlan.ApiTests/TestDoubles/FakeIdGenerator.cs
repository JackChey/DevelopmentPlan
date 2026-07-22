using Instructure.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InprovePlan.ApiTests.TestDoubles;

/// <summary>
/// 浼€犵殑 ID 鐢熸垚鍣ㄥ疄鐜帮紝鐢ㄤ簬鐢熸垚鍒嗗竷寮忔垨娴嬭瘯鐜涓嬬殑鍞竴閫掑 ID銆?
/// 瀹炵幇浜?IIdGenerator 鎺ュ彛锛屾彁渚涚嚎绋嬪畨鍏ㄧ殑 ID 鐢熸垚鏈嶅姟銆?
/// </summary>
public class FakeIdGenerator : IIdGenerator
{
    /// <summary>
    /// 褰撳墠 ID 璁℃暟鍣紝鍒濆鍊间负 100000銆?
    /// 浣跨敤 long 绫诲瀷浠ユ敮鎸佽緝澶х殑 ID 鑼冨洿銆?
    /// </summary>
    private long _current = 100000;

    /// <summary>
    /// 鐢熸垚涓€涓柊鐨勫敮涓€ ID銆?
    /// 閫氳繃鍘熷瓙鎿嶄綔閫掑鍐呴儴璁℃暟鍣紝纭繚鍦ㄥ绾跨▼鐜涓嬬殑绾跨▼瀹夊叏鎬у拰鍞竴鎬с€?
    /// </summary>
    /// <returns>鏂扮敓鎴愮殑鍞竴闀挎暣鍨?ID銆?/returns>
    public long NewId()
    {
        // 浣跨敤 Interlocked.Increment 纭繚瀵?_current 鐨勯€掑鎿嶄綔鏄師瀛愮殑锛?
        // 闃叉澶氱嚎绋嬪苟鍙戣闂椂浜х敓绔炴€佹潯浠讹紝淇濊瘉姣忎釜绾跨▼鑾峰彇鍒扮殑 ID 閮芥槸鍞竴鐨勪笖閫掑鐨勩€?
        return Interlocked.Increment(ref _current);
    }
}


