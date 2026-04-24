using UnityEngine;

/// <summary>
/// Handles spawning specific particle effects when special matches are made!
/// </summary>
public class MatchEffectsManager : MonoBehaviour
{
    public static MatchEffectsManager Instance { get; private set; }

    [Header("Match Particle Effects")]
    [Tooltip("Particle effect spawned when a standard Match-3 occurs.")]
    public GameObject match3ParticlePrefab;
    
    [Tooltip("Particle effect spawned when a Horizontal Bomb is created (Match-4).")]
    public GameObject horizontalBombCreateParticle;
    
    [Tooltip("Particle effect spawned when a Vertical Bomb is created (Match-4).")]
    public GameObject verticalBombCreateParticle;
    
    [Tooltip("Particle effect spawned when a Shape Bomb / Disco Ball is created (2x2 Match).")]
    public GameObject shapeBombCreateParticle;

    [Header("Bomb Detonation Effects")]
    [Tooltip("Particle effect spawned when a Horizontal Bomb detonates.")]
    public GameObject horizontalBombExplodeParticle;

    [Tooltip("Particle effect spawned when a Vertical Bomb detonates.")]
    public GameObject verticalBombExplodeParticle;

    [Tooltip("Particle effect spawned when a Shape Bomb / Disco Ball detonates.")]
    public GameObject shapeBombExplodeParticle;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMatch3Effect(Vector3 position)
    {
        if (match3ParticlePrefab != null)
        {
            Instantiate(match3ParticlePrefab, position, Quaternion.identity);
        }
    }

    public void PlayHorizontalBombCreateEffect(Vector3 position)
    {
        if (horizontalBombCreateParticle != null)
        {
            Instantiate(horizontalBombCreateParticle, position, Quaternion.identity);
        }
    }

    public void PlayVerticalBombCreateEffect(Vector3 position)
    {
        if (verticalBombCreateParticle != null)
        {
            Instantiate(verticalBombCreateParticle, position, Quaternion.identity);
        }
    }

    public void PlayShapeBombCreateEffect(Vector3 position)
    {
        if (shapeBombCreateParticle != null)
        {
            Instantiate(shapeBombCreateParticle, position, Quaternion.identity);
        }
    }

    public void PlayHorizontalBombExplodeEffect(Vector3 position)
    {
        if (horizontalBombExplodeParticle != null)
        {
            Instantiate(horizontalBombExplodeParticle, position, Quaternion.identity);
        }
    }

    public void PlayVerticalBombExplodeEffect(Vector3 position)
    {
        if (verticalBombExplodeParticle != null)
        {
            Instantiate(verticalBombExplodeParticle, position, Quaternion.identity);
        }
    }

    public void PlayShapeBombExplodeEffect(Vector3 position)
    {
        if (shapeBombExplodeParticle != null)
        {
            Instantiate(shapeBombExplodeParticle, position, Quaternion.identity);
        }
    }
}
