using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleSolver : MonoBehaviour
{
    private const int WARP_SIZE = 1024;
    
    public ComputeShader compute;
    public Material material;
    public int particleCount;
    
    private int kernelId;
    private ComputeBuffer m_ParticleBuffer;
    private int warpCount;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
    }

    private void InitiateParticles(Particle[] particles)
    {
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = Vector3.one * i;
            particles[i].velocity = Vector3.zero;
        }
    }

    private void Start()
    {
        m_ParticleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        Particle[] particles = new Particle[particleCount];
        InitiateParticles(particles);
        m_ParticleBuffer.SetData(particles);
        kernelId = compute.FindKernel("Update");
        compute.SetBuffer(kernelId, "Particles", m_ParticleBuffer);
        material.SetBuffer("particleBuffer", m_ParticleBuffer);
        warpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);
    }

    private void Update()
    {
        compute.SetFloat("dt", Time.deltaTime);
        compute.Dispatch(kernelId, warpCount, 1, 1);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, particleCount);
    }

    private void OnDestroy()
    {
        m_ParticleBuffer.Release();
        m_ParticleBuffer = null;
    }
}
