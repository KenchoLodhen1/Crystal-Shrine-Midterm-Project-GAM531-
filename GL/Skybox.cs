using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;

namespace CrystalShrine.GLUtils
{
    public class Skybox : IDisposable
    {
        private readonly int _vao;
        private readonly int _vbo;
        private readonly int _texture;
        private readonly Shader _shader;

        public Skybox(string texturePath, string vertexShaderPath, string fragmentShaderPath)
        {
            float[] skyVertices = {
                -1f,  1f, -1f,
                -1f, -1f, -1f,
                 1f, -1f, -1f,
                 1f, -1f, -1f,
                 1f,  1f, -1f,
                -1f,  1f, -1f,

                -1f, -1f,  1f,
                -1f, -1f, -1f,
                -1f,  1f, -1f,
                -1f,  1f, -1f,
                -1f,  1f,  1f,
                -1f, -1f,  1f,

                 1f, -1f, -1f,
                 1f, -1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f, -1f,
                 1f, -1f, -1f,

                -1f, -1f,  1f,
                -1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                 1f, -1f,  1f,
                -1f, -1f,  1f,

                -1f,  1f, -1f,
                 1f,  1f, -1f,
                 1f,  1f,  1f,
                 1f,  1f,  1f,
                -1f,  1f,  1f,
                -1f,  1f, -1f,

                -1f, -1f, -1f,
                -1f, -1f,  1f,
                 1f, -1f, -1f,
                 1f, -1f, -1f,
                -1f, -1f,  1f,
                 1f, -1f,  1f
            };

            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();

            GL.BindVertexArray(_vao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, skyVertices.Length * sizeof(float), skyVertices, BufferUsageHint.StaticDraw);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0);

            // Load texture
            _texture = TextureLoader.Load(texturePath);

            // Load shaders
            _shader = new Shader(vertexShaderPath, fragmentShaderPath);
            _shader.Use();
            _shader.SetInt("skyTexture", 0);
        }

        public void Draw(Matrix4 view, Matrix4 projection)
        {
            GL.DepthMask(false);
            GL.DepthFunc(DepthFunction.Lequal);

            _shader.Use();
            _shader.SetMatrix4("view", new Matrix4(new Matrix3(view)));
            _shader.SetMatrix4("projection", projection);

            GL.BindVertexArray(_vao);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.DrawArrays(PrimitiveType.Triangles, 0, 36);

            GL.DepthFunc(DepthFunction.Less);
            GL.DepthMask(true);
        }

        public void Dispose()
        {
            GL.DeleteVertexArray(_vao);
            GL.DeleteBuffer(_vbo);
            GL.DeleteTexture(_texture);
        }
    }

    public static class TextureLoader
    {
        public static int Load(string path)
        {
            using (var image = new System.Drawing.Bitmap(path))
            {
                int tex = GL.GenTexture();
                GL.BindTexture(TextureTarget.Texture2D, tex);

                image.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipY);
                var data = image.LockBits(
                    new System.Drawing.Rectangle(0, 0, image.Width, image.Height),
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                    data.Width, data.Height, 0,
                    OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                    PixelType.UnsignedByte, data.Scan0);

                image.UnlockBits(data);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

                GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
                return tex;
            }
        }
    }
}
