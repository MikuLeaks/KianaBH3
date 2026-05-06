using KianaBH.Data;
using KianaBH.Data.Excel;
using KianaBH.Enums.Item;
using KianaBH.Enums.Player;
using KianaBH.Internationalization;
using KianaBH.Proto;
using KianaBH.Util;

namespace KianaBH.GameServer.Command.Commands;

[CommandInfo("give", "Game.Command.GiveOne.Desc", "Game.Command.GiveOne.Usage", ["g"], [PermEnum.Admin, PermEnum.Support])]
public class CommandGiveOne : ICommands
{
    [CommandMethod("material")]
    public async ValueTask GiveMaterial(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;

        var itemID = arg.GetInt(0);
        var quantity = Math.Max(1,arg.GetInt(1)); //Must give at least one

        GameData.MaterialData.TryGetValue(itemID, out MaterialDataExcel? item);
        if (item == null)
        {
            Logger.GetByClassName().Debug($"Player tried giving an item with ID {itemID} which does not exist");
            await arg.SendMsg(I18NManager.Translate("Game.Command.GiveOne.ItemNotExist"));
            return;
        }


        if (itemID == 100)
        {
            quantity = Math.Min(quantity, 99999999);
        }
        else
        {
            quantity = Math.Min(quantity, item.QuantityLimit);

            if (quantity <= 0)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.GiveOne.InvalidQuantity"));
                return;
            }
        }


        //var mat = arg.Target!.Player!.InventoryManager!.get
        var outItem = await arg.Target!.Player!.InventoryManager!.AddItem(itemID, quantity, ItemMainTypeEnum.Material, 0, sync: true);
        if (outItem == null)
        {
            Logger.GetByClassName().Error($"Player tried giving an item with ID {itemID} which does not exist");
            await arg.SendMsg(I18NManager.Translate("Game.Command.GiveOne.FailedToAdd", itemID.ToString(), ItemMainTypeEnum.Material.ToString()));
            return;
        }
        else
        {
            //The textmap is not loaded at this point, sadly...
            //MaterialDataExcel itemExcel = GameData.MaterialData[itemID];
            //var itemNameTextmapID = itemExcel.BaseType.hash;
            //var itemName = map.TryGetValue(material.BaseType.hash, out var value) ? value : $"[{material.BaseType.hash}]";
            await arg.SendMsg(I18NManager.Translate("Game.Command.GiveOne.GiveOneItem", quantity.ToString(), I18NManager.Translate("Word.Material")));

        }

            //await arg.Target!.Player!.SyncInventory();
    }
}