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
        Buffer? indexBuffer;
        int? first;
        int? count;
        public readonly ShaderProgram shader;
        public Primitive(
            string name_, 
            ShaderProgram shader_, 
            Def format_,
            Buffer[] vb, 
            Buffer? ib,
            int? indexFirst_ = null,
            int? indexCount_ = null) :
            base(name_) {
            shader = shader_;
            format = format_;
            vertexBuffer = vb;
            indexBuffer = ib;
            first = indexFirst_;
            count = indexCount_;
            handle = GL.CreateVertexArray();
            GL.ObjectLabel(ObjectIdentifier.VertexArray, ( uint ) handle.Handle, Name.Length, Name);
            uint streamIndex = 0;
            uint attrIndex = 0;
            foreach (var stream in format.streams) {
                foreach (var attr in stream.attributes) {
                    GL.EnableVertexArrayAttrib(handle, attrIndex);
                    GL.VertexArrayAttribBinding(handle, attrIndex, streamIndex);
                    GL.VertexArrayAttribFormat(handle, attrIndex, (int)attr.width, attr.type, attr.normalized, attr.offset);
                    attrIndex++;
                }
                streamIndex++;
            }
        }
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
            if(indexBuffer != null) {
                GL.VertexArrayElementBuffer(handle, indexBuffer.handle);
            }
        }
        public IShaderProgram Shader => shader;
        public long Order => shader.Order;
        public void Draw() {
            //sgader.SetUniform("time", () => (double)sw.ElapsedTicks / (double)Stopwatch.Frequency);
            shader.Bind();
            GL.BindVertexArray(handle);
            if(indexBuffer != null) {
                int f = first.GetValueOrDefault(0);
                int c = count.GetValueOrDefault(indexBuffer.length);
                GL.DrawElements(
                    format.primitiveType,
                    c,
                    indexBuffer.stride switch {
                        1 => DrawElementsType.UnsignedByte,
                        2 => DrawElementsType.UnsignedShort,
                        4 => DrawElementsType.UnsignedInt,
                        _ => throw new InvalidOperationException("Indexbuffer stride must be 1, 2, or 4")
                    }, f);
            } else {
                int f = first.GetValueOrDefault(0);
                int c = count.GetValueOrDefault(vertexBuffer[0].length);
                GL.DrawArrays(format.primitiveType, f, c);
            }
        }
    }
}
