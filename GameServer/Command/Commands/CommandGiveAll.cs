using KianaBH.Data;
using KianaBH.Enums.Item;
using KianaBH.Enums.Player;
using KianaBH.Internationalization;

namespace KianaBH.GameServer.Command.Commands;

[CommandInfo("giveall", "Game.Command.GiveAll.Desc", "Game.Command.GiveAll.Usage", ["ga"], [PermEnum.Admin, PermEnum.Support])]
public class CommandGiveall : ICommands
{
    [CommandMethod("weapon")]
    public async ValueTask GiveWeapon(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        foreach (var conf in GameData.WeaponData.Values.Where(weapon => weapon.Rarity == weapon.MaxRarity))
        {
            var item = await arg.Target!.Player!.InventoryManager!.AddItem(conf.ID, 1, ItemMainTypeEnum.Weapon, conf.MaxLv, sync:false);
        }
        await arg.Target!.Player!.SyncInventory();
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems", I18NManager.Translate("Word.Weapon")));
    }

    [CommandMethod("stigmata")]
    public async ValueTask GiveStigmata(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        foreach (var conf in GameData.StigmataData.Values.Where(stigmata => stigmata.Rarity == stigmata.MaxRarity))
        {
            var item = await arg.Target!.Player!.InventoryManager!.AddItem(conf.ID, 1, ItemMainTypeEnum.Stigmata, conf.MaxLv, sync: false);
        }
        await arg.Target!.Player!.SyncInventory();
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems", I18NManager.Translate("Word.Stigmata")));
    }

    [CommandMethod("material")]
    public async ValueTask GiveMaterial(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        foreach (var conf in GameData.MaterialData.Values)
        {
            //Coins go up to 99999999, all others is 999
            var quantity = conf.ID == 100 ? 99999999 : (conf.QuantityLimit > 999 ? 999 : conf.QuantityLimit);
            var item = await arg.Target!.Player!.InventoryManager!.AddItem(conf.ID, quantity, ItemMainTypeEnum.Material, 0, sync: false);
        }
        await arg.Target!.Player!.SyncInventory();
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems", I18NManager.Translate("Word.Material")));
    }

    [CommandMethod("dress")]
    public async ValueTask GiveDress(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        foreach (var config in GameData.DressData.Values)
        {
            foreach (var valkId in config.AvatarIDList)
            {
                var valk = arg.Target!.Player!.AvatarManager!.GetAvatar(valkId);
                if (valk == null) continue;
                if (valk.DressList.Contains(config.DressID)) continue;
                valk.DressList.Add(config.DressID);
            }
        }
        await arg.Target!.Player!.SyncValk();
        await arg.SendMsg(I18NManager.Translate("Game.Command.GiveAll.GiveAllItems", I18NManager.Translate("Word.Dress")));
    }
}

