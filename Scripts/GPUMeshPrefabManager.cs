using UnityEngine;
  using Sperlich.GPURender;
  using System.Collections.Generic;
  
public class GPUMeshPrefabManager : MonoBehaviour
{
    [System.Serializable]
    public class Prototype {
        public GameObject prefab;
        public bool useIndirect;
        
        [HideInInspector] public MeshSet[] meshsets;
        [HideInInspector] public Collection collection;
        [HideInInspector] public List<IndirectInstancedCollectionDrawer> indirectDrawers = new();
    }

    public Prototype[] prototypes;
	int count = 100;

    public void LoadPrototype()
    {
        foreach (var proto in prototypes) {
            if (proto.prefab == null) continue;

            MeshFilter[] myMeshs = proto.prefab.GetComponentsInChildren<MeshFilter>();
            proto.meshsets = new MeshSet[myMeshs.Length];
            proto.collection = new Collection();
            
            for (int j = 0; j < myMeshs.Length; j++) {
                MeshFilter filter = myMeshs[j];
                MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                
                proto.meshsets[j] = new MeshSet(filter.sharedMesh, renderer.sharedMaterials, filter.gameObject.layer);
                
                if (proto.useIndirect) {
                    // Create a drawer for each MeshFilter to handle its submeshes/materials
                    RenderableData[] submeshDataList = new RenderableData[proto.meshsets[j].SubMeshCount];
                    for (int s = 0; s < proto.meshsets[j].SubMeshCount; s++) {
                        submeshDataList[s] = new RenderableData {
                            mesh = proto.meshsets[j].mesh,
                            meshIndex = s,
                            material = proto.meshsets[j].materials[s]
                        };
                    }
                    
                    var drawer = new IndirectInstancedCollectionDrawer(submeshDataList, "transformBuffer");
                    proto.indirectDrawers.Add(drawer);
                }
            }
        }
    }

    public class GPUMeshTRS:ScriptableObject {
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;
    
    };
 
	public void InstantiateMesh(int idx, GPUMeshTRS[] trss) {
        if (idx < 0 || idx >= prototypes.Length) return;
        var proto = prototypes[idx];

        if (proto.useIndirect) {
            Matrix4x4[] matrices = new Matrix4x4[trss.Length];
            for (int i = 0; i < trss.Length; i++) {
                matrices[i] = Matrix4x4.TRS(trss[i].position, trss[i].rotation, trss[i].scale);
            }

            foreach (var drawer in proto.indirectDrawers) {
                drawer.SetMatrices(matrices);
            }
        } else {
            for (int x = 0; x < trss.Length; x++) {
                for (int k = 0; k < proto.meshsets.Length; k++) {
                    GPUMesh gpuMesh = new GPUMesh(proto.meshsets[k], proto.collection);
                    gpuMesh.Rotation = trss[x].rotation;
                    gpuMesh.Position = trss[x].position;
                    gpuMesh.Scale = trss[x].scale;
                    gpuMesh.Enable();
                }
            }
        }
	}

    public void TestInstantiateMesh() {
        if (prototypes.Length == 0) return;
        
        GPUMeshTRS[] trss = new GPUMeshTRS[count * count];
        for (int x = 0; x < count; x++) {
            for (int y = 0; y < count; y++) {
                trss[x * count + y] = ScriptableObject.CreateInstance<GPUMeshTRS>();
                trss[x * count + y].position = new Vector3(x * 3, 0, y * 3);
                trss[x * count + y].rotation = Quaternion.identity;
                trss[x * count + y].scale = Vector3.one;
            }
        }
        InstantiateMesh(0, trss);
    }

	void Start() {
		LoadPrototype();
		TestInstantiateMesh();
	}

    void Update()
    {
        foreach (var proto in prototypes) {
            if (proto.useIndirect) {
                foreach (var drawer in proto.indirectDrawers) {
                    drawer.Draw();
                }
            }
        }
    }

    void OnDestroy()
    {
        foreach (var proto in prototypes) {
            foreach (var drawer in proto.indirectDrawers) {
                drawer.Dispose();
            }
            proto.indirectDrawers.Clear();
        }
    }
}
