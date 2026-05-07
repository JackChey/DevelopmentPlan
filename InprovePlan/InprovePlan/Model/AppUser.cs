namespace InprovePlan.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class AppUser
    {
        /// <summary>
        /// 用户ID.唯一标识
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 用户密码(后续加密)
        /// </summary>
        public string PassWord { get; set; } = string.Empty;

        /// <summary>
        /// 0-男性,1-女性
        /// </summary>
        public int Sex { get; set; } 

        /// <summary>
        /// 居住地址
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// 权限
        /// </summary>
        public string? Root { get; set; }
    }
}
