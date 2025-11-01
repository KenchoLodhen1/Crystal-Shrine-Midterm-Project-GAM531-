using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace CrystalShrine.GLUtils
{
    public class FloatingIsland : IDisposable
    {
        private int _vao, _vbo, _ebo;
        private int _indexCount;

        // Store terrain data for collision detection
        private float[,] _heightMap;
        private int _resolution;
        private float _size;
        private float _halfSize;

        public FloatingIsland(int resolution = 180, float size = 150f, float height = 2f)
        {
            _resolution = resolution;
            _size = size;
            _halfSize = size / 2f;
            _heightMap = new float[resolution + 1, resolution + 1];

            Generate(resolution, size, height);
        }

        public float GetHalfSize()
        {
            return _halfSize;
        }

        private void Generate(int resolution, float size, float height)
        {
            var verts = new List<float>();
            var inds = new List<uint>();

            float half = size / 2f;
            Random rng = new Random(42);

            // Generate vertices with organic island shape
            for (int z = 0; z <= resolution; z++)
            {
                for (int x = 0; x <= resolution; x++)
                {
                    float fx = x / (float)resolution;
                    float fz = z / (float)resolution;

                    float wx = MathHelper.Lerp(-half, half, fx);
                    float wz = MathHelper.Lerp(-half, half, fz);

                    // Calculate distance from center
                    float dist = MathF.Sqrt(wx * wx + wz * wz) / half;

                    // Add some variation to the island radius for organic shape
                    float angleVariation = Noise(wx * 0.02f, wz * 0.02f, rng) * 0.3f;
                    float adjustedDist = dist + angleVariation;
                    adjustedDist = MathHelper.Clamp(adjustedDist, 0f, 1.2f);

                    // Smooth island edge falloff
                    float edgeFalloff = adjustedDist < 0.85f ? 1f : MathF.Pow(MathHelper.Clamp((1.2f - adjustedDist) / 0.35f, 0f, 1f), 2f);

                    // If completely outside island bounds, skip or set to zero
                    if (adjustedDist > 1.2f)
                        edgeFalloff = 0f;

                    // 🔸 Base flat height
                    float baseHeight = height * 0.5f * edgeFalloff;

                    // 🔸 Very subtle scattered variations
                    float noiseHeight = 0f;

                    // Fine bumps and dips
                    noiseHeight += height * Noise(wx * 0.4f, wz * 0.4f, rng) * 0.15f;

                    // Medium scattered details
                    noiseHeight += height * Noise(wx * 0.25f, wz * 0.25f, rng) * 0.2f;

                    // Subtle larger variations
                    noiseHeight += height * Noise(wx * 0.1f, wz * 0.1f, rng) * 0.12f;

                    // Very fine details
                    noiseHeight += height * Noise(wx * 0.6f, wz * 0.6f, rng) * 0.08f;

                    // Combine
                    float y = baseHeight + (noiseHeight * edgeFalloff);
                    y = MathHelper.Clamp(y, 0f, height * 1.5f);

                    // Store in height map for collision
                    _heightMap[z, x] = y;

                    verts.Add(wx);
                    verts.Add(y);
                    verts.Add(wz);

                    // Simple upward normal
                    verts.Add(0f);
                    verts.Add(1f);
                    verts.Add(0f);

                    // Texture coordinates
                    verts.Add(fx * 15f);
                    verts.Add(fz * 15f);
                }
            }

            // Create indices
            for (int z = 0; z < resolution; z++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    uint topLeft = (uint)(z * (resolution + 1) + x);
                    uint topRight = topLeft + 1;
                    uint bottomLeft = topLeft + (uint)(resolution + 1);
                    uint bottomRight = bottomLeft + 1;

                    inds.Add(topLeft);
                    inds.Add(bottomLeft);
                    inds.Add(topRight);

                    inds.Add(topRight);
                    inds.Add(bottomLeft);
                    inds.Add(bottomRight);
                }
            }

            _indexCount = inds.Count;

            // Upload to GPU
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, verts.Count * sizeof(float), verts.ToArray(), BufferUsageHint.StaticDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, inds.Count * sizeof(uint), inds.ToArray(), BufferUsageHint.StaticDraw);

            int stride = 8 * sizeof(float);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
            GL.EnableVertexAttribArray(2);

            GL.BindVertexArray(0);
        }

        // Get terrain height at world position (x, z)
        public float GetHeightAt(float worldX, float worldZ)
        {
            // Convert world coordinates to grid coordinates
            float gridX = (worldX + _halfSize) / _size * _resolution;
            float gridZ = (worldZ + _halfSize) / _size * _resolution;

            // Clamp to valid range
            if (gridX < 0 || gridX >= _resolution || gridZ < 0 || gridZ >= _resolution)
                return 0f; // Outside island bounds

            // Get integer indices
            int x0 = (int)MathF.Floor(gridX);
            int z0 = (int)MathF.Floor(gridZ);
            int x1 = Math.Min(x0 + 1, _resolution);
            int z1 = Math.Min(z0 + 1, _resolution);

            // Get fractional part for interpolation
            float fx = gridX - x0;
            float fz = gridZ - z0;

            // Bilinear interpolation
            float h00 = _heightMap[z0, x0];
            float h10 = _heightMap[z0, x1];
            float h01 = _heightMap[z1, x0];
            float h11 = _heightMap[z1, x1];

            float h0 = MathHelper.Lerp(h00, h10, fx);
            float h1 = MathHelper.Lerp(h01, h11, fx);

            return MathHelper.Lerp(h0, h1, fz);
        }

        private float Noise(float x, float z, Random rng)
        {
            int xi = (int)MathF.Floor(x);
            int zi = (int)MathF.Floor(z);

            float xf = x - xi;
            float zf = z - zi;

            float u = xf * xf * (3f - 2f * xf);
            float v = zf * zf * (3f - 2f * zf);

            float a = Hash(xi, zi);
            float b = Hash(xi + 1, zi);
            float c = Hash(xi, zi + 1);
            float d = Hash(xi + 1, zi + 1);

            return MathHelper.Lerp(
                MathHelper.Lerp(a, b, u),
                MathHelper.Lerp(c, d, u),
                v
            );
        }

        private float Hash(int x, int z)
        {
            int n = x + z * 57;
            n = (n << 13) ^ n;
            return 1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f;
        }

        public void Draw()
        {
            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, 0);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
        }
    }
}