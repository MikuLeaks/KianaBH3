using KianaBH.Data.Models.Sdk;
using KianaBH.Database;
using KianaBH.Database.Account;
using KianaBH.Util;
using Microsoft.AspNetCore.Mvc;

namespace KianaBH.SdkServer.Handlers.Sdk;

[ApiController]
public class MaPassportController : ControllerBase
{
    [HttpPost("/{productName}/account/ma-passport/api/appLoginByPassword")]
    public IActionResult AppLoginByPassword(string productName,
        [FromBody] AppLoginByPasswordRequest request)
    {
        Logger.GetByClassName().Debug($"ma-passport login request: account={request.Account ?? "<null>"}, password={request.Password ?? "<null>"}, is_crypto={request.IsCrypto}");

        //For some reason these are read only despite having setters so copy to new variables
        var username = request.Account;
        var password = request.Password;
        if (true)
        {
            username = "username";
            password = "0";
            Logger.GetByClassName().Info("ma-passport decryption is not implemented. Your username is \"username\" and your password is 0.");
        }
        Logger.GetByClassName().Debug("Checking account with username " + username);
        var account = AccountData.GetAccountByUserName(username ?? string.Empty);

        if (account == null && !ConfigManager.Config.ServerOption.AutoCreateUser)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account not found"
            });
        }

        if (account == null)
        {
            AccountData.CreateAccount(username ?? string.Empty, 0, password ?? string.Empty);
            account = AccountData.GetAccountByUserName(username ?? string.Empty);
        }

        if (account == null)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Failed to create account"
            });
        }

        if (!string.IsNullOrEmpty(account.Password) && !AccountData.VerifyPassword(account, password ?? string.Empty))
        {
            return Ok(new ResponseBase
            {
                Retcode = -201,
                Success = false,
                Message = "Incorrect account or password"
            });
        }

        return Ok(CreateLoginResponse(account));
    }

    [HttpPost("/{productName}/account/ma-passport/token/getByGameToken")]
    public IActionResult GetByGameToken(string productName, [FromBody] GetByGameTokenRequest request)
    {
        var token = request.GameToken ?? request.Token;
        if (string.IsNullOrWhiteSpace(token))
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Missing game token"
            });
        }

        var account = DatabaseHelper.GetAllInstance<AccountData>()?.FirstOrDefault(candidate => candidate.ComboToken == token);
        if (account == null)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "For account safety, please log in again"
            });
        }

        return Ok(CreateLoginResponse(account, token));
    }

    [HttpPost("/{productName}/account/ma-passport/api/logout")]
    public IActionResult Logout(string productName, [FromBody] LogoutRequest request)
    {
        return Ok(new ResponseBase());
    }

    private static AppLoginByPasswordResponse CreateLoginResponse(AccountData account, string? token = null)
    {
        return new AppLoginByPasswordResponse
        {
            Data = new AppLoginByPasswordResponse.AppLoginByPasswordResponseData
            {
                Token = new AppLoginByPasswordResponse.MaPassportTokenData
                {
                    TokenType = 1,
                    Token = token ?? account.GenerateComboToken()
                },
                UserInfo = new AppLoginByPasswordResponse.MaPassportUserInfoData
                {
                    Aid = account.Uid.ToString(),
                    Mid = string.Empty,
                    AccountName = account.Username,
                    Email = $"{account.Username}@neonteam.dev",
                    IsEmailVerify = 0,
                    AreaCode = "**",
                    Mobile = string.Empty,
                    SafeAreaCode = string.Empty,
                    SafeMobile = string.Empty,
                    Realname = string.Empty,
                    IdentityCode = string.Empty,
                    RebindAreaCode = string.Empty,
                    RebindMobile = string.Empty,
                    RebindMobileTime = "315532800",
                    Links = [],
                    Country = "US",
                    PasswordTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                    IsAdult = 0,
                    UnmaskedEmail = string.Empty,
                    UnmaskedEmailType = 0
                },
                ExtUserInfo = new AppLoginByPasswordResponse.MaPassportExtUserInfoData
                {
                    GuardianEmail = string.Empty,
                    Birth = "0"
                },
                ReactivateActionTicket = string.Empty,
                BindEmailActionTicket = string.Empty
            }
        };
    }
}

