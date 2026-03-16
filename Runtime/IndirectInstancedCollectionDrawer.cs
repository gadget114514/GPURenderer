using UnityEngine;
using System;


namespace Sperlich.GPURender
{
    public class IndirectInstancedCollectionDrawer : IDisposable
    {
        private ComputeBuffer transformBuffer;
        private IndirectInstancedMeshDrawer[] drawers;

        public IndirectInstancedCollectionDrawer(RenderableData[] dataList, string transformBufferName)
        {
            drawers = new IndirectInstancedMeshDrawer[dataList.Length];

            for (int i = 0; i < dataList.Length; i++)
            {
                drawers[i] = new IndirectInstancedMeshDrawer(dataList[i], transformBufferName);
            }
        }

        public void SetBounds(Bounds bounds)
        {
            for (int i = 0; i < drawers.Length; i++)
            {
                drawers[i].SetBounds(bounds);
            }
        }

        public void SetMatrices(Matrix4x4[] matrices)
        {
            if (transformBuffer != null) transformBuffer.Release();

            transformBuffer = new ComputeBuffer(matrices.Length, 16 * sizeof(float));
            transformBuffer.SetData(matrices);

            foreach (var drawer in drawers)
            {
                drawer.SetBuffer(transformBuffer);
            }
        }

        public void Draw()
        {
            foreach (var drawer in drawers)
            {
                drawer.Draw();
            }
        }

        public void Dispose()
        {
            if (transformBuffer != null)
            {
                transformBuffer.Release();
                transformBuffer = null;
            }

            foreach (var drawer in drawers)
            {
                drawer.Dispose();
            }
        }
    }
}