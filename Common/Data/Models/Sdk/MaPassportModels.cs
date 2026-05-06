namespace KianaBH.Data.Models.Sdk;

public class AppLoginByPasswordRequest
{
    public string? Account { get; set; }
    public string? Password { get; set; }
    public bool IsCrypto { get; set; }
}

public class GetByGameTokenRequest
{
    public string? GameToken { get; set; }
    public string? Token { get; set; }
}

public class LogoutRequest
{
    public string? Token { get; set; }
}

public class AppLoginByPasswordResponse : ResponseBase
{
    public new AppLoginByPasswordResponseData? Data { get; set; }

    public class AppLoginByPasswordResponseData
    {
        public MaPassportTokenData? Token { get; set; }
        public MaPassportUserInfoData? UserInfo { get; set; }
        public MaPassportExtUserInfoData? ExtUserInfo { get; set; }
        public string ReactivateActionTicket { get; set; } = string.Empty;
        public string BindEmailActionTicket { get; set; } = string.Empty;
    }

    public class MaPassportTokenData
    {
        public int TokenType { get; set; }
        public string Token { get; set; } = string.Empty;
    }

    public class MaPassportUserInfoData
    {
        public string Aid { get; set; } = string.Empty;
        public string Mid { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int IsEmailVerify { get; set; }
        public string AreaCode { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string SafeAreaCode { get; set; } = string.Empty;
        public string SafeMobile { get; set; } = string.Empty;
        public string Realname { get; set; } = string.Empty;
        public string IdentityCode { get; set; } = string.Empty;
        public string RebindAreaCode { get; set; } = string.Empty;
        public string RebindMobile { get; set; } = string.Empty;
        public string RebindMobileTime { get; set; } = string.Empty;
        public object[] Links { get; set; } = [];
        public string Country { get; set; } = string.Empty;
        public string PasswordTime { get; set; } = string.Empty;
        public int IsAdult { get; set; }
        public string UnmaskedEmail { get; set; } = string.Empty;
        public int UnmaskedEmailType { get; set; }
    }

    public class MaPassportExtUserInfoData
    {
        public string GuardianEmail { get; set; } = string.Empty;
        public string Birth { get; set; } = string.Empty;
    }
}