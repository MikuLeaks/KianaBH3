using KianaBH.Data.Models.Sdk;
using KianaBH.Database.Account;
using KianaBH.Util;
using Microsoft.AspNetCore.Mvc;

namespace KianaBH.SdkServer.Models.Sdk;

[ApiController]
public class MdkController : Controller
{
    [HttpPost("/{productName}/mdk/shield/api/login")]
    public IActionResult MdkShieldLogin(string productName, [FromBody] MdkShieldLoginRequest request)
    {
        var account = AccountData.GetAccountByUserName(request.Account!);

        if (account == null && !ConfigManager.Config.ServerOption.AutoCreateUser) 
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account not found"
            });
        };

        // Make new account
        if (account == null && ConfigManager.Config.ServerOption.AutoCreateUser)
        {
            AccountData.CreateAccount(request.Account!, 0, request.Password!);

            account = AccountData.GetAccountByUserName(request.Account!)!;
        };

        if (account == null)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account not found"
            });
        }

        return Ok(new MdkShieldResponse
        {
            Data = new MdkShieldResponse.MdkShieldResponseData
            {
                Account = new MdkShieldAccountData
                {
                    Uid = account.Uid.ToString(),
                    Token = account.GenerateComboToken(),
                    Name = account.Username,
                    Realname = account.Username,
                    IsEmailVerify = "0",
                    Email = $"{account.Username}@neonteam.dev",
                    AreaCode = "**",
                    Country = "US",
                },
            }
        });
    }

    [HttpPost("/{productName}/mdk/shield/api/verify")]
    public IActionResult MdkShieldVerify(string productName, [FromBody] MdkShieldVerifyRequest request)
    {
        int accountUid;
        try
        {
            accountUid = int.Parse(request.Uid!);
        }
        catch
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account cache error"
            });
        }

        var account = AccountData.GetAccountByUid(accountUid,true);

        if (account == null)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account cache error"
            });
        }

        if (account.ComboToken != request.Token)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "For account safety, please log in again"
            });
        }

        return Ok(new MdkShieldResponse
        {
            Data = new MdkShieldResponse.MdkShieldResponseData
            {
                Account = new MdkShieldAccountData
                {
                    Uid = account.Uid.ToString(),
                    Token = account.ComboToken!,
                    Name = account.Username,
                    Realname = account.Username,
                    IsEmailVerify = "0",
                    Email = $"{account!.Username}@neonteam.dev",
                    AreaCode = "**",
                    Country = "US",
                },
            }
        });
    }

    [HttpGet("/{productName}/mdk/agreement/api/getAgreementInfos")]
    public IActionResult MdkGetAgreementInfos(string productName)
    {
        return Ok(new ResponseBase
        {
            Data = new { marketing_agreements = Array.Empty<object>() }
        });
    }

    [HttpGet("/{productName}/mdk/shield/api/loadConfig")]
    public IActionResult MdkLoadConfig(string productName)
    {
        return Ok(new ResponseBase
        {
            Data = new
            {
                id = 16,
                game_key = productName,
                client = "PC",
                identity = "I_IDENTITY",
                guest = false,
                ignore_versions = "",
                scene = "S_NORMAL",
                name = "崩坏3rd-东南亚",
                disable_regist = false,
                enable_email_captcha = false,
                thirdparty = Array.Empty<string>(),
                disable_mmt = false,
                server_guest = false,
                thirdparty_ignore = new { },
                enable_ps_bind_account = false,
                thirdparty_login_configs = new { },
                initialize_firebase = false,
                bbs_auth_login = false,
                bbs_auth_login_ignore = Array.Empty<string>(),
                fetch_instance_id = false,
                enable_flash_login = false,
                enable_logo_18 = false,
                logo_height = "0",
                logo_width = "0",
                enable_cx_bind_account = false,
                firebase_blacklist_devices_switch = false,
                firebase_blacklist_devices_version = 0,
                hoyolab_auth_login = false,
                hoyolab_auth_login_ignore = Array.Empty<string>(),
                hoyoplay_auth_login = true,
                enable_douyin_flash_login = false,
                enable_age_gate = false,
                enable_age_gate_ignore = Array.Empty<string>()
            }
        });
    }
}