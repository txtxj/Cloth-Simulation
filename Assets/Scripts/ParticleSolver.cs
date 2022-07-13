using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleSolver : MonoBehaviour
{
    public ComputeShader compute;
    public int particleCount;
    
    private int kernelId;
    private ComputeBuffer m_ParticleBuffer;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    private void InitiateParticles(Particle[] particles)
    {
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = new Vector3(i, i, i);
            particles[i].velocity = Vector3.zero;
        }
    }

    private void Start()
    {
        m_ParticleBuffer = new ComputeBuffer(particleCount, 24);
        Particle[] particles = new Particle[particleCount];
        InitiateParticles(particles);
        m_ParticleBuffer.SetData(particles);
        kernelId = compute.FindKernel("Update");
    }

    private void Update()
    {
        compute.SetBuffer(kernelId, "Particles", m_ParticleBuffer);
        compute.SetFloat("dt", Time.deltaTime);
        compute.Dispatch(kernelId, (particleCount >> 10) + 1, 1, 1);
    }
}
