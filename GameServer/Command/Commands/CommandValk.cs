using KianaBH.Data;
using KianaBH.Database.Avatar;
using KianaBH.Enums.Player;
using KianaBH.Internationalization;

namespace KianaBH.GameServer.Command.Commands;

[CommandInfo("valk", "Game.Command.Valk.Desc", "Game.Command.Valk.Usage", ["v"], [PermEnum.Admin, PermEnum.Support])]
public class CommandValk : ICommands
{
    [CommandMethod("add")]
    public async ValueTask GetValk(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;

        var valkId = arg.GetInt(0);
        if (await arg.GetOption('l') is not int level) return;
        if (await arg.GetOption('s') is not int star) return;

        level = Math.Clamp(level, 1, 80);
        star = Math.Clamp(star, 1, 5);

        if (valkId == -1)
        {
            // add all
            foreach (var config in GameData.AvatarData.Values)
                await arg.Target!.Player!.AvatarManager!.AddAvatar(config.AvatarID, level, star,false);
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkAddedAll"));
            await arg.Target!.Player!.SyncAll();
        }
        else
        {
            var valk = await arg.Target!.Player!.AvatarManager!.AddAvatar(valkId, level, star);
            if (valk == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkNotFound"));
                return;
            }
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkAdded",
                valk!.FaceAnimationGroupName ?? valkId.ToString()));
        }
    }

    [CommandMethod("level")]
    public async ValueTask SetValkLevel(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (!await arg.CheckArgCnt(2)) return;

        var valkId = arg.GetInt(0);
        var level = Math.Clamp(arg.GetInt(1), 1, 80); // Limit level to 1-80

        if (valkId == -1)
        {
            // set level for all valk
            foreach (var config in arg.Target!.Player!.AvatarManager!.AvatarData.Avatars)
            {
                config.Level = level;
            }
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetLevelAll", level.ToString()));
        }
        else
        {
            var valk = arg.Target!.Player!.AvatarManager!.GetAvatar(valkId);
            if (valk == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkNotFound"));
                return;
            }

            valk.Level = level;
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetLevel",
                valkId.ToString(), level.ToString()));
        }
        await arg.Target!.Player!.SyncValk();
    }

    [CommandMethod("star")]
    public async ValueTask SetValkStar(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (!await arg.CheckArgCnt(2)) return;

        var valkId = arg.GetInt(0);
        var star = Math.Clamp(arg.GetInt(1), 1, 5); // Limit star to 1-5

        if (valkId == -1)
        {
            // set star for all valk
            foreach (var config in arg.Target!.Player!.AvatarManager!.AvatarData.Avatars)
            {
                config.Star = star;
            }
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetStarAll", star.ToString()));
        }
        else
        {
            var valk = arg.Target!.Player!.AvatarManager!.GetAvatar(valkId);
            if (valk == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkNotFound"));
                return;
            }

            valk.Star = star;
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetStar",
                valkId.ToString(), star.ToString()));
        }
        await arg.Target!.Player!.SyncValk();
    }

    [CommandMethod("skill")]
    public async ValueTask SetValkSkill(CommandArg arg)
    {
        if (!await arg.CheckOnlineTarget()) return;
        if (!await arg.CheckArgCnt(1)) return;

        var valkId = arg.GetInt(0);
        if (valkId == -1)
        {
            foreach (var valk in arg.Target!.Player!.AvatarManager!.AvatarData.Avatars)
            {
                foreach (var skill in valk.SkillList)
                {
                    var subSkillList = GameData.AvatarSubSkillData.Values
                        .Where(x => x.SkillId == skill.SkillId);

                    foreach (var subSkillData in subSkillList)
                    {
                        var subSkill = skill.SubSkillList
                            .Find(x => x.SubSkillId == subSkillData.AvatarSubSkillId);

                        if (subSkill != null)
                        {
                            subSkill.Level = subSkillData.MaxLv;
                        }
                        else
                        {
                            skill.SubSkillList.Add(new AvatarSubSkill
                            {
                                SubSkillId = subSkillData.AvatarSubSkillId,
                                Level = subSkillData.MaxLv
                            });
                        }
                    }
                }
            }
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetSkillLevelAll"));
        }
        else
        {
            var valk = arg.Target!.Player!.AvatarManager!.GetAvatar(valkId);
            if (valk == null)
            {
                await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkNotFound"));
                return;
            }

            foreach (var skill in valk.SkillList)
            {
                var subSkillList = GameData.AvatarSubSkillData.Values
                    .Where(x => x.SkillId == skill.SkillId);

                foreach (var subSkillData in subSkillList)
                {
                    var subSkill = skill.SubSkillList
                        .Find(x => x.SubSkillId == subSkillData.AvatarSubSkillId);

                    if (subSkill != null)
                    {
                        subSkill.Level = subSkillData.MaxLv;
                    }
                    else
                    {
                        skill.SubSkillList.Add(new AvatarSubSkill
                        {
                            SubSkillId = subSkillData.AvatarSubSkillId,
                            Level = subSkillData.MaxLv
                        });
                    }
                }
            }
            await arg.SendMsg(I18NManager.Translate("Game.Command.Valk.ValkSetSkillLevel",
                valkId.ToString()));
        }
        await arg.Target!.Player!.SyncValk();
    }
}