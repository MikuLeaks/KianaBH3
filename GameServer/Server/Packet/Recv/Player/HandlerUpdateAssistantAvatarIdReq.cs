using KianaBH.GameServer.Server.Packet.Send.Player;
using KianaBH.Proto;
using KianaBH.Util;
using System.Numerics;

namespace KianaBH.GameServer.Server.Packet.Recv.Player
{
    [Opcode(CmdIds.UpdateAssistantAvatarIdReq)]
    internal class HandlerUpdateAssistantAvatarIdReq : Handler
    {
        public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
        {

            var req = UpdateAssistantAvatarIdReq.Parser.ParseFrom(data);
            var avatarId = req.AvatarId;
            var player = connection.Player!;

            player.Data.AssistantAvatarId = (int)avatarId;
            //Send packet to the client that updates the data
            await connection.SendPacket(new PacketGetMainDataRsp(player));
            await connection.SendPacket(CmdIds.UpdateAssistantAvatarIdRsp);
        }
    }
}
