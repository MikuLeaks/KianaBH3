using KianaBH.GameServer.Server.Packet.Send.Player;
using KianaBH.Proto;
using KianaBH.Util;

namespace KianaBH.GameServer.Server.Packet.Recv.Player;

[Opcode(CmdIds.SetSelfDescReq)]
public class HandlerSetSelfDescReq : Handler
{
    public override async Task OnHandle(Connection connection, byte[] header, byte[] data)
    {
        var req = SetSelfDescReq.Parser.ParseFrom(data);
        var player = connection.Player!;

        player.Data.Signature = req.SelfDesc;

        //Send packet to the client that updates the data
        await connection.SendPacket(new PacketGetMainDataRsp(player));
        //This sends a dummy packet with everything set to 0. 0 happens to be success so we don't actually need to implement it
        await connection.SendPacket(CmdIds.SetSelfDescRsp);
    }
}
