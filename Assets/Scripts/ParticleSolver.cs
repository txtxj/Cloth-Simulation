using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

public class ParticleSolver : MonoBehaviour
{
    private const int WARP_SIZE = 1024;
    
    public ComputeShader compute;
    public Material material;
    public Vector2Int clothSize;
    public float gapSize;

    public float dt;
    public float ks;
    public float kd;
    public float mass;
    public Vector3 gravity;
    
    private int particleKernelId;
    private int springKernelId;
    private ComputeBuffer m_ParticleBuffer;
    private ComputeBuffer m_SpringBuffer;
    private int particleWarpCount;
    private int springWarpCount;
    private int particleCount;
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
        public Spring(int a, int b, Particle[] particles)
        {
            node = new Vector2Int(a, b);
            length = (particles[a].position - particles[b].position).magnitude;
        }
    }

    private void InitiateParticles(Particle[] particles)
    {
        for (int i = 0; i < clothSize.x; i++)
        {
            for (int j = 0; j < clothSize.y; j++)
            {
                particles[i * clothSize.y + j].position = new Vector3(-i, 0f, j) * gapSize;
                particles[i * clothSize.y + j].velocity = Vector3.zero;
                particles[i * clothSize.y + j].force = Vector3.zero;
                particles[i * clothSize.y + j].isFixed = j == 0 ? 1f : 0f;
            }
        }
    }

    private void InitiateSprings(List<Spring> springs, Particle[] particles)
    {
        for (int i = 0; i < clothSize.x; i++)
        {
            for (int j = 0; j < clothSize.y; j++)
            {
                int u = i * clothSize.y + j;
                if (j != clothSize.y - 1)
                {
                    springs.Add(new Spring(u, u + 1, particles));
                }
                if (i != clothSize.x - 1)
                {
                    springs.Add(new Spring(u, u + clothSize.y, particles));
                }
                if (j != clothSize.y - 1 && j != clothSize.y - 2)
                {
                    springs.Add(new Spring(u, u + 2, particles));
                }
                if (i != clothSize.x - 1 && i != clothSize.x - 2)
                {
                    springs.Add(new Spring(u, u + clothSize.y * 2, particles));
                }
                if (j != clothSize.y - 1 && i != clothSize.x - 1)
                {
                    springs.Add(new Spring(u, u + clothSize.y + 1, particles));
                }
                if (j != clothSize.y - 1 && i != 0)
                {
                    springs.Add(new Spring(u, u - clothSize.y + 1, particles));
                }
            }
        }
    }

    private void Start()
    {
        particleCount = clothSize.x * clothSize.y;
        Particle[] particles = new Particle[particleCount];
        List<Spring> springs = new List<Spring>();
        InitiateParticles(particles);
        InitiateSprings(springs, particles);
        springCount = springs.Count;
        m_ParticleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        m_SpringBuffer = new ComputeBuffer(springCount, Marshal.SizeOf(typeof(Spring)));
        m_ParticleBuffer.SetData(particles);
        m_SpringBuffer.SetData(springs);
        particleKernelId = compute.FindKernel("UpdateParticles");
        springKernelId = compute.FindKernel("UpdateSprings");
        compute.SetBuffer(particleKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "springs", m_SpringBuffer);
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
