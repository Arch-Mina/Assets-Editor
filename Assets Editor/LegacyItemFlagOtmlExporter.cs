using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public static class LegacyItemFlagOtmlExporter
    {
        public const string FileName = "itemFlag.otml";

        public static int Write(string outputPath, Appearances appearances, LegacyAssetExportProfile profile)
        {
            ArgumentNullException.ThrowIfNull(appearances);
            ArgumentNullException.ThrowIfNull(profile);

            var root = OTMLNode.Create("items");
            var itemCount = 0;

            foreach (var item in appearances.Object.OrderBy(item => item.Id))
            {
                if (item.Flags == null)
                {
                    continue;
                }

                var itemNode = OTMLNode.Create(ToString(item.Id));
                AddItemFlags(itemNode, item.Flags, profile);
                if (itemNode.Children.Count == 0)
                {
                    continue;
                }

                root.AddChild(itemNode);
                itemCount++;
            }

            File.WriteAllText(outputPath, root.Emit() + "\n");
            return itemCount;
        }

        private static void AddItemFlags(OTMLNode itemNode, AppearanceFlags flags, LegacyAssetExportProfile profile)
        {
            if (!profile.IncludeModernFlags)
            {
                if (flags.NoMovementAnimation)
                    AddBool(itemNode, "noMovementAnimation");

                if (flags.Clothes != null)
                    AddValue(itemNode, "cloth", ToClothSlotName(flags.Clothes.Slot));

                if (flags.DefaultAction != null)
                    AddValue(itemNode, "defaultAction", ToDefaultActionName(flags.DefaultAction.Action));

                if (flags.Wrap)
                    AddBool(itemNode, "wrap");

                if (flags.Unwrap)
                    AddBool(itemNode, "unwrap");

                if (flags.Topeffect)
                    AddBool(itemNode, "topEffect");
            }

            if (flags.Changedtoexpire != null)
                AddValue(itemNode, "changedToExpire", flags.Changedtoexpire.HasFormerObjectTypeid ? flags.Changedtoexpire.FormerObjectTypeid : 0);

            if (flags.Corpse)
                AddBool(itemNode, "corpse");

            if (flags.PlayerCorpse)
                AddBool(itemNode, "playerCorpse");

            if (flags.Cyclopediaitem != null)
                AddValue(itemNode, "cyclopediaType", flags.Cyclopediaitem.HasCyclopediaType ? flags.Cyclopediaitem.CyclopediaType : 0);

            if (flags.Ammo)
                AddBool(itemNode, "ammo");

            if (flags.ShowOffSocket)
                AddBool(itemNode, "showOffSocket");

            if (flags.Reportable)
                AddBool(itemNode, "reportable");

            if (flags.Upgradeclassification != null)
                AddValue(itemNode, "classification", flags.Upgradeclassification.HasUpgradeClassification ? flags.Upgradeclassification.UpgradeClassification : 0);

            if (flags.ReverseAddonsEast)
                AddBool(itemNode, "reverseAddonsEast");

            if (flags.ReverseAddonsWest)
                AddBool(itemNode, "reverseAddonsWest");

            if (flags.ReverseAddonsSouth)
                AddBool(itemNode, "reverseAddonsSouth");

            if (flags.ReverseAddonsNorth)
                AddBool(itemNode, "reverseAddonsNorth");

            if (flags.Wearout)
                AddBool(itemNode, "charges");

            if (flags.Clockexpire)
                AddBool(itemNode, "duration");

            if (flags.Expire)
                AddBool(itemNode, "expire");

            if (flags.Expirestop)
                AddBool(itemNode, "expireStop");

            if (flags.DecoItemKit)
                AddBool(itemNode, "decoKit");

            if (flags.SkillwheelGem != null)
                AddSkillWheelGem(itemNode, flags.SkillwheelGem);

            if (flags.DualWielding)
                AddBool(itemNode, "dualWielding");

            if (flags.Imbueable != null)
                AddValue(itemNode, "imbueSlots", flags.Imbueable.HasSlotCount ? flags.Imbueable.SlotCount : 0);

            if (flags.Proficiency != null)
                AddValue(itemNode, "proficiency", flags.Proficiency.HasProficiencyId ? flags.Proficiency.ProficiencyId : 0);

            if (flags.RestrictToVocation.Count > 0)
                AddVocationList(itemNode, "restrictVocation", flags.RestrictToVocation);

            if (flags.HasMinimumLevel)
                AddValue(itemNode, "minimumLevel", flags.MinimumLevel);

            if (flags.HasWeaponType)
                AddValue(itemNode, "weaponType", ToWeaponTypeName(flags.WeaponType));

            if (flags.Transparencylevel != null)
                AddValue(itemNode, "transparencyLevel", flags.Transparencylevel.HasLevel ? flags.Transparencylevel.Level : 0);
        }

        private static void AddSkillWheelGem(OTMLNode itemNode, AppearanceFlagSkillWheelGem flag)
        {
            var node = OTMLNode.Create("skillWheelGem");
            if (flag.HasGemQualityId)
                AddValue(node, "gemQualityId", flag.GemQualityId);

            if (flag.HasVocationId)
                AddValue(node, "vocationId", flag.VocationId);

            if (node.Children.Count > 0)
                itemNode.AddChild(node);
            else
                AddBool(itemNode, "skillWheelGem");
        }

        private static void AddVocationList(OTMLNode itemNode, string tag, Google.Protobuf.Collections.RepeatedField<VOCATION> values)
        {
            var node = OTMLNode.Create(tag);
            foreach (var value in values)
            {
                node.AddChild(OTMLNode.Create(string.Empty, ToVocationName(value)));
            }

            itemNode.AddChild(node);
        }

        private static void AddBool(OTMLNode node, string tag)
        {
            AddValue(node, tag, "true");
        }

        private static void AddValue(OTMLNode node, string tag, uint value)
        {
            AddValue(node, tag, ToString(value));
        }

        private static void AddValue(OTMLNode node, string tag, string value)
        {
            node.AddChild(OTMLNode.Create(tag, value));
        }

        private static string ToString(uint value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string ToString(int value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }

        private static string ToClothSlotName(uint slot)
        {
            return slot switch
            {
                1 => "helmet",
                2 => "amulet",
                3 => "backpack",
                4 => "armor",
                5 => "shield",
                6 => "weapon",
                7 => "legs",
                8 => "boots",
                9 => "ring",
                10 => "arrow",
                _ => "none",
            };
        }

        private static string ToDefaultActionName(PLAYER_ACTION action)
        {
            return (int)action switch
            {
                1 => "look",
                2 => "use",
                3 => "open",
                4 => "autowalkHighlight",
                _ => "none",
            };
        }

        private static string ToVocationName(VOCATION vocation)
        {
            return (int)vocation switch
            {
                -1 => "any",
                0 => "none",
                1 => "knight",
                2 => "paladin",
                3 => "sorcerer",
                4 => "druid",
                5 => "monk",
                10 => "promoted",
                _ => ToString((int)vocation),
            };
        }

        private static string ToWeaponTypeName(WEAPON_TYPE weaponType)
        {
            return (int)weaponType switch
            {
                1 => "sword",
                2 => "axe",
                3 => "club",
                4 => "fist",
                5 => "bow",
                6 => "crossbow",
                7 => "wandRod",
                8 => "throw",
                _ => "none",
            };
        }
    }
}
