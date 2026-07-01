using System.Collections.Generic;
using UnityEngine;

namespace Prison.Visuals
{
    /// <summary>
    /// Builds a flat-shaded low-poly humanoid mesh (~2m tall, centered at origin).
    /// Submesh 0 = skin, 1 = clothing, 2 = boots/accent.
    /// </summary>
    public static class LowPolyCharacterMeshBuilder
    {
        public const int SkinSubmesh = 0;
        public const int ClothingSubmesh = 1;
        public const int BootsSubmesh = 2;

        public static Mesh BuildHumanoidMesh(CharacterVisualRole role)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var submeshTriangles = new List<List<int>>
            {
                new List<int>(),
                new List<int>(),
                new List<int>()
            };

            // Boots
            AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(-0.13f, -0.94f, 0.03f), new Vector3(0.2f, 0.12f, 0.3f));
            AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0.13f, -0.94f, 0.03f), new Vector3(0.2f, 0.12f, 0.3f));

            // Lower legs (clothing)
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(-0.13f, -0.68f, 0f), new Vector3(0.17f, 0.44f, 0.17f));
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0.13f, -0.68f, 0f), new Vector3(0.17f, 0.44f, 0.17f));

            // Upper legs
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(-0.13f, -0.28f, 0f), new Vector3(0.19f, 0.4f, 0.19f));
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0.13f, -0.28f, 0f), new Vector3(0.19f, 0.4f, 0.19f));

            // Pelvis / hips
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0f, -0.02f, 0f), new Vector3(0.38f, 0.22f, 0.24f));

            // Torso
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0f, 0.34f, 0f), new Vector3(0.46f, 0.52f, 0.26f));

            // Lower arms (clothing sleeves)
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(-0.36f, 0.02f, 0f), new Vector3(0.12f, 0.34f, 0.12f));
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0.36f, 0.02f, 0f), new Vector3(0.12f, 0.34f, 0.12f));

            // Upper arms (clothing)
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(-0.36f, 0.38f, 0f), new Vector3(0.14f, 0.36f, 0.14f));
            AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0.36f, 0.38f, 0f), new Vector3(0.14f, 0.36f, 0.14f));

            // Hands
            AddBox(vertices, normals, submeshTriangles[SkinSubmesh], new Vector3(-0.36f, -0.2f, 0f), new Vector3(0.11f, 0.12f, 0.1f));
            AddBox(vertices, normals, submeshTriangles[SkinSubmesh], new Vector3(0.36f, -0.2f, 0f), new Vector3(0.11f, 0.12f, 0.1f));

            // Neck
            AddBox(vertices, normals, submeshTriangles[SkinSubmesh], new Vector3(0f, 0.62f, 0f), new Vector3(0.14f, 0.1f, 0.14f));

            // Head — slightly wider for readable silhouette
            AddBox(vertices, normals, submeshTriangles[SkinSubmesh], new Vector3(0f, 0.82f, 0.02f), new Vector3(0.3f, 0.32f, 0.28f));

            AddRoleExtras(role, vertices, normals, submeshTriangles);

            var mesh = new Mesh { name = $"LowPolyHumanoid_{role}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);

            mesh.subMeshCount = submeshTriangles.Count;
            for (int i = 0; i < submeshTriangles.Count; i++)
                mesh.SetTriangles(submeshTriangles[i], i);

            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddRoleExtras(
            CharacterVisualRole role,
            List<Vector3> vertices,
            List<Vector3> normals,
            List<List<int>> submeshTriangles)
        {
            switch (role)
            {
                case CharacterVisualRole.Guard:
                    // Uniform shirt layer
                    AddBox(vertices, normals, submeshTriangles[ClothingSubmesh], new Vector3(0f, 0.36f, 0f), new Vector3(0.48f, 0.46f, 0.28f));
                    // Belt
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, -0.02f, 0.14f), new Vector3(0.4f, 0.1f, 0.06f));
                    // Shoulder epaulettes
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(-0.28f, 0.52f, 0f), new Vector3(0.12f, 0.06f, 0.18f));
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0.28f, 0.52f, 0f), new Vector3(0.12f, 0.06f, 0.18f));
                    break;

                case CharacterVisualRole.Player:
                    // Highlight vest so the player reads in a crowd
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, 0.34f, 0.15f), new Vector3(0.34f, 0.4f, 0.05f));
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, 0.52f, 0f), new Vector3(0.18f, 0.08f, 0.18f));
                    break;

                case CharacterVisualRole.Prisoner:
                    // Jumpsuit chest pocket
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(-0.12f, 0.28f, 0.14f), new Vector3(0.1f, 0.12f, 0.03f));
                    // Stripe bands
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, 0.38f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f));
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, 0.24f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f));
                    AddBox(vertices, normals, submeshTriangles[BootsSubmesh], new Vector3(0f, 0.1f, 0.14f), new Vector3(0.44f, 0.05f, 0.02f));
                    break;
            }
        }

        private static void AddBox(
            List<Vector3> vertices,
            List<Vector3> normals,
            List<int> triangles,
            Vector3 center,
            Vector3 size)
        {
            Vector3 half = size * 0.5f;

            Vector3[] corners =
            {
                center + new Vector3(-half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, -half.z),
                center + new Vector3(half.x, -half.y, half.z),
                center + new Vector3(-half.x, -half.y, half.z),
                center + new Vector3(-half.x, half.y, -half.z),
                center + new Vector3(half.x, half.y, -half.z),
                center + new Vector3(half.x, half.y, half.z),
                center + new Vector3(-half.x, half.y, half.z)
            };

            void AddFace(int i0, int i1, int i2, int i3, Vector3 normal)
            {
                int baseIndex = vertices.Count;
                vertices.Add(corners[i0]);
                vertices.Add(corners[i1]);
                vertices.Add(corners[i2]);
                vertices.Add(corners[i3]);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                normals.Add(normal);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 1);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex);
                triangles.Add(baseIndex + 2);
                triangles.Add(baseIndex + 3);
            }

            AddFace(0, 1, 2, 3, Vector3.down);
            AddFace(4, 7, 6, 5, Vector3.up);
            AddFace(0, 4, 5, 1, Vector3.back);
            AddFace(2, 6, 7, 3, Vector3.forward);
            AddFace(0, 3, 7, 4, Vector3.left);
            AddFace(1, 5, 6, 2, Vector3.right);
        }
    }
}
