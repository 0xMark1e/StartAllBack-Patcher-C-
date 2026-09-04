using System.Collections.Generic;

namespace SynezSAB
{
    internal static class Data
    {
        public const string DllPath = @"C:\Program Files\StartAllBack\StartAllBackX64.dll";
        public const string TrialReminderKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\CLSID";

        public static readonly byte[] FootprintCheckLicense = new byte[]
        {
            0x48, 0x89, 0x5c, 0x24, 0x08, 0x55, 0x56, 0x57, 0x48, 0x8d, 0xac, 0x24,
            0x70, 0xff, 0xff, 0xff, 0x48, 0x81, 0xec, 0x90, 0x01, 0x00, 0x00, 0x48,
            0x8b, 0xf1, 0x48, 0x8d, 0x4d, 0x20
        };

        public static readonly byte[] FootprintCompareFileTime = new byte[]
        {
            0x48, 0x89, 0x5c, 0x24, 0x18, 0x57, 0x48, 0x83, 0xec, 0x30, 0x48, 0x8d,
            0x4c, 0x24, 0x48
        };

        public static readonly byte[] PatchedFootprintCheckLicense = new byte[]
        {
            0x48, 0xc7, 0x01, 0x01, 0x00, 0x00, 0x00, 0xb8, 0x01, 0x00, 0x00, 0x00, 0xc3
        };

        public static readonly byte[] PatchedFootprintCompareFileTime = new byte[]
        {
            0x48, 0x89, 0x5c, 0x24, 0x18, 0xb8, 0x00, 0x00, 0x00, 0x00, 0xc3
        };

        public static readonly List<PatchEntry> Repository = new List<PatchEntry>
        {
            new PatchEntry("3.5.5",
                "3f38db606009e1fc4ea82beb357f8351d438c0d0",
                "b812d69da8e057463f33ad500dabb7d52be6b6a5",
                new[] {
                    new BytePatch(0x1369, new byte[]{ 0xc7,0x01,0x01,0x00,0x00,0x00,0xb8,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(0x1564, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("3.5.6",
                "00d51d42b6715fbc7a822e0ec443b6c74eacd7a7",
                "264649730808f5ec7259f2a54feda2651f04705c",
                new[] {
                    new BytePatch(0x1369, new byte[]{ 0xc7,0x01,0x01,0x00,0x00,0x00,0xb8,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(0x1564, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("3.5.7",
                "5e4009d5400360af836045eaf76cd6dcb15c688b",
                "26b168a2db8a7ed62ba4222bef1bfd2db064dc8e",
                new[] {
                    new BytePatch(0x1369, new byte[]{ 0xc7,0x01,0x01,0x00,0x00,0x00,0xb8,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(0x1564, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.0",
                "c517f366c06400fbf89b142090d6842c224afc8a",
                "bc759d64b3d7af277814e2ceae3dbf9577f3a24e",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.1",
                "31fb650427a846b7d3a381db0ce2a46df45b6b94",
                "6a0989c36afecb6d778a0a685f9923ad2dfc2c98",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.2",
                "b0ff7156c7d1d48a98c5f0751ad762b68e677c74",
                "0962f3b1ea42cadda3e0ab158415e0074a8eea2f",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.3",
                "d089c96a4d562c775cf982b2d805d2ff500b0be5",
                "9de5fce9fef93a45866fa66b6b445facb295c676",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.4",
                "64ffbe4f16c565d3362ea3f09b9d0468eafbd368",
                "d2a16d1375f5f63145da96331307dc8cdd48beaa",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
            new PatchEntry("v3.6.5",
                "a499c47201a2be752c849d94ebb8af3efe65d791",
                "abdedd119d8d7c2db7bdf1a26b3f14823d5a82ef",
                new[] {
                    new BytePatch(4972, new byte[]{ 0x67,0x48,0xc7,0x01,0x01,0x00,0x00,0x00 }),
                    new BytePatch(4981, new byte[]{ 0xc7,0xc0,0x01,0x00,0x00,0x00,0xc3 }),
                    new BytePatch(5480, new byte[]{ 0xb8,0x00,0x00,0x00,0x00,0xc3 }),
                }),
        };
    }

    internal class BytePatch
    {
        public int Offset { get; }
        public byte[] Bytes { get; }
        public BytePatch(int offset, byte[] bytes) { Offset = offset; Bytes = bytes; }
    }

    internal class PatchEntry
    {
        public string Version { get; }
        public string OriginalHash { get; }
        public string PatchedHash { get; }
        public BytePatch[] Patches { get; }
        public PatchEntry(string version, string originalHash, string patchedHash, BytePatch[] patches)
        {
            Version = version; OriginalHash = originalHash; PatchedHash = patchedHash; Patches = patches;
        }
    }
}
