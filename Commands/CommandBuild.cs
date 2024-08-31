using DanhengPlugin.CharacterBuilder.Data;
using EggLink.DanhengServer.Command;
using EggLink.DanhengServer.Command.Command;
using EggLink.DanhengServer.Data;
using EggLink.DanhengServer.Database.Avatar;
using EggLink.DanhengServer.Database.Inventory;
using EggLink.DanhengServer.Enums.Avatar;
using EggLink.DanhengServer.Enums.Item;
using EggLink.DanhengServer.Internationalization;
using EggLink.DanhengServer.Util;

namespace DanhengPlugin.CharacterBuilder.Commands;

[CommandInfo("build", "Build a character", "Usage: /build <cur/[id]>")]
public class CommandBuild : ICommand
{
    [CommandMethod("0 cur")]
    public async ValueTask BuildCur(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        // get current avatar data
        var id = player.LineupManager?.GetCurLineup()?.LeaderAvatarId ?? 0;
        if (id == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }
        // get avatar data
        var avatar = player.AvatarManager!.GetAvatar(id);
        if (avatar == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        await BuildAvatar(avatar, arg);
    }

    [CommandDefault]
    public async ValueTask BuildTarget(CommandArg arg)
    {
        var player = arg.Target?.Player;
        if (player == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.PlayerNotFound"));
            return;
        }

        if (arg.BasicArgs.Count == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Notice.InvalidArguments"));
            return;
        }

        var id = arg.GetInt(0);
        if (id == 0)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }
        // get avatar data
        var avatar = player.AvatarManager!.GetAvatar(id);
        if (avatar == null)
        {
            await arg.SendMsg(I18NManager.Translate("Game.Command.Avatar.AvatarNotFound"));
            return;
        }

        await BuildAvatar(avatar, arg);
    }

    public async ValueTask BuildAvatar(AvatarInfo avatar, CommandArg arg)
    {
        // build avatar
        var player = arg.Target!.Player!;
        if (player.InventoryManager!.Data.RelicItems.Count + 6 > GameConstants.INVENTORY_MAX_RELIC)
        {
            await arg.SendMsg(I18NManager.Translate("CharacterBuilder.InventoryRelicMax"));
            return;
        }
        PluginGameData.AvatarRelicRecommendData.TryGetValue(avatar.GetAvatarId(), out var excel);
        if (excel == null)
        {
            await arg.SendMsg(I18NManager.Translate("CharacterBuilder.NoRecommend"));
            return;
        }

        var fourSetId = excel.Set4IDList[0];
        var twoSetId = excel.Set2IDList[0];

        // get excels
        var head = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == fourSetId && x is { Type: RelicTypeEnum.HEAD, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        var hand = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == fourSetId && x is { Type: RelicTypeEnum.HAND, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        var body = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == fourSetId && x is { Type: RelicTypeEnum.BODY, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        var foot = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == fourSetId && x is { Type: RelicTypeEnum.FOOT, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        var neck = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == twoSetId && x is { Type: RelicTypeEnum.NECK, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        var obj = GameData.RelicConfigData.Values.FirstOrDefault(x =>
            x.SetID == twoSetId && x is { Type: RelicTypeEnum.OBJECT, Rarity: RarityEnum.CombatPowerRelicRarity5 });

        if (head == null || hand == null || body == null || foot == null || neck == null || obj == null)
        {
            await arg.SendMsg(I18NManager.Translate("CharacterBuilder.NoRecommend"));
            return;
        }

        // build it
        var affixPropertyList = excel.SubAffixPropertyList;
        HashSet<int> subAffixId = [];

        foreach (var id in affixPropertyList.Select(typeEnum => PluginConstants.RelicSubAffix.GetValueOrDefault(typeEnum, 1)))
        {
            subAffixId.Add(id);
        }

        var curId = 1;
        while (subAffixId.Count < 4)
        {
            // need to add to 4
            subAffixId.Add(curId++);
        }

        List<ItemSubAffix> subAffixes = [];
        var count = 5;
        var affixIndex = 0;
        foreach (var id in subAffixId)
        {
            affixIndex++;
            var extraCount = Random.Shared.Next(0, count + 1);
            if (affixIndex == 4)
            {
                extraCount = count;
            }
            count -= extraCount;
            subAffixes.Add(new ItemSubAffix(GameData.RelicSubAffixData[5][id], 1 + extraCount));
        }

        var headMainAffixId = 1;
        var handMainAffixId = 1;
        var bodyMainAffixId = PluginConstants.RelicMainAffix[RelicTypeEnum.BODY]
            .GetValueOrDefault(excel.PropertyList.FirstOrDefault(x => x.RelicType == RelicTypeEnum.BODY)?.PropertyType ?? AvatarPropertyTypeEnum.HPAddedRatio,
                1);
        var footMainAffixId = PluginConstants.RelicMainAffix[RelicTypeEnum.FOOT]
            .GetValueOrDefault(excel.PropertyList.FirstOrDefault(x => x.RelicType == RelicTypeEnum.FOOT)?.PropertyType ?? AvatarPropertyTypeEnum.HPAddedRatio,
                1);
        var neckMainAffixId = PluginConstants.RelicMainAffix[RelicTypeEnum.NECK]
            .GetValueOrDefault(excel.PropertyList.FirstOrDefault(x => x.RelicType == RelicTypeEnum.NECK)?.PropertyType ?? AvatarPropertyTypeEnum.HPAddedRatio,
                1);
        var objMainAffixId = PluginConstants.RelicMainAffix[RelicTypeEnum.OBJECT]
            .GetValueOrDefault(excel.PropertyList.FirstOrDefault(x => x.RelicType == RelicTypeEnum.OBJECT)?.PropertyType ?? AvatarPropertyTypeEnum.HPAddedRatio,
                1);

        // build head

        var headSubAffixes = subAffixes.ToList();
        var removeCount = 0;
        foreach (var i in subAffixes.Where(i => PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == AvatarPropertyTypeEnum.HPDelta))
        {
            // not same
            headSubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var headItem = new ItemData
        {
            ItemId = head.ID,
            Count = 1,
            Level = 15,
            MainAffix = headMainAffixId,
            SubAffixes = headSubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            headItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                headItem.IncreaseRandomRelicSubAffix();
            }
        }

        // build hand

        var handSubAffixes = subAffixes.ToList();
        removeCount = 0;
        foreach (var i in subAffixes.Where(i => PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == AvatarPropertyTypeEnum.AttackDelta))
        {
            // not same
            handSubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var handItem = new ItemData
        {
            ItemId = hand.ID,
            Count = 1,
            Level = 15,
            MainAffix = handMainAffixId,
            SubAffixes = handSubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            handItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                handItem.IncreaseRandomRelicSubAffix();
            }
        }

        // build body

        var bodySubAffixes = subAffixes.ToList();
        removeCount = 0;
        foreach (var i in subAffixes.Where(i =>
                     PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == PluginConstants
                         .RelicMainAffix[RelicTypeEnum.BODY].First(c => c.Value == bodyMainAffixId).Key))
        {
            // not same
            bodySubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var bodyItem = new ItemData
        {
            ItemId = body.ID,
            Count = 1,
            Level = 15,
            MainAffix = bodyMainAffixId,
            SubAffixes = bodySubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            bodyItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                bodyItem.IncreaseRandomRelicSubAffix();
            }
        }

        // build foot

        var footSubAffixes = subAffixes.ToList();
        removeCount = 0;
        foreach (var i in subAffixes.Where(i =>
                     PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == PluginConstants
                         .RelicMainAffix[RelicTypeEnum.FOOT].First(c => c.Value == footMainAffixId).Key))
        {
            // not same
            bodySubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var footItem = new ItemData
        {
            ItemId = foot.ID,
            Count = 1,
            Level = 15,
            MainAffix = footMainAffixId,
            SubAffixes = footSubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            footItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                footItem.IncreaseRandomRelicSubAffix();
            }
        }

        // build neck

        var neckSubAffixes = subAffixes.ToList();
        removeCount = 0;
        foreach (var i in subAffixes.Where(i =>
                     PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == PluginConstants
                         .RelicMainAffix[RelicTypeEnum.NECK].First(c => c.Value == neckMainAffixId).Key))
        {
            // not same
            neckSubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var neckItem = new ItemData
        {
            ItemId = neck.ID,
            Count = 1,
            Level = 15,
            MainAffix = neckMainAffixId,
            SubAffixes = neckSubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            neckItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                neckItem.IncreaseRandomRelicSubAffix();
            }
        }

        // build object

        var objSubAffixes = subAffixes.ToList();
        removeCount = 0;
        foreach (var i in subAffixes.Where(i =>
                     PluginConstants.RelicSubAffix.First(x => x.Value == i.Id).Key == PluginConstants
                         .RelicMainAffix[RelicTypeEnum.OBJECT].First(c => c.Value == objMainAffixId).Key))
        {
            // not same
            objSubAffixes.Remove(i);
            removeCount += i.Count;
        }

        var objItem = new ItemData
        {
            ItemId = obj.ID,
            Count = 1,
            Level = 15,
            MainAffix = objMainAffixId,
            SubAffixes = objSubAffixes,
            UniqueId = ++player.InventoryManager!.Data.NextUniqueId
        };

        if (removeCount > 0)
        {
            objItem.AddRandomRelicSubAffix();
            for (var i = 0; i < removeCount - 1; i++)
            {
                objItem.IncreaseRandomRelicSubAffix();
            }
        }

        // add to inventory
        await player.InventoryManager.AddItem(headItem, false);
        await player.InventoryManager.AddItem(handItem, false);
        await player.InventoryManager.AddItem(bodyItem, false);
        await player.InventoryManager.AddItem(footItem, false);
        await player.InventoryManager.AddItem(neckItem, false);
        await player.InventoryManager.AddItem(objItem, false);

        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), headItem.UniqueId, 1);
        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), handItem.UniqueId, 2);
        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), bodyItem.UniqueId, 3);
        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), footItem.UniqueId, 4);
        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), neckItem.UniqueId, 5);
        await player.InventoryManager.EquipRelic(avatar.GetAvatarId(), objItem.UniqueId, 6);

        await arg.SendMsg(I18NManager.Translate("CharacterBuilder.BuildSuccess"));
    }
}