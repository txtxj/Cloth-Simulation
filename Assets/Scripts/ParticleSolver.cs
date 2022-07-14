using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UIElements;

public class ParticleSolver : MonoBehaviour
{
    private const int WARP_SIZE = 1024;
    
    public ComputeShader compute;
    public Material material;
    public int particleCount;
    
    public float dt;
    public float ks;
    public float kd;
    public float mass;
    public Vector3 gravity;
    
    private int particleKernelId;
    private int springKernelId;
    private ComputeBuffer m_ParticleBuffer;
    private ComputeBuffer m_SpingBuffer;
    private int particleWarpCount;
    private int springWarpCount;
    private int springCount;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float isFixed;
    }

    private struct Spring
    {
        public Vector2Int node;
        public float length;
    }

    private void InitiateParticles(Particle[] particles)
    {
        for (int i = 0; i < particleCount; i++)
        {
            particles[i].position = new Vector3(i, 0f, 0f);
            particles[i].velocity = Vector3.zero;
            particles[i].force = Vector3.zero;
            particles[i].isFixed = i == 0 ? 1f : 0f;
        }
    }

    private void InitiateSprings(Spring[] springs, Particle[] particles)
    {
        for (int i = 0; i < springCount; i++)
        {
            springs[i].node = new Vector2Int(i, i + 1);
            springs[i].length = (particles[springs[i].node.x].position - particles[springs[i].node.y].position).magnitude;
        }
    }

    private void Start()
    {
        springCount = particleCount - 1;
        m_ParticleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        m_SpingBuffer = new ComputeBuffer(springCount, Marshal.SizeOf(typeof(Spring)));
        Particle[] particles = new Particle[particleCount];
        Spring[] springs = new Spring[springCount];
        InitiateParticles(particles);
        InitiateSprings(springs, particles);
        m_ParticleBuffer.SetData(particles);
        m_SpingBuffer.SetData(springs);
        particleKernelId = compute.FindKernel("UpdateParticles");
        springKernelId = compute.FindKernel("UpdateSprings");
        compute.SetBuffer(particleKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "springs", m_SpingBuffer);
        compute.SetFloat("dt", dt);
        compute.SetFloat("ks", ks);
        compute.SetFloat("kd", kd);
        compute.SetFloat("wass", 1f / mass);
        compute.SetVector("gravity", gravity);
        material.SetBuffer("particleBuffer", m_ParticleBuffer);
        particleWarpCount = Mathf.CeilToInt((float)particleCount / WARP_SIZE);
        springWarpCount = Mathf.CeilToInt((float)springCount / WARP_SIZE);
    }

    private void Update()
    {
        compute.Dispatch(springKernelId, springWarpCount, 1, 1);
        compute.Dispatch(particleKernelId, particleWarpCount, 1, 1);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, particleCount);
    }

    private void OnDestroy()
    {
        //m_ParticleBuffer.Release();
        //m_ParticleBuffer = null;
    }
}
