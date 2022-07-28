using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

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
    private int particleCount;
    private int groupCount;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public float isFixed;
    }

    private void InitiateParticles(Particle[] particles)
    {
        for (int i = 0; i < clothSize.x; i++)
        {
            for (int j = 0; j < clothSize.y; j++)
            {
                particles[i * clothSize.y + j].velocity = Vector3.zero; 
                particles[i * clothSize.y + j].position = new Vector3(i, 0, -j) * gapSize;
                particles[i * clothSize.y + j].force = Vector3.zero;
                particles[i * clothSize.y + j].isFixed = j == 0 && (i == 0 || i == clothSize.x - 1) ? 1f : 0f;
            }
        }
    }


    private void Start()
    {
        particleCount = clothSize.x * clothSize.y;
        Particle[] particles = new Particle[particleCount];
        InitiateParticles(particles);
        m_ParticleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        m_ParticleBuffer.SetData(particles);
        particleKernelId = compute.FindKernel("UpdateParticles");
        springKernelId = compute.FindKernel("UpdateSprings");
        compute.SetBuffer(particleKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "particles", m_ParticleBuffer);
        compute.SetFloat("dt", dt);
        compute.SetFloat("ks", ks);
        compute.SetFloat("kd", kd);
        compute.SetFloat("wass", 1f / mass);
        compute.SetVector("gravity", gravity);
        compute.SetInts("size", new int[]{clothSize.x, clothSize.y, clothSize.x * clothSize.y});
        compute.SetVector("rest", new Vector3(gapSize, gapSize * Mathf.Sqrt(2f), gapSize * 2f));
        material.SetBuffer("particleBuffer", m_ParticleBuffer);
        groupCount = (clothSize.x * clothSize.y + WARP_SIZE - 1) / WARP_SIZE;
    }

    private void Update()
    {
        compute.Dispatch(springKernelId, groupCount, 1, 1);
        compute.Dispatch(particleKernelId, groupCount, 1, 1);
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

    public void PrintSpring()
    {
        
    }
}
