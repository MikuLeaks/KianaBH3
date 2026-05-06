using KianaBH.GameServer.Server.Packet.Send.Player;
using KianaBH.Proto;
using KianaBH.Util;


namespace KianaBH.GameServer.Server.Packet.Recv.Player
{
    [Opcode(CmdIds.MedalOpReq)]
    internal class HandlerMedalOpReq : Handler
    {
        public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
        {

            var req = SetCustomHeadReq.Parser.ParseFrom(data);
            var player = connection.Player!;

            //Not sure what player param matches MedalOp
            //player.Data.HeadIcon = (int)req.Id;
            //Send packet to the client that updates the data
            //await connection.SendPacket(new PacketGetMainDataRsp(player));
            Logger.GetByClassName().Warn("MedalOpReq not implemented");
            await connection.SendPacket(CmdIds.MedalOpRsp);
        }
    }
}
