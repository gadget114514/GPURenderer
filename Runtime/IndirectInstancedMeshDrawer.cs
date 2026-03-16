using UnityEngine;
using System;


namespace Sperlich.GPURender 
{
    [Serializable]
    public class RenderableData
    {
        public Mesh mesh;
        public int meshIndex;
        public Material material;
    }

    public class IndirectInstancedMeshDrawer : IDisposable
    {
        private ComputeBuffer transformBuffer;
        private ComputeBuffer argsBuffer;
        private uint[] args;
        private RenderableData data;
        private Bounds bounds;

        public IndirectInstancedMeshDrawer(RenderableData data, string name="")
        {
            this.data = data;
            this.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);

            args = new uint[5];
            args[0] = (uint)data.mesh.GetIndexCount(data.meshIndex);
            args[2] = (uint)data.mesh.GetIndexStart(data.meshIndex);
            args[3] = (uint)data.mesh.GetBaseVertex(data.meshIndex);

            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        }

        public void SetBounds(Bounds bounds)
        {
            this.bounds = bounds;
        }

        public void SetMatrices(Matrix4x4[] matrices)
        {
            if (transformBuffer != null) transformBuffer.Release();

            transformBuffer = new ComputeBuffer(matrices.Length, 16 * sizeof(float));
            transformBuffer.SetData(matrices);

            SetBuffer(transformBuffer);
        }

        public void SetBuffer(ComputeBuffer transformBuffer)
        {
            this.transformBuffer = transformBuffer;

            data.material.SetBuffer("transformBuffer", transformBuffer);

            args[1] = (uint)transformBuffer.count;

            argsBuffer.SetData(args);
        }

        public void Draw()
        {
            Graphics.DrawMeshInstancedIndirect(data.mesh, data.meshIndex, data.material, bounds, argsBuffer);
        }

        public void Dispose()
        {
            if (argsBuffer != null)
            {
                argsBuffer.Release();
                argsBuffer = null;
            }
        }
    }
}