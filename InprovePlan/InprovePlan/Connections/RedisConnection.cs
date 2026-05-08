namespace InprovePlan.Connections
{
    /// <summary>
    /// 
    /// </summary>
    public class RedisConnection
    {
        /// <summary>
        /// 
        /// </summary>
        public string server { get; set; } = null!;

        /// <summary>
        /// 
        /// </summary>
        public int port { get; set; } 

        /// <summary>
        /// 
        /// </summary>
        public string password { get; set; } = null!;
    }
}
