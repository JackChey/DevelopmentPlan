namespace InprovePlan.Service.Jwt
{
    /// <summary>
    /// Jwt设置信息
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// Token过期时间(min)
        /// </summary>
        public int AccessTokenExpirationMinutes { get; set; } 

        /// <summary>
        /// 秘钥
        /// </summary>
        public string Secret { get; set; } = null!;

        /// <summary>
        /// 发送者
        /// </summary>
        public string Issuer { get; set; } = null!;

        /// <summary>
        /// 接受者
        /// </summary>
        public string Audience { get; set; } = null!;
    }
}
