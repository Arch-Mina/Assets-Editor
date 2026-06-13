using System;
using System.Collections.Generic;
using System.IO;
using Tibia.Protobuf.Appearances;

namespace Assets_Editor
{
    public static class LegacyDatValidator
    {
        public static void ValidateFiles(string datPath, string sprPath, LegacyAssetExportProfile profile)
        {
            ValidateExport(datPath, profile, ReadSprSpriteCount(sprPath, profile));
        }

        public static void ValidateExport(string datPath, LegacyAssetExportProfile profile, uint maxSpriteId)
        {
            using var stream = File.OpenRead(datPath);
            using var reader = new BinaryReader(stream);

            var signature = reader.ReadUInt32();
            if (signature != profile.DatSignature)
            {
                throw new InvalidDataException($"Invalid dat signature 0x{signature:X8}; expected 0x{profile.DatSignature:X8} for profile '{profile.Id}'.");
            }

            var objectCount = reader.ReadUInt16();
            var outfitCount = reader.ReadUInt16();
            var effectCount = reader.ReadUInt16();
            var missileCount = reader.ReadUInt16();

            ReadRange(reader, profile, APPEARANCE_TYPE.AppearanceObject, 100, objectCount, maxSpriteId);
            ReadRange(reader, profile, APPEARANCE_TYPE.AppearanceOutfit, 1, outfitCount, maxSpriteId);
            ReadRange(reader, profile, APPEARANCE_TYPE.AppearanceEffect, 1, effectCount, maxSpriteId);
            ReadRange(reader, profile, APPEARANCE_TYPE.AppearanceMissile, 1, missileCount, maxSpriteId);

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': parser stopped at {stream.Position}, file length is {stream.Length}.");
            }
        }

        public static uint ReadSprSpriteCount(string sprPath, LegacyAssetExportProfile profile)
        {
            using var stream = File.OpenRead(sprPath);
            using var reader = new BinaryReader(stream);

            var signature = reader.ReadUInt32();
            if (signature != profile.SprSignature)
            {
                throw new InvalidDataException($"Invalid spr signature 0x{signature:X8}; expected 0x{profile.SprSignature:X8} for profile '{profile.Id}'.");
            }

            return profile.SpriteIdsU32 ? reader.ReadUInt32() : reader.ReadUInt16();
        }

        private static void ReadRange(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint firstId, uint lastId, uint maxSpriteId)
        {
            if (lastId < firstId)
            {
                return;
            }

            for (var id = firstId; id <= lastId; id++)
            {
                ReadAppearance(reader, profile, type, id, maxSpriteId);
            }
        }

        private static void ReadAppearance(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, uint maxSpriteId)
        {
            if (profile.DatLayout == LegacyDatLayout.Tibia860)
            {
                ReadAttributes860(reader, profile, type, id);
                ReadSpriteInfo(reader, profile, type, id, maxSpriteId);
                return;
            }

            ReadAttributes1098(reader, profile, type, id);

            var frameGroupCount = 1;
            if (type == APPEARANCE_TYPE.AppearanceOutfit && profile.IncludeFrameGroups)
            {
                frameGroupCount = ReadByte(reader, profile, type, id, "frame group count");
                if (frameGroupCount == 0)
                {
                    throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} has zero frame groups.");
                }
            }

            for (var group = 0; group < frameGroupCount; group++)
            {
                if (type == APPEARANCE_TYPE.AppearanceOutfit && profile.IncludeFrameGroups)
                {
                    ReadByte(reader, profile, type, id, "frame group type");
                }

                ReadSpriteInfo(reader, profile, type, id, maxSpriteId);
            }
        }

        private static void ReadAttributes860(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id)
        {
            while (true)
            {
                var attr = ReadByte(reader, profile, type, id, "attribute");
                if (attr == 0xFF)
                {
                    return;
                }

                if (attr == (byte)AppearanceFlag860.Clothes && !profile.IncludeModernFlags)
                {
                    throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} writes unsupported Clothes/attr 32.");
                }

                switch ((AppearanceFlag860)attr)
                {
                    case AppearanceFlag860.Ground:
                    case AppearanceFlag860.Writeable:
                    case AppearanceFlag860.WriteableOnce:
                    case AppearanceFlag860.LightSource:
                    case AppearanceFlag860.Displaced:
                    case AppearanceFlag860.Elevated:
                    case AppearanceFlag860.MinimapColor:
                    case AppearanceFlag860.HelpInfo:
                    case AppearanceFlag860.Clothes:
                        SkipPayload860(reader, (AppearanceFlag860)attr);
                        break;
                    case AppearanceFlag860.Clip:
                    case AppearanceFlag860.Bottom:
                    case AppearanceFlag860.Top:
                    case AppearanceFlag860.Container:
                    case AppearanceFlag860.Stackable:
                    case AppearanceFlag860.ForceUse:
                    case AppearanceFlag860.Multiuse:
                    case AppearanceFlag860.LiquidContainer:
                    case AppearanceFlag860.LiquidPool:
                    case AppearanceFlag860.Impassable:
                    case AppearanceFlag860.Unmovable:
                    case AppearanceFlag860.BlocksSight:
                    case AppearanceFlag860.BlocksPathfinding:
                    case AppearanceFlag860.Pickupable:
                    case AppearanceFlag860.Hangable:
                    case AppearanceFlag860.HooksSouth:
                    case AppearanceFlag860.HooksEast:
                    case AppearanceFlag860.Rotateable:
                    case AppearanceFlag860.AlwaysSeen:
                    case AppearanceFlag860.Translucent:
                    case AppearanceFlag860.LyingObject:
                    case AppearanceFlag860.AlwaysAnimated:
                    case AppearanceFlag860.FullTile:
                    case AppearanceFlag860.Lookthrough:
                        break;
                    default:
                        throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} has unsupported 8.60 attr {attr}.");
                }
            }
        }

        private static void ReadAttributes1098(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id)
        {
            while (true)
            {
                var attr = ReadByte(reader, profile, type, id, "attribute");
                if (attr == 0xFF)
                {
                    return;
                }

                switch ((AppearanceFlag1098)attr)
                {
                    case AppearanceFlag1098.Ground:
                    case AppearanceFlag1098.Writeable:
                    case AppearanceFlag1098.WriteableOnce:
                    case AppearanceFlag1098.LightSource:
                    case AppearanceFlag1098.Displaced:
                    case AppearanceFlag1098.Elevated:
                    case AppearanceFlag1098.MinimapColor:
                    case AppearanceFlag1098.HelpInfo:
                    case AppearanceFlag1098.Clothes:
                    case AppearanceFlag1098.DefaultAction:
                        SkipPayload1098(reader, (AppearanceFlag1098)attr);
                        break;
                    case AppearanceFlag1098.Market:
                        ReadMarket(reader, profile, type, id);
                        break;
                    case AppearanceFlag1098.Clip:
                    case AppearanceFlag1098.Bottom:
                    case AppearanceFlag1098.Top:
                    case AppearanceFlag1098.Container:
                    case AppearanceFlag1098.Stackable:
                    case AppearanceFlag1098.ForceUse:
                    case AppearanceFlag1098.Usable:
                    case AppearanceFlag1098.Multiuse:
                    case AppearanceFlag1098.LiquidContainer:
                    case AppearanceFlag1098.LiquidPool:
                    case AppearanceFlag1098.Impassable:
                    case AppearanceFlag1098.Unmovable:
                    case AppearanceFlag1098.BlocksSight:
                    case AppearanceFlag1098.BlocksPathfinding:
                    case AppearanceFlag1098.NoMovementAnimation:
                    case AppearanceFlag1098.Pickupable:
                    case AppearanceFlag1098.Hangable:
                    case AppearanceFlag1098.HooksSouth:
                    case AppearanceFlag1098.HooksEast:
                    case AppearanceFlag1098.Rotateable:
                    case AppearanceFlag1098.AlwaysSeen:
                    case AppearanceFlag1098.Translucent:
                    case AppearanceFlag1098.LyingObject:
                    case AppearanceFlag1098.AlwaysAnimated:
                    case AppearanceFlag1098.FullTile:
                    case AppearanceFlag1098.Lookthrough:
                    case AppearanceFlag1098.Wrappable:
                    case AppearanceFlag1098.UnWrappable:
                    case AppearanceFlag1098.TopEffect:
                        break;
                    default:
                        throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} has unsupported 10/11 attr {attr}.");
                }
            }
        }

        private static void ReadMarket(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id)
        {
            ReadUInt16(reader, profile, type, id, "market category");
            ReadUInt16(reader, profile, type, id, "market trade as");
            ReadUInt16(reader, profile, type, id, "market show as");
            var nameLength = ReadUInt16(reader, profile, type, id, "market name length");
            if (profile.MaxMarketNameLength > 0 && nameLength > profile.MaxMarketNameLength)
            {
                throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} market name length is {nameLength}, max is {profile.MaxMarketNameLength}.");
            }

            Skip(reader, nameLength, profile, type, id, "market name");
            ReadUInt16(reader, profile, type, id, "market vocation");
            ReadUInt16(reader, profile, type, id, "market level");
        }

        private static void ReadSpriteInfo(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, uint maxSpriteId)
        {
            var width = ReadByte(reader, profile, type, id, "sprite width");
            var height = ReadByte(reader, profile, type, id, "sprite height");
            if (width == 0 || height == 0)
            {
                throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} has zero sprite dimensions.");
            }

            if (width > 1 || height > 1)
            {
                ReadByte(reader, profile, type, id, "sprite exact size");
            }

            var layers = ReadByte(reader, profile, type, id, "sprite layers");
            var patternX = ReadByte(reader, profile, type, id, "sprite pattern x");
            var patternY = ReadByte(reader, profile, type, id, "sprite pattern y");
            var patternZ = ReadByte(reader, profile, type, id, "sprite pattern z");
            var frames = ReadByte(reader, profile, type, id, "sprite frames");

            if (layers == 0 || patternX == 0 || patternY == 0 || patternZ == 0 || frames == 0)
            {
                throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} has zero sprite pattern data.");
            }

            if (profile.IncludeEnhancedAnimations && frames > 1)
            {
                ReadByte(reader, profile, type, id, "animation mode");
                ReadUInt32(reader, profile, type, id, "animation loop count");
                ReadByte(reader, profile, type, id, "animation default phase");
                for (var phase = 0; phase < frames; phase++)
                {
                    ReadUInt32(reader, profile, type, id, "animation min duration");
                    ReadUInt32(reader, profile, type, id, "animation max duration");
                }
            }

            var spriteCount = (uint)width * height * layers * patternX * patternY * patternZ * frames;
            for (var sprite = 0; sprite < spriteCount; sprite++)
            {
                var spriteId = profile.SpriteIdsU32
                    ? ReadUInt32(reader, profile, type, id, "sprite id")
                    : ReadUInt16(reader, profile, type, id, "sprite id");

                if (spriteId > maxSpriteId)
                {
                    throw new InvalidDataException($"Dat validation failed for profile '{profile.Id}': {type} {id} references sprite {spriteId}, but spr has only {maxSpriteId} sprites.");
                }
            }
        }

        private static void SkipPayload860(BinaryReader reader, AppearanceFlag860 attr)
        {
            var bytes = attr switch
            {
                AppearanceFlag860.LightSource or AppearanceFlag860.Displaced => 4,
                _ => 2,
            };
            Skip(reader, bytes, null, default, 0, attr.ToString());
        }

        private static void SkipPayload1098(BinaryReader reader, AppearanceFlag1098 attr)
        {
            var bytes = attr switch
            {
                AppearanceFlag1098.LightSource or AppearanceFlag1098.Displaced => 4,
                _ => 2,
            };
            Skip(reader, bytes, null, default, 0, attr.ToString());
        }

        private static byte ReadByte(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, string field)
        {
            EnsureAvailable(reader, 1, profile, type, id, field);
            return reader.ReadByte();
        }

        private static ushort ReadUInt16(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, string field)
        {
            EnsureAvailable(reader, 2, profile, type, id, field);
            return reader.ReadUInt16();
        }

        private static uint ReadUInt32(BinaryReader reader, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, string field)
        {
            EnsureAvailable(reader, 4, profile, type, id, field);
            return reader.ReadUInt32();
        }

        private static void Skip(BinaryReader reader, int bytes, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, string field)
        {
            EnsureAvailable(reader, bytes, profile, type, id, field);
            reader.BaseStream.Seek(bytes, SeekOrigin.Current);
        }

        private static void EnsureAvailable(BinaryReader reader, int bytes, LegacyAssetExportProfile profile, APPEARANCE_TYPE type, uint id, string field)
        {
            if (reader.BaseStream.Position + bytes <= reader.BaseStream.Length)
            {
                return;
            }

            var profileId = profile?.Id ?? "unknown";
            throw new InvalidDataException($"Dat validation failed for profile '{profileId}': EOF while reading {field} for {type} {id} at offset {reader.BaseStream.Position}.");
        }
    }
}
