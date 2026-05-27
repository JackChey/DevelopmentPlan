using InprovePlan.Model;

namespace InprovePlan.FakeData
{
    /// <summary>
    /// 虚拟用户数据
    /// </summary>
    public class Users
    {
        /// <summary>
        /// 
        /// </summary>
        public static List<AppUser> _users = new()
        {
            new AppUser()
            {
                UserId = 100001,
                UserName = "Jack",
                PassWord = "123456",
                Address = "China",
                Root = "Admin",
            },
            new AppUser()
            {
                UserId = 100002,
                UserName = "Json",
                PassWord = "123456",
                Address = "Singaple",
                Root = "User",
            },
            new AppUser()
            {
                UserId = 100003,
                UserName = "Mary",
                PassWord = "123456",
                Address = "Jepan",
                Root = "User",
            },
        };
    }
}
