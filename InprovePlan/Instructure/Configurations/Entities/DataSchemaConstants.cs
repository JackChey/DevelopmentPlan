using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Instructure.Configurations.Entities;

/// <summary>
/// 数据库字段约束常量，统一维护长度、精度等 Schema 规则。
/// </summary>
public static class DataSchemaConstants
{
    public const int PasswordHashLength = 255; // BCrypt/Argon2/PBKDF2 结果建议预留 255。
    public const int UserNameLength = 64; // 用户名长度，避免 varchar 过大。
    public const int PhoneNumberLength = 32; // 手机号/区号/国际号码预留长度。
    public const int EmailLength = 128; // 邮箱长度，生产中一般 128 或 255。

    public const int ProductCodeLength = 64; // 商品业务编码长度。
    public const int ProductNameLength = 128; // 商品名称长度。
    public const int ProductDescriptionLength = 1000; // 商品描述长度。

    public const int OrderNoLength = 64; // 订单业务编号长度。
    public const int CurrencyLength = 3; // ISO 4217 货币编码长度，例如 CNY、USD。
}