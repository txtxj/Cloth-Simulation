using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class ParticleSolver : MonoBehaviour
{
    private const int WARP_SIZE = 1024;
    
    public ComputeShader compute;
    public Material material;
    public Vector2Int clothSize;
    public float gapSize;
    public Texture texture;

    public float dt;
    public float ks;
    public float kd;
    public float mass;
    public Vector3 gravity;
    public float windStrength;
    public Vector3 windDir;

    private int particleKernelId;
    private int springKernelId;
    private int normalKernelId;
    private ComputeBuffer m_ParticleBuffer;
    private int particleCount;
    private int groupCount;

    private GraphicsBuffer indexBuffer;

    private struct Particle
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 force;
        public Vector3 normal;
        public Vector2 uv;
        public float isFixed;
    }

    private void CreateParticles(Particle[] particles)
    {
        for (int i = 0; i < clothSize.x; i++)
        {
            for (int j = 0; j < clothSize.y; j++)
            {
                int id = i * clothSize.y + j;
                particles[id].velocity = Vector3.zero; 
                particles[id].position = new Vector3(i, 0, -j) * gapSize;
                particles[id].force = Vector3.zero;
                particles[id].isFixed = j == 0 && (i == 0 || i == clothSize.x - 1) ? 1f : 0f;
                particles[id].uv = new Vector2(i / (clothSize.x - 1.0f), j / (clothSize.y - 1.0f));
            }
        }
    }

    private void InitiateParticles()
    {
        particleCount = clothSize.x * clothSize.y;
        Particle[] particles = new Particle[particleCount];
        CreateParticles(particles);
        m_ParticleBuffer = new ComputeBuffer(particleCount, Marshal.SizeOf(typeof(Particle)));
        m_ParticleBuffer.SetData(particles);
        particleKernelId = compute.FindKernel("UpdateParticles");
        springKernelId = compute.FindKernel("UpdateSprings");
        normalKernelId = compute.FindKernel("UpdateNormals");
        compute.SetBuffer(particleKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(springKernelId, "particles", m_ParticleBuffer);
        compute.SetBuffer(normalKernelId, "particles", m_ParticleBuffer);
        UpdateParams();
        material.SetBuffer("particleBuffer", m_ParticleBuffer);
        material.SetTexture("_MainTex", texture);
        groupCount = (clothSize.x * clothSize.y + WARP_SIZE - 1) / WARP_SIZE;
    }

    private void UpdateParams()
    {
        compute.SetFloat("dt", dt);
        compute.SetFloat("ks", ks);
        compute.SetFloat("kd", kd);
        compute.SetFloat("mass", mass);
        compute.SetVector("gravity", gravity);
        compute.SetInts("size", new int[]{clothSize.x, clothSize.y, clothSize.x * clothSize.y});
        compute.SetVector("rest", new Vector3(gapSize, gapSize * Mathf.Sqrt(2f), gapSize * 2f));
        compute.SetFloat("wind", windStrength);
        compute.SetVector("winddir", windDir.normalized);
    }

    private void InitiateIndexBuffer()
    {
        List<int> indices = new List<int>();
        for (int i = 1; i < clothSize.x; i++)
        {
            for (int j = 1; j < clothSize.y; j++)
            {
                int id = i * clothSize.y + j;
                indices.Add(id - clothSize.y - 1);
                indices.Add(id - clothSize.y);
                indices.Add(id);
                indices.Add(id);
                indices.Add(id - 1);
                indices.Add(id - clothSize.y - 1);
            }
        }
        indexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Index, indices.Count, sizeof(int));
        indexBuffer.SetData(indices);
    }
    
    private void Start()
    {
        InitiateParticles();
        InitiateIndexBuffer();
    }

    private void Update()
    {
        #if UNITY_EDITOR
        UpdateParams();
        #endif
        compute.Dispatch(springKernelId, groupCount, 1, 1);
        compute.Dispatch(particleKernelId, groupCount, 1, 1);
        compute.Dispatch(normalKernelId, groupCount, 1, 1);
    }

    private void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, indexBuffer, indexBuffer.count);
    }

    private void OnDestroy()
    {
        m_ParticleBuffer.Release();
        m_ParticleBuffer = null;
    }
}
