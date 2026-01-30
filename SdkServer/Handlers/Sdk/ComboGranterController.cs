using Microsoft.AspNetCore.Mvc;
using KianaBH.Data.Models.Sdk;
using KianaBH.Database.Account;
using KianaBH.Util;

namespace KianaBH.SdkServer.Handlers.Sdk;

[ApiController]
public class ComboGranterController : Controller
{
    [HttpPost("/{productName}/combo/granter/login/v2/login")]
    public IActionResult ComboLoginV2(string productName, [FromBody] ComboGranterRequest request)
    {
        // TODO: Reuse this logic with MDK Controller Verify Token

        int accountUid;
        try
        {
            accountUid = int.Parse(request.Data?.Uid!);
        }
        catch
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account token error"
            });
        }

        var account = AccountData.GetAccountByUid(accountUid,true);

        if (account == null || account!.ComboToken != request.Data!.Token)
        {
            return Ok(new ResponseBase
            {
                Retcode = -101,
                Success = false,
                Message = "Account token error"
            });
        }

        return Ok(new ComboGranterResponse
        {
            Data = new ComboGranterResponse.ComboGranterResponseData
            {
                AccountType = 1,
                Data = "{\"guest\": false}",
                Heartbeat = false,
                OpenId = account!.Uid.ToString(),
                ComboToken = account!.ComboToken,
            },
        });
    }

    [HttpPost("/{productName}/combo/granter/api/compareProtocolVersion")]
    public IActionResult CompareProtocolVersion(string productName)
    {
        return Ok(new ResponseBase
        {
            Data = new
            {
                Modified = false,
            }
        });
    }

    [HttpGet("/{productName}/combo/granter/api/getConfig")]
    public IActionResult GetConfig()
    {
        return Ok(new ResponseBase
        {
            Data = new
            {
                protocol = true,
                qr_enabled = false,
                log_level = "INFO",
                announce_url =
                    $"{ConfigManager.Config.HttpServer.GetDisplayAddress()}/announcement/index.html",
                push_alias_type = 2,
                disable_ysdk_guard = false,
                enable_announce_popup = false,
                app_name = "崩坏3-东南亚",
                qr_enabled_apps = new
                {
                    bbs = false,
                    cloud = false
                },
                qr_app_icons = new
                {
                    app = "",
                    bbs = "",
                    cloud = "",
                },
                qr_cloud_display_name = "",
                enable_user_center = false,
                functional_switch_configs = new { }
            }
        });
    }

    [HttpGet("/combo/box/api/config/sdk/combo")]
    public IActionResult GetComboConfig()
    {
        return Ok(new ResponseBase
        {
            Data = new
            {
                vals = new
                {
                    network_report_config =
                        "{ \"enable\": 1, \"status_codes\": [206], \"url_paths\": [\"dataUpload\", \"red_dot\"] }",
                    list_price_tierv2_enable = "false",
                    default_os_pay_dialog_type = "old",
                    kibana_pc_config = "{ \"enable\": 1, \"level\": \"Info\",\"modules\": [\"download\"]\n",
                    telemetry_config = "{\n \"dataupload_enable\": 1,\n}",
                    h5log_filter_config =
                        "{\n\t\"function\": {\n\t\t\"event_name\": [\"info_get_cps\", \"notice_close_notice\", \"info_get_uapc\", \"report_set_info\", \"info_get_channel_id\", \"info_get_sub_channel_id\"]\n\t}\n}",
                }
            }
        });
    }
}