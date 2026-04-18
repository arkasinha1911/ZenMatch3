using UnityEngine;

/// <summary>
/// Attach this to any Power-Up prefab (HBomb, VBomb, SBomb).
/// On Awake it finds EVERY SpriteRenderer on the root AND all children,
/// and swaps their material to the assigned "powerUpMaterial".
/// This solves the problem where the root SpriteRenderer has no sprite,
/// and the child objects (Capsule, Triangle, Hexagon, etc.) have their own
/// SpriteRenderers with actual sprites — we need the shader on THOSE renderers.
/// </summary>
public class PowerUpShaderApplier : MonoBehaviour
{
    [Tooltip("The material using one of the bomb shaders (HorizontalBomb2D, VerticalBomb2D, or ColorBomb2D).")]
    public Material powerUpMaterial;

    void Awake()
    {
        if (powerUpMaterial == null)
        {
            Debug.LogWarning($"PowerUpShaderApplier on '{gameObject.name}': No material assigned!");
            return;
        }

        // Grab every SpriteRenderer on this object AND all its children
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer sr in renderers)
        {
            // Only apply to renderers that actually have a sprite to draw
            // (skip the root's empty SpriteRenderer)
            if (sr.sprite != null)
            {
                // Use sharedMaterial to avoid creating runtime material instances
                // (all children can share the same material since shader uses _MainTex per-renderer)
                sr.material = powerUpMaterial;
            }
        }
    }
}
