using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    private ParticleSystem particleSystem;
    private ParticleSystem ParticleSystem { get { return (particleSystem == null) ? particleSystem = GetComponent<ParticleSystem>() : particleSystem; } }

    public void SetParticleColor(Color color)
    {
        var main = ParticleSystem.main;
        main.startColor = color;
    }
}
