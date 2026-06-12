using System;
using System.Collections.Generic;

namespace Assets_Editor
{
    public enum LegacyDatLayout
    {
        Tibia860,
        Tibia1098,
    }

    public sealed class LegacyAssetExportProfile
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public required LegacyDatLayout DatLayout { get; init; }
        public required uint DatSignature { get; init; }
        public required uint SprSignature { get; init; }
        public required bool Transparency { get; init; }
        public required bool SpriteIdsU32 { get; init; }
        public required bool IncludeEnhancedAnimations { get; init; }
        public required bool IncludeFrameGroups { get; init; }
        public required bool IncludeModernFlags { get; init; }
        public required int LegacyOutfitAnimationFrames { get; init; }
    }

    public static class LegacyAssetExportProfiles
    {
        private static readonly LegacyAssetExportProfile[] Profiles =
        [
            new LegacyAssetExportProfile
            {
                Id = "cip860-extended",
                DisplayName = "CipSoft 8.60 extended",
                DatLayout = LegacyDatLayout.Tibia860,
                DatSignature = 0x4C2C7993,
                SprSignature = 0x4C220594,
                Transparency = false,
                SpriteIdsU32 = true,
                IncludeEnhancedAnimations = false,
                IncludeFrameGroups = false,
                IncludeModernFlags = false,
                LegacyOutfitAnimationFrames = 3,
            },
            new LegacyAssetExportProfile
            {
                Id = "canary860-extended",
                DisplayName = "Canary 8.60 extended",
                DatLayout = LegacyDatLayout.Tibia860,
                DatSignature = 0x4C2C7993,
                SprSignature = 0x4C220594,
                Transparency = false,
                SpriteIdsU32 = true,
                IncludeEnhancedAnimations = false,
                IncludeFrameGroups = false,
                IncludeModernFlags = false,
                LegacyOutfitAnimationFrames = 3,
            },
            new LegacyAssetExportProfile
            {
                Id = "legacy1098",
                DisplayName = "Legacy 10.98 compatible",
                DatLayout = LegacyDatLayout.Tibia1098,
                DatSignature = 0x000042A3,
                SprSignature = 0x53159CA9,
                Transparency = false,
                SpriteIdsU32 = true,
                IncludeEnhancedAnimations = true,
                IncludeFrameGroups = true,
                IncludeModernFlags = true,
                LegacyOutfitAnimationFrames = 0,
            },
        ];

        public static IReadOnlyList<LegacyAssetExportProfile> All => Profiles;

        public static LegacyAssetExportProfile Get(string id)
        {
            foreach (var profile in Profiles)
            {
                if (string.Equals(profile.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    return profile;
                }
            }

            if (string.Equals(id, "860", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(id, "cip860", StringComparison.OrdinalIgnoreCase))
            {
                return Get("cip860-extended");
            }

            if (string.Equals(id, "1098", StringComparison.OrdinalIgnoreCase))
            {
                return Get("legacy1098");
            }

            throw new ArgumentException($"Unknown export profile '{id}'. Use --list-profiles to see valid profiles.");
        }
    }
}
