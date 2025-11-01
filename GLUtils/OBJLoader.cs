using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using OpenTK.Mathematics;

namespace CrystalShrine.GLUtils
{
    // Material info with texture
    public class Material
    {
        public string Name;
        public Vector3 DiffuseColor = new(1f, 1f, 1f);
        public Vector3 EmissiveColor = new(0f, 0f, 0f);
        public Texture? Texture = null;
    }

    // Submesh with its own material
    public class SubMesh
    {
        public Mesh Mesh;
        public Material Material;

        public SubMesh(Mesh mesh, Material material)
        {
            Mesh = mesh;
            Material = material;
        }
    }

    // Model with multiple submeshes
    public class OBJModel
    {
        public List<SubMesh> SubMeshes = new();
        public Vector3 DefaultColor = new(1f, 1f, 1f);

        // Legacy support for single-material models
        public Mesh Mesh => SubMeshes.Count > 0 ? SubMeshes[0].Mesh : null;
        public Vector3 DiffuseColor => SubMeshes.Count > 0 ? SubMeshes[0].Material.DiffuseColor : DefaultColor;
        public string? TexturePath => SubMeshes.Count > 0 && SubMeshes[0].Material.Texture != null ? "multi-material" : null;
    }

    public static class OBJLoader
    {
        public static OBJModel Load(string objPath)
        {
            List<Vector3> positions = new();
            List<Vector3> normals = new();
            List<Vector2> texcoords = new();

            // Materials dictionary
            Dictionary<string, Material> materials = new();
            string objDirectory = Path.GetDirectoryName(objPath)!;

            // Look for MTL file
            string? mtlPath = null;
            foreach (var line in File.ReadAllLines(objPath))
            {
                if (line.StartsWith("mtllib"))
                {
                    string mtlFile = line.Split(' ', 2)[1].Trim();
                    mtlPath = Path.Combine(objDirectory, mtlFile);
                    break;
                }
            }

            // Fallback MTL
            if (mtlPath == null || !File.Exists(mtlPath))
            {
                string fallbackMtl = Path.ChangeExtension(objPath, ".mtl");
                if (File.Exists(fallbackMtl))
                {
                    Console.WriteLine($"[OBJ] Using fallback MTL: {fallbackMtl}");
                    mtlPath = fallbackMtl;
                }
            }

            // Parse MTL file
            if (mtlPath != null && File.Exists(mtlPath))
            {
                Console.WriteLine($"[OBJ] Reading MTL: {mtlPath}");
                ParseMTL(mtlPath, materials, objDirectory);
            }

            // Parse OBJ and split by material
            var model = new OBJModel();
            var submeshData = new Dictionary<string, (List<float> verts, List<uint> inds, Material mat)>();
            string currentMaterialName = "default";
            Material currentMaterial = new Material { Name = "default" };

            // Ensure default material exists
            if (!materials.ContainsKey("default"))
            {
                materials["default"] = currentMaterial;
            }

            foreach (var line in File.ReadAllLines(objPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                switch (parts[0])
                {
                    case "v":
                        positions.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                        break;

                    case "vt":
                        texcoords.Add(new Vector2(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            parts.Length > 2 ? 1.0f - float.Parse(parts[2], CultureInfo.InvariantCulture) : 0f));
                        break;

                    case "vn":
                        normals.Add(new Vector3(
                            float.Parse(parts[1], CultureInfo.InvariantCulture),
                            float.Parse(parts[2], CultureInfo.InvariantCulture),
                            float.Parse(parts[3], CultureInfo.InvariantCulture)));
                        break;

                    case "usemtl":
                        currentMaterialName = parts[1];
                        if (materials.ContainsKey(currentMaterialName))
                        {
                            currentMaterial = materials[currentMaterialName];
                        }
                        break;

                    case "f":
                        // Ensure submesh data exists for this material
                        if (!submeshData.ContainsKey(currentMaterialName))
                        {
                            submeshData[currentMaterialName] = (new List<float>(), new List<uint>(), currentMaterial);
                        }

                        var (verts, inds, mat) = submeshData[currentMaterialName];

                        for (int i = 1; i <= 3; i++)
                        {
                            var v = parts[i].Split('/');
                            int vi = int.Parse(v[0]) - 1;
                            int ti = v.Length > 1 && v[1] != "" ? int.Parse(v[1]) - 1 : -1;
                            int ni = v.Length > 2 && v[2] != "" ? int.Parse(v[2]) - 1 : 0;

                            var pos = positions[vi];
                            var nor = normals.Count > 0 && ni >= 0 && ni < normals.Count ? normals[ni] : Vector3.UnitY;
                            var tex = ti >= 0 && ti < texcoords.Count ? texcoords[ti] : Vector2.Zero;

                            // Add vertex: pos(3) + normal(3) + texcoord(2) + color(3)
                            verts.AddRange(new float[]
                            {
                                pos.X, pos.Y, pos.Z,
                                nor.X, nor.Y, nor.Z,
                                tex.X, tex.Y,
                                mat.DiffuseColor.X, mat.DiffuseColor.Y, mat.DiffuseColor.Z
                            });

                            inds.Add((uint)inds.Count);
                        }
                        break;
                }
            }

            Console.WriteLine($"[OBJ] Loaded {positions.Count} vertices, {texcoords.Count} texcoords, {normals.Count} normals");

            // Create submeshes
            foreach (var kvp in submeshData)
            {
                var (verts, inds, mat) = kvp.Value;
                if (inds.Count > 0)
                {
                    var mesh = new Mesh(verts.ToArray(), inds.ToArray(), hasVertexColors: true);
                    model.SubMeshes.Add(new SubMesh(mesh, mat));
                    Console.WriteLine($"[OBJ] Created submesh for material '{kvp.Key}' with {inds.Count / 3} triangles");
                }
            }

            // Set default color
            if (model.SubMeshes.Count > 0)
            {
                model.DefaultColor = model.SubMeshes[0].Material.DiffuseColor;
            }

            return model;
        }

        private static void ParseMTL(string mtlPath, Dictionary<string, Material> materials, string baseDirectory)
        {
            Material? currentMaterial = null;

            foreach (var line in File.ReadAllLines(mtlPath))
            {
                var trimmed = line.Trim();

                if (trimmed.StartsWith("newmtl"))
                {
                    string matName = trimmed.Split(' ', 2)[1].Trim();
                    currentMaterial = new Material { Name = matName };
                    materials[matName] = currentMaterial;
                }
                else if (currentMaterial != null)
                {
                    if (trimmed.StartsWith("Kd"))
                    {
                        var p = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length >= 4)
                        {
                            currentMaterial.DiffuseColor = new Vector3(
                                float.Parse(p[1], CultureInfo.InvariantCulture),
                                float.Parse(p[2], CultureInfo.InvariantCulture),
                                float.Parse(p[3], CultureInfo.InvariantCulture)
                            );
                            Console.WriteLine($"[OBJ] Material '{currentMaterial.Name}': R:{currentMaterial.DiffuseColor.X:F2} G:{currentMaterial.DiffuseColor.Y:F2} B:{currentMaterial.DiffuseColor.Z:F2}");
                        }
                    }
                    else if (trimmed.StartsWith("Ke"))
                    {
                        var p = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length >= 4)
                        {
                            currentMaterial.EmissiveColor = new Vector3(
                                float.Parse(p[1], CultureInfo.InvariantCulture),
                                float.Parse(p[2], CultureInfo.InvariantCulture),
                                float.Parse(p[3], CultureInfo.InvariantCulture)
                            );
                            Console.WriteLine($"[OBJ] Material '{currentMaterial.Name}' emissive: R:{currentMaterial.EmissiveColor.X:F2} G:{currentMaterial.EmissiveColor.Y:F2} B:{currentMaterial.EmissiveColor.Z:F2}");
                        }
                    }
                    else if (trimmed.StartsWith("map_Kd"))
                    {
                        string texFile = trimmed.Substring(6).Trim();
                        string texturePath = Path.Combine(baseDirectory, texFile);

                        Console.WriteLine($"[OBJ] Found texture reference: {texFile}");
                        Console.WriteLine($"[OBJ] Full texture path: {texturePath}");

                        if (File.Exists(texturePath))
                        {
                            try
                            {
                                currentMaterial.Texture = new Texture(texturePath);
                                Console.WriteLine($"[OBJ] ✓ Loaded texture for material '{currentMaterial.Name}'");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[OBJ] ✗ Failed to load texture: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[OBJ] ✗ Texture file NOT found!");
                        }
                    }
                }
            }
        }
    }
}