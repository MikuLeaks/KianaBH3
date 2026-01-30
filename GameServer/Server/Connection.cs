using KianaBH.GameServer.Game.Player;
using KianaBH.GameServer.Server.Packet;
using KianaBH.KcpSharp;
using KianaBH.KcpSharp.Base;
using KianaBH.Proto;
using KianaBH.Util;
using KianaBH.Util.Extensions;
using KianaBH.Util.Security;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using System.Buffers;
using System.Net;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;
using System.Net.Sockets;
using System.Reflection.Emit;

namespace KianaBH.GameServer.Server;

public class Connection(KcpConversation conversation, IPEndPoint remote) : KcpConnection(conversation, remote)
{
    private static readonly Logger Logger = new("GameServer");

    public PlayerInstance? Player { get; set; }

    private static readonly HashSet<string> DummyPacketNames =
    [
        "AddGoodfeelReq", "ArkPlusActivityGetDataReq", "BuffAssistGetActivityReq", "BwWorldCampActivityGetDataReq",
        "ChatworldBeastGetActivityReq", "ChatworldGetPrayInfoReq", "ClientReportReq", "GetAdventureGroupReq",
        "GetArmadaDataReq", "GetArmadaStageScoreActivityReq", "GetAskAddFriendListReq", "GetAssistantFrozenListReq",
        "GetAvatarMissionActivityReq", "GetBattlePassMissionPanelReq", "GetBlackListReq", "GetCardProductInfoReq",
        "GetChapterActivityDataReq", "GetChapterCompensationInfoReq", "GetChatgroupListReq", "GetClientMailDataReq",
        "GetConsignedOrderDataReq", "GetCurrencyExchangeInfoReq", "GetExtractReforgeActivityReq",
        "GetFarmActivityDataReq", "GetFriendListReq", "GetFriendRemarkListReq", "GetGachaDisplayReq",
        "GetGardenScheduleReq", "GetGobackReq", "GetGratuityActivityReq", "GetMasterPupilCardReq",
        "GetMasterPupilDataReq", "GetMasterPupilMainDataReq", "GetMosaicActivityReq", "GetNewbieActivityReq",
        "GetNinjaActivityReq", "GetOfflineResourceDataReq", "GetOpenworldQuestActivityReq",
        "GetRaffleActivityReq", "GetRankScheduleDataReq", "GetRecommendFriendListReq", "GetRecommendGoodsReq",
        "GetRoomDataReq", "GetRpgTaleReq", "GetScratchTicketReq", "GetSecurityPasswordReq", "GetShoppingMallListReq",
        "GetStageChapterReq", "GetSupportActivityReq", "GetSurveyDataReq",
        "GetTradingCardActivityReq", "GetTvtActivityReq", "GetWeeklyRoutineActivityReq", "GrandKeyActivateSkillReq",
        "MassiveWarGetActivityReq", "OpenworldGetMechaTeamReq", "OpenworldHuntActivityGetDataReq",
        "PjmsGetAchievementDataReq", "PjmsGetConditionDataReq", "PjmsGetCurWorldReq", "PjmsGetStoryDataReq",
        "ReunionCookGetActivityReq", "SimplifiedGodWarGetActivityReq",
        "StageInnerDataReportReq", "SusannaTrialGetActivityReq", "ThemeWantedRefreshTicketReq",
        "UpdateMissionProgressReq", "WaveRushGetActivityReq"
    ];

    public override async void Start()
    {
        Logger.Info($"New connection from {RemoteEndPoint}.");
        State = SessionStateEnum.WAITING_FOR_TOKEN;
        await ReceiveLoop();
    }

    public override void Stop(bool isServerStop = false)
    {
        //if (isServerStop) await Player!.SendPacket(new PacketPlayerKickOutScNotify(KickType.KickLoginWhiteTimeout));
        Player?.OnLogoutAsync();
        KcpListener.UnregisterConnection(this);
        base.Stop(isServerStop);
    }

    protected async Task ReceiveLoop()
    {
        while (!CancelToken.IsCancellationRequested)
        {
            // WaitToReceiveAsync call completes when there is at least one message is received or the transport is closed.
            var result = await Conversation.WaitToReceiveAsync(CancelToken.Token);
            if (result.TransportClosed)
            {
                Logger.Debug("Connection was closed");
                break;
            }

            var buffer = ArrayPool<byte>.Shared.Rent(result.BytesReceived);
            try
            {
                // TryReceive should not return false here, unless the transport is closed.
                // So we don't need to check for result.TransportClosed.
                if (!Conversation.TryReceive(buffer, out result))
                {
                    Logger.Error("Failed to receive packet");
                    break;
                }

                await ProcessMessageAsync(buffer.AsMemory(0, result.BytesReceived));
            }
            catch (Exception ex)
            {
                Logger.Error("Packet parse error", ex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        Stop();
    }

    // DO THE PROCESSING OF THE GAME PACKET
    private async Task ProcessMessageAsync(Memory<byte> data)
    {
        var gamePacket = data.ToArray();

        await using MemoryStream ms = new(gamePacket);
        using BinaryReader br = new(ms);

        // Handle
        try
        {
            while (br.BaseStream.Position < br.BaseStream.Length)
            {
                // Length
                if (br.BaseStream.Length - br.BaseStream.Position < 32) return;
                // Packet sanity check
                var headMagic = br.ReadUInt32BE();
                if (headMagic != 0x01234567)
                {
                    Logger.Error($"Bad Data Package Received: got 0x{headMagic:X}, expect 0x01234567");
                    return; // Bad packet
                }

                var packetVersion = br.ReadUInt16BE();
                var ClientVersion = br.ReadUInt16BE();
                var PacketId = br.ReadUInt32BE();
                var UserId = br.ReadUInt32BE();
                var UserIp = br.ReadUInt32BE();
                var Sign = br.ReadUInt32BE();
                var SignType = br.ReadUInt16BE();
                var CmdId = br.ReadUInt16BE();
                var HeaderLength = br.ReadUInt16BE();
                var BodyLength = br.ReadUInt32BE();

                // Data
                var header = br.ReadBytes(HeaderLength);
                var Body = br.ReadBytes((int)BodyLength);
                var TailMagic = br.ReadUInt32BE();
                LogPacket("Recv", CmdId, Body);
                await HandlePacket(CmdId, header, Body);
            }
        }
        catch (Exception e)
        {
            Logger.Error(e.Message, e);
        }
    }

    private async Task HandlePacket(ushort opcode, byte[] header, byte[] payload)
    {
        var packetName = LogMap.GetValueOrDefault(opcode);
        if (DummyPacketNames.Contains(packetName!))
        {
            await SendDummy(packetName!);
            Logger.Info($"[Dummy] Send Dummy {packetName}");
            return;
        }

        // Find the Handler for this opcode
        var handler = HandlerManager.GetHandler(opcode);
        if (handler != null)
        {
            // Handle
            // Make sure session is ready for packets
            var state = State;
            try
            {
                await handler.OnHandle(this, header, payload);
            }
            catch (Exception e)
            {
                Logger.Error(e.Message, e);
            }
            return;
        }

        if (ConfigManager.Config.ServerOption.EnableDebug &&
                 ConfigManager.Config.ServerOption.DebugNoHandlerPacket)
            Logger.Error($"No handler found for {packetName}({opcode})");
    }

    private async Task SendDummy(string packetName)
    {
        var respName = packetName.Replace("Req", "Rsp"); // Get the response packet name
        if (respName == packetName) return; // do not send rsp when resp name = recv name
        var respOpcode = LogMap.FirstOrDefault(x => x.Value == respName).Key; // Get the response opcode

        // Send Rsp
        await SendPacket(respOpcode);
    }
}