using DesktopPet.Shop;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace DesktopPet.Decor
{
    /// <summary>装饰手持/虚影创建与着色；Luby 虚影复用 Strip / Tint。</summary>
    internal static class DecorHoldVisuals
    {
        public static GameObject Create(
            ShopItemDefinition item,
            Transform parent,
            string name,
            Color color,
            int sortingOrder,
            bool startActive)
        {
            GameObject go = DecorPrefabUtil.InstantiateDecor(item, parent);
            if (go == null)
                return null;

            go.name = name;
            Collider2D[] cols = go.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }

            PlacedDecor pd = go.GetComponent<PlacedDecor>();
            if (pd != null)
                Object.Destroy(pd);

            ParticleSystem[] particles = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < particles.Length; i++)
            {
                if (particles[i] != null)
                    Object.Destroy(particles[i].gameObject);
            }

            DecorNightLight[] nightLights = go.GetComponentsInChildren<DecorNightLight>(true);
            for (int i = 0; i < nightLights.Length; i++)
            {
                if (nightLights[i] != null)
                    Object.Destroy(nightLights[i]);
            }

            IDecorShortClickHandler[] shortClickHandlers =
                go.GetComponentsInChildren<IDecorShortClickHandler>(true);
            for (int i = 0; i < shortClickHandlers.Length; i++)
            {
                if (shortClickHandlers[i] is Component c && c != null)
                    Object.Destroy(c);
            }

            DecorInteractable[] interactables = go.GetComponentsInChildren<DecorInteractable>(true);
            for (int i = 0; i < interactables.Length; i++)
            {
                if (interactables[i] != null)
                    Object.Destroy(interactables[i]);
            }

            Light2D[] lights = go.GetComponentsInChildren<Light2D>(true);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    Object.Destroy(lights[i].gameObject);
            }

            SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                renderers[i].sortingOrder = sortingOrder;
                renderers[i].color = color;
            }

            go.SetActive(startActive);
            return go;
        }

        /// <summary>Luby 等非 DecorHoldVisuals.Create 的虚影：关碰撞与 Behaviour。</summary>
        public static void StripForGhost(GameObject go)
        {
            if (go == null)
                return;

            Collider2D[] cols = go.GetComponentsInChildren<Collider2D>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                if (cols[i] != null)
                    cols[i].enabled = false;
            }

            Behaviour[] behaviours = go.GetComponentsInChildren<Behaviour>(true);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null)
                    behaviours[i].enabled = false;
            }
        }

        public static void Tint(SpriteRenderer[] renderers, Color color)
        {
            if (renderers == null)
                return;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].color = color;
            }
        }

        public static void ApplyPlacementTint(SpriteRenderer[] renderers, bool valid, float alpha)
        {
            Color c = valid
                ? new Color(1f, 1f, 1f, alpha)
                : new Color(1f, 0.35f, 0.35f, alpha);
            Tint(renderers, c);
        }

        public static void Destroy(ref GameObject go)
        {
            if (go == null)
                return;
            Object.Destroy(go);
            go = null;
        }
    }
}
