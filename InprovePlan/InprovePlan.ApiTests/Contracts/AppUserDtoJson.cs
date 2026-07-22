namespace InprovePlan.ApiTests.Contracts;

public class AppUserDtoJson
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public int Sex { get; set; }
    public int UserStatus { get; set; }
}

