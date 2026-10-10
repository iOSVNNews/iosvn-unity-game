using UnityEngine;
using UnityEngine.UI;

namespace IOSVN.TuTien.Core
{
    // One appearance contract for full-body actors, portraits and cropped HUD faces.
    // Each image owns its material so editing one player cannot recolor another.
    internal sealed class CharacterAppearance : MonoBehaviour
    {
        private Material tint;

        public static void Apply(Image image, LookSpec look)
        {
            if (image == null || image.sprite == null || look == null) return;
            var owner = image.GetComponent<CharacterAppearance>() ?? image.gameObject.AddComponent<CharacterAppearance>();
            var shader = Resources.Load<Shader>("Characters/AppearanceTint");
            if (shader == null) return;
            if (owner.tint == null) owner.tint = new Material(shader) { name = "Character appearance", hideFlags = HideFlags.DontSave };
            var fullBody = image.sprite.texture.name.StartsWith("FullBodyActors");
            var inkActor = image.sprite.texture.name == "FullBodyActorsV3";
            var female = look.Get("g", "m") == "f";
            var center = FaceCenter(female, !fullBody);
            if (inkActor) center = new Vector2(female ? .43f : .51f, female ? .84f : .86f);
            owner.tint.SetVector("_Face", new Vector4(center.x, center.y, fullBody ? .072f : .11f, fullBody ? .060f : .095f));
            owner.tint.SetFloat("_HairStart", fullBody ? .75f : .69f);
            owner.tint.SetFloat("_RobeEnd", fullBody ? .74f : .69f);
            owner.tint.SetFloat("_FullBody", fullBody ? 1 : 0);
            owner.tint.SetFloat("_Female", female ? 1 : 0);
            owner.tint.SetColor("_HairTint", ColorOf(look, "hc", female ? "#d8d8e0" : "#232128"));
            owner.tint.SetColor("_RobeTint", ColorOf(look, "oc", "#2f5f63"));
            owner.tint.SetColor("_SkinTint", ColorOf(look, "sk", "#f0d2b4"));
            owner.tint.SetColor("_EyeTint", ColorOf(look, "ec", "#4d3732"));
            owner.tint.SetFloat("_EyeY", inkActor ? (female ? .861f : .889f) : fullBody ? (female ? .868f : .902f) : (female ? .840f : .824f));
            owner.tint.SetFloat("_EyeSpacing", fullBody ? .017f : .034f);
            image.material = owner.tint;
        }

        private static Color ColorOf(LookSpec look, string key, string fallback)
            => HeroSprites.ParseColor(look.Get(key, fallback), Color.white);

        internal static Vector2 FaceCenter(bool female, bool portrait)
            => portrait ? new Vector2(female ? .554f : .595f, .83f) : new Vector2(female ? .355f : .455f, female ? .86f : .89f);

        private void OnDestroy()
        {
            if (tint == null) return;
            if (Application.isPlaying) Destroy(tint); else DestroyImmediate(tint);
        }
    }
}
