using KianaBH.GameServer.Server.Packet.Send.Player;
using KianaBH.Proto;

namespace KianaBH.GameServer.Server.Packet.Recv.Player
{
    [Opcode(CmdIds.SetCustomHeadReq)]
    internal class HandlerSetCustomHeadReq : Handler
    {
        public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
        {

            var req = SetCustomHeadReq.Parser.ParseFrom(data);
            var player = connection.Player!;

            player.Data.HeadIcon = (int)req.Id;
            //Send packet to the client that updates the data
            await connection.SendPacket(new PacketGetMainDataRsp(player));
            await connection.SendPacket(CmdIds.UpdateAssistantAvatarIdRsp);
        }
    }
}
