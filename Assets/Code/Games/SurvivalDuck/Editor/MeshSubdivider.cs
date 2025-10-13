using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace SurvivalDuck.Editor
{
    public class MeshSubdivider : EditorWindow
    {
        private GameObject selectedObject;
        private int subdivisionLevel = 2;
        private bool createNewAsset = true;
        
        [MenuItem("Tools/Survival Duck/Mesh Subdivider")]
        public static void ShowWindow()
        {
            GetWindow<MeshSubdivider>("Mesh Subdivider");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("Mesh Subdivision Tool", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                "This tool subdivides meshes to make them smoother for the curved world effect. " +
                "Higher subdivision = more polygons = better curves but worse performance.", 
                MessageType.Info
            );
            
            GUILayout.Space(10);
            
            selectedObject = (GameObject)EditorGUILayout.ObjectField("Target Object", selectedObject, typeof(GameObject), true);
            subdivisionLevel = EditorGUILayout.IntSlider("Subdivision Level", subdivisionLevel, 1, 4);
            createNewAsset = EditorGUILayout.Toggle("Create New Asset", createNewAsset);
            
            GUILayout.Space(10);
            
            EditorGUILayout.HelpBox(
                $"Level {subdivisionLevel} will multiply triangles by {Mathf.Pow(4, subdivisionLevel):F0}x", 
                MessageType.Warning
            );
            
            GUILayout.Space(10);
            
            GUI.enabled = selectedObject != null;
            if (GUILayout.Button("Subdivide Mesh", GUILayout.Height(40)))
            {
                SubdivideMesh();
            }
            
            if (GUILayout.Button("Subdivide All Children", GUILayout.Height(40)))
            {
                SubdivideAllChildren();
            }
            GUI.enabled = true;
        }
        
        private void SubdivideMesh()
        {
            MeshFilter meshFilter = selectedObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected object has no MeshFilter component!", "OK");
                return;
            }
            
            Mesh originalMesh = meshFilter.sharedMesh;
            if (originalMesh == null)
            {
                EditorUtility.DisplayDialog("Error", "MeshFilter has no mesh!", "OK");
                return;
            }
            
            Mesh subdividedMesh = SubdivideMeshInternal(originalMesh, subdivisionLevel);
            
            if (createNewAsset)
            {
                string path = EditorUtility.SaveFilePanelInProject(
                    "Save Subdivided Mesh",
                    originalMesh.name + "_subdivided",
                    "asset",
                    "Save subdivided mesh as..."
                );
                
                if (!string.IsNullOrEmpty(path))
                {
                    AssetDatabase.CreateAsset(subdividedMesh, path);
                    AssetDatabase.SaveAssets();
                    meshFilter.sharedMesh = subdividedMesh;
                    EditorUtility.DisplayDialog("Success", $"Subdivided mesh saved to {path}", "OK");
                }
            }
            else
            {
                meshFilter.mesh = subdividedMesh;
                EditorUtility.DisplayDialog("Success", "Mesh subdivided (runtime only, not saved)", "OK");
            }
        }
        
        private void SubdivideAllChildren()
        {
            MeshFilter[] meshFilters = selectedObject.GetComponentsInChildren<MeshFilter>();
            
            if (meshFilters.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No MeshFilter components found in children!", "OK");
                return;
            }
            
            int count = 0;
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh != null)
                {
                    Mesh subdividedMesh = SubdivideMeshInternal(meshFilter.sharedMesh, subdivisionLevel);
                    
                    if (createNewAsset)
                    {
                        string path = $"Assets/Games/Survival/Meshes/{meshFilter.sharedMesh.name}_subdivided.asset";
                        System.IO.Directory.CreateDirectory("Assets/Games/Survival/Meshes");
                        
                        AssetDatabase.CreateAsset(subdividedMesh, path);
                        meshFilter.sharedMesh = subdividedMesh;
                    }
                    else
                    {
                        meshFilter.mesh = subdividedMesh;
                    }
                    count++;
                }
            }
            
            if (createNewAsset)
            {
                AssetDatabase.SaveAssets();
            }
            
            EditorUtility.DisplayDialog("Success", $"Subdivided {count} meshes!", "OK");
        }
        
        private Mesh SubdivideMeshInternal(Mesh originalMesh, int level)
        {
            Mesh mesh = Instantiate(originalMesh);
            
            for (int i = 0; i < level; i++)
            {
                mesh = SubdivideOnce(mesh);
            }
            
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            
            return mesh;
        }
        
        private Mesh SubdivideOnce(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            int[] triangles = mesh.triangles;
            
            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<Vector3> newNormals = new List<Vector3>(normals);
            List<Vector2> newUVs = new List<Vector2>(uvs);
            List<int> newTriangles = new List<int>();
            
            Dictionary<string, int> midpointCache = new Dictionary<string, int>();
            
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int i0 = triangles[i];
                int i1 = triangles[i + 1];
                int i2 = triangles[i + 2];
                
                int m01 = GetMidpointIndex(i0, i1, vertices, normals, uvs, newVertices, newNormals, newUVs, midpointCache);
                int m12 = GetMidpointIndex(i1, i2, vertices, normals, uvs, newVertices, newNormals, newUVs, midpointCache);
                int m20 = GetMidpointIndex(i2, i0, vertices, normals, uvs, newVertices, newNormals, newUVs, midpointCache);
                
                // Create 4 new triangles
                newTriangles.AddRange(new[] { i0, m01, m20 });
                newTriangles.AddRange(new[] { i1, m12, m01 });
                newTriangles.AddRange(new[] { i2, m20, m12 });
                newTriangles.AddRange(new[] { m01, m12, m20 });
            }
            
            Mesh newMesh = new Mesh();
            newMesh.name = mesh.name + "_subdivided";
            
            if (newVertices.Count > 65535)
            {
                newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }
            
            newMesh.vertices = newVertices.ToArray();
            newMesh.normals = newNormals.ToArray();
            newMesh.uv = newUVs.ToArray();
            newMesh.triangles = newTriangles.ToArray();
            
            return newMesh;
        }
        
        private int GetMidpointIndex(
            int i0, int i1,
            Vector3[] vertices, Vector3[] normals, Vector2[] uvs,
            List<Vector3> newVertices, List<Vector3> newNormals, List<Vector2> newUVs,
            Dictionary<string, int> cache)
        {
            string key = i0 < i1 ? $"{i0}_{i1}" : $"{i1}_{i0}";
            
            if (cache.TryGetValue(key, out int index))
            {
                return index;
            }
            
            Vector3 midVertex = (vertices[i0] + vertices[i1]) * 0.5f;
            Vector3 midNormal = ((normals[i0] + normals[i1]) * 0.5f).normalized;
            Vector2 midUV = (uvs[i0] + uvs[i1]) * 0.5f;
            
            newVertices.Add(midVertex);
            newNormals.Add(midNormal);
            newUVs.Add(midUV);
            
            int newIndex = newVertices.Count - 1;
            cache[key] = newIndex;
            
            return newIndex;
        }
    }
}

