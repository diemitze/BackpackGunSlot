using System.IO;
using EFT.Utilities;
using UnityEngine;

namespace BackpackGunSlot
{
    internal static class SlotIcons
    {
        private static Sprite _smg;
        private static Sprite _shotgun;

        internal static Sprite Pistol => ResourcesCache.Pop<Sprite>("Slots/Equipment/Holster");

        // No vanilla SMG or shotgun ghost, assets/make_ghost.py builds these.
        internal static Sprite Smg => _smg != null ? _smg : _smg = Load("BackpackGunSlot.smg.png");
        internal static Sprite Shotgun => _shotgun != null ? _shotgun : _shotgun = Load("BackpackGunSlot.shotgun.png");

        private static Sprite Load(string resource)
        {
            using var stream = typeof(SlotIcons).Assembly.GetManifestResourceStream(resource);
            if (stream == null)
            {
                Plugin.Log.LogError("Missing embedded icon " + resource);
                return null;
            }
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = resource,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.LoadImage(memory.ToArray(), true);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        }

        internal static void Clear()
        {
            Destroy(ref _smg);
            Destroy(ref _shotgun);
        }

        private static void Destroy(ref Sprite sprite)
        {
            if (sprite == null) return;
            Object.Destroy(sprite.texture);
            Object.Destroy(sprite);
            sprite = null;
        }
    }
}
