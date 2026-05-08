namespace InprovePlan.Connections
{
    /// <summary>
    /// 
    /// </summary>
    public class RabbitMqConnection
    {
        /// <summary>
        /// 协议
        /// </summary>
        public string protocol { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public string username { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public string password { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public string host { get; set; } = null!;

        /// <summary>
        /// 端口
        /// </summary>
        public int port { get; set; } 

      
    }
}
