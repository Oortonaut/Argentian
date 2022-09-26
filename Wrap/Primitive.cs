using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;

namespace Argentian.Wrap {
    public interface IPrimitive {
        // Debugging and reporting
        public string Name { get; }
        public IShaderProgram Shader { get; }
        // User must order passes
        public long Order => Shader.Order;
        // Called once per distinct primitive
        public void SetupGeometry();
        public void Draw();
    }
    public class Primitive : Disposable, IPrimitive {
        public enum Winding {
            CW = -1, // DirectX
            Either = 0, // Ignore face culling
            CCW = 1, // OpenGL
        }
        public record VertexAttrib(
            string name,
            uint width, // number of element
            uint offset, // word
            VertexAttribType type = VertexAttribType.Float,
            bool normalized = false
        ) {}
        public class StreamDef {
            public string name = "";
            public uint stride;
            public uint divisor;
            public List<VertexAttrib> attributes = new();
        }
        public class Def {
            public PrimitiveType primitiveType = PrimitiveType.Triangles;
            public List<StreamDef> streams = new();
            public Winding windingMode = Winding.CW;
        }
        public Def format;
        //--- Data -----------
        Buffer[] vertexBuffer;
        int vertexBase = 0;
        Buffer indexBuffer;
        int indexBase = 0;
        int indexCount = -1;
        int instanceBase = 0;
        int instanceCount = 1;
        public readonly ShaderProgram shader;
        public Primitive(
            string name_, 
            ShaderProgram shader_, 
            Def format_,
            Buffer[] vb, 
            Buffer ib) :
            base(name_) {
            shader = shader_;
            format = format_;
            vertexBuffer = vb;
            indexBuffer = ib;
            handle = GL.CreateVertexArray();
            GL.ObjectLabel(ObjectIdentifier.VertexArray, ( uint ) handle.Handle, Name.Length, Name);
            uint streamIndex = 0;
            uint attrIndex = 0;
            foreach (var stream in format.streams) {
                GL.VertexArrayBindingDivisor(handle, streamIndex, stream.divisor);
                foreach (var attr in stream.attributes) {
                    GL.EnableVertexArrayAttrib(handle, attrIndex);
                    GL.VertexArrayAttribBinding(handle, attrIndex, streamIndex);
                    GL.VertexArrayAttribFormat(handle, attrIndex, (int)attr.width, attr.type, attr.normalized, attr.offset);
                    attrIndex++;
                }
                streamIndex++;
            }
        }
        public void VertexBase(int vertexBase_ = 0) {
            vertexBase = vertexBase_;
        }
        public void IndexRange(int indexBase_ = 0, int indexCount_ = -1) {
            indexBase = indexBase_;
            indexCount = indexCount_;
        }
        public void Instance(int instanceBase_ = 0, int instanceCount_ = 1) {
            instanceBase = instanceBase_;
            instanceCount = instanceCount_;
        }
        public void Instance(int instanceCount) => Instance(0, instanceCount);

        public VertexArrayHandle handle;
        protected override void Delete() {
            GL.DeleteVertexArray(handle);
        }
        public override string ToString() => $"VertexArray {handle.Handle} '{Name}'{DisposedString}";
        string IPrimitive.Name => base.Name;
        public void SetupGeometry() {
            // Bind source data to vertex array object
            uint streamIndex = 0;
            foreach (var stream in format.streams) {
                GL.VertexArrayVertexBuffer(
                    handle,
                    streamIndex, 
                    vertexBuffer[streamIndex].handle,
                    IntPtr.Zero,
                    vertexBuffer[streamIndex].stride);
                streamIndex++;
            }
            GL.VertexArrayElementBuffer(handle, indexBuffer.handle);
        }
        public IShaderProgram Shader => shader;
        public long Order => shader.Order;
        public void Draw() {
            //sgader.SetUniform("time", () => (double)sw.ElapsedTicks / (double)Stopwatch.Frequency);
            shader.Bind();
            GL.BindVertexArray(handle);
            GL.DrawElementsInstancedBaseVertex(
                format.primitiveType,
                Count,
                indexBuffer.stride switch {
                    1 => DrawElementsType.UnsignedByte,
                    2 => DrawElementsType.UnsignedShort,
                    4 => DrawElementsType.UnsignedInt,
                    _ => throw new InvalidOperationException("Indexbuffer stride must be 1, 2, or 4")
                }, indexBase, instanceCount, vertexBase);
        }
        public int Count => indexCount >= 0 ? indexCount : indexBuffer.count;
    }
}
