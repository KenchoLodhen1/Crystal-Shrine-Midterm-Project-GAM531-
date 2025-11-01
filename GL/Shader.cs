using OpenTK.Graphics.OpenGL4;
using System;
using System.IO;

namespace CrystalShrine.GLUtils
{
    public class Shader
    {
        public readonly int Handle;

        public Shader(string vertexPath, string fragmentPath)
        {
            string vertexCode = File.ReadAllText(vertexPath);
            string fragmentCode = File.ReadAllText(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexCode);
            GL.CompileShader(vertexShader);
            CheckCompileErrors(vertexShader, "VERTEX");

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentCode);
            GL.CompileShader(fragmentShader);
            CheckCompileErrors(fragmentShader, "FRAGMENT");

            Handle = GL.CreateProgram();
            GL.AttachShader(Handle, vertexShader);
            GL.AttachShader(Handle, fragmentShader);
            GL.LinkProgram(Handle);
            CheckCompileErrors(Handle, "PROGRAM");

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);
        }

        public void Use() => GL.UseProgram(Handle);

        public void SetInt(string name, int value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1) GL.Uniform1(location, value);
        }

        public void SetBool(string name, bool value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1) GL.Uniform1(location, value ? 1 : 0);
        }

        public void SetFloat(string name, float value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1) GL.Uniform1(location, value);
        }

        public void SetVector3(string name, OpenTK.Mathematics.Vector3 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1) GL.Uniform3(location, value);
        }

        public void SetMatrix4(string name, OpenTK.Mathematics.Matrix4 value)
        {
            int location = GL.GetUniformLocation(Handle, name);
            if (location != -1) GL.UniformMatrix4(location, false, ref value);
        }

        private static void CheckCompileErrors(int obj, string type)
        {
            if (type != "PROGRAM")
            {
                GL.GetShader(obj, ShaderParameter.CompileStatus, out int success);
                if (success == 0)
                {
                    string info = GL.GetShaderInfoLog(obj);
                    Console.WriteLine($"ERROR::SHADER_COMPILATION_ERROR of type: {type}\n{info}");
                }
            }
            else
            {
                GL.GetProgram(obj, GetProgramParameterName.LinkStatus, out int success);
                if (success == 0)
                {
                    string info = GL.GetProgramInfoLog(obj);
                    Console.WriteLine($"ERROR::PROGRAM_LINKING_ERROR\n{info}");
                }
            }
        }
    }
}