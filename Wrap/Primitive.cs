using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Argentian.Wrap {
    // [Flags] public enum PassName { // Must match exactly
    //     None = 0,
    // 
    //     // Actions - pick 1
    //     Prepare = 1 << 0,
    //     Complete = 1 << 1,
    // 
    //     Up = 1 << 2,
    //     Down = 1 << 3,
    // 
    //     Size = 1 << 4,
    //     Sharpness = 1 << 2,
    //     Scatter = 1 << 5,
    //     Gather = 1 << 6,
    //     Analyze = 1 << 7,
    //     Accumulate = 1 << 8,
    //     Draw = 1 << 9,
    // 
    //     // masked = (value & mask)
    //     // masked == test (test values of specific flags)
    // }
    // // Pass must provide all requested primitive flags
    // [Flags] public enum PassFeatures : long {
    //     // bits 0 -15 - entity == pass
    //     // bits 15-31 - (entity & pass) == pass -- pass requirements of entity, any of (Characters or Clutter or Terrain)
    //     // bits 32-48 - (entity & pass) == entity -- entity requirements of pass
    //     None = 0,
    //     // Scene handling
    //     Objects = 1 << 0, // pass renders scene objects
    //     Fullscreen = 1 << 1, // pass is fullscreen 
    // 
    //     // Actions
    //     Start = 1 << 2,
    //     Finish = 1 << 3,
    //     Blur = 1 << 4,
    //     Sharpen = 1 << 5,
    //     Scale = 1 << 6,
    //     Scatter = 1 << 7,
    //     Gather = 1 << 8,
    //     Analyze = 1 << 9,
    // 
    //     // General adjectives
    //     Up = 1 << 7,
    //     Down = 1 << 8,
    //     UI = 1 << 9, 
    //     Scene = 1 << 10,
    //     Debug = 1 << 11,
    // 
    //     // About the pass output
    //     Depth = 1 << 32,
    //     Identity = 1 << 13, // just the id
    //     Surface = 1 << 14, // vbuffer - id, triangle, uvs, gradients
    //     Material = 1 << 15, // gbuffer - albedo, normal, tangent
    //     Lighting = 1 << 16,
    //     Shadow = 1 << 17,
    //     Transparency = 1 << 18,
    //     Color = 1 << 19, // Lit color
    //     // About the pass targets
    //     Linear = 1 << 20,
    //     SRgb = 1 << 21,
    // 
    //     Fog = 1 << 22,
    //     Reflection = 1 << 23,
    //     Sky = 1 << 24,
    //     Terrain = 1 << 25,
    //     Water = 1 << 26,
    //     Ice = 1 << 27,
    //     Physics = 1 << 28,
    //     Simulation = 1 << 29,
    // 
    //     Antialiasing = 1 << 30,
    //     ToneMapping = 1 << 31,
    //     Resampling,
    //     FinalResolve,
    // 
    //      All = ~0,
    // };
    // // OR
    // [Flags] public enum EntityFeatures {
    //     Eighth = 1 << 10, // pass renders to half resolution targets
    //     Quarter = 3 << 10,
    //     Half = 7 << 10,
    //     Ultra = 1 << 13,
    //     High,
    //     Medium,
    //     Low,
    //     Weak,
    // 
    // };
    [Flags]
    public enum PassFlags : long {
        None = 0,
        Tick = 1 << 0,
        All = ~0,
    }
    public interface IPrimitive {
        // Debugging and reporting
        public string Name { get; }
        public IShaderProgram Shader { get; }
        // User must order passes
        public long Order => Shader.Order;
        // Called once per distinct primitive
        public void Tick(double DeltaTime);
        public void BindDraw();
        public void SubmitDraw();
        public PassFlags Flags { get; }
    }
    public class ComputePrimitive: Wrap.Disposable, IPrimitive {
        public ComputePrimitive(string name_, ShaderProgram shader_, Vector3i size_ = new Vector3i()) : base(name_) {
            shader = shader_;
            size = size_;
            //GroupSize = shader.GetUniformVector3i(shader.Location(ProgramInterface.Uniform, "gl_WorkGroupSize"));
        }
        public readonly Vector3i GroupSize;
        public Vector3i size; // Stuff like setting the domain into the shader is part of the pass along with the computation setup
        readonly ShaderProgram shader;
        public void BindDraw() {}
        public static uint Intervals(uint x, uint y) {
            return (x + y - 1) / y;
        }
        public void SubmitDraw() {
            shader.Bind();
            uint x = Intervals((uint)size.X, (uint)GroupSize.X); //
            uint y = Intervals((uint)size.Y, (uint)GroupSize.Y); //
            uint z = Intervals((uint)size.Z, (uint)GroupSize.Z); //
            GL.DispatchCompute(x, y, z);
        }
        public virtual void Tick(double DeltaTime) {}
        protected override void Delete() {}
        public IShaderProgram Shader => shader;
        public PassFlags Flags => PassFlags.None;
    }
    public class RenderPrimitive: Wrap.Disposable, IPrimitive {
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
        ) { }
        public class StreamDef {
            public string name = "";
            public uint stride;
            public uint divisor;
            public List<VertexAttrib> attributes = new();
        }
        public class Def {
            public PrimitiveType primitiveType = PrimitiveType.Triangles;
            public List<StreamDef> vertexStreams = new();
            public Winding windingMode = Winding.CW;
        }
        public Def def;
        //--- Data -----------
        GpuBuffer[] vertexBuffer;
        int vertexBase = 0;
        GpuBuffer indexBuffer;
        int indexBase = 0;
        int indexCount = -1;
        uint instanceBase = 0;
        int instanceCount = 1;
        public readonly ShaderProgram shader;
        public RenderPrimitive(
            string name_,
            ShaderProgram shader_,
            Def format_,
            GpuBuffer[] vb,
            GpuBuffer ib) :
            base(name_)
        {
            shader = shader_;
            def = format_;
            vertexBuffer = vb;
            indexBuffer = ib;
            vertexArrayNative = new VertexArrayHandle(GL.CreateVertexArray());
            int vertexHandle = vertexArrayNative.Handle;
            GL.ObjectLabel(ObjectIdentifier.VertexArray, (uint)vertexArrayNative.Handle, Name.Length, Name);
            // build the gl binding data structure from our def
            uint streamIndex = 0;
            uint attrIndex = 0;
            foreach (var stream in def.vertexStreams) {
                if (stream.divisor > 0) {
                    GL.VertexArrayBindingDivisor(vertexHandle, streamIndex, Math.Max(1, stream.divisor));
                }
                foreach (var attr in stream.attributes) {
                    GL.EnableVertexArrayAttrib(vertexHandle, attrIndex);
                    GL.VertexArrayAttribBinding(vertexHandle, attrIndex, streamIndex);
                    GL.VertexArrayAttribFormat(vertexHandle, attrIndex, (int)attr.width, attr.type, attr.normalized, attr.offset);
                    attrIndex++;
                }
                GL.VertexArrayVertexBuffer(
                    vertexArrayNative.Handle,
                    streamIndex,
                    vertexBuffer[streamIndex].native.Handle,
                    (IntPtr)vertexBuffer[streamIndex].offset.X,
                    vertexBuffer[streamIndex].stride.X);
                streamIndex++;
            }
            GL.VertexArrayElementBuffer(vertexArrayNative.Handle, indexBuffer.native.Handle);
        }
        public void VertexBase(int vertexBase_ = 0) {
            vertexBase = vertexBase_;
        }
        public void IndexRange(int indexBase_ = 0, int indexCount_ = -1) {
            indexBase = indexBase_;
            indexCount = indexCount_;
        }
        public void Instance(uint instanceBase_, uint instanceCount_) {
            instanceBase = instanceBase_;
            instanceCount = (int)instanceCount_;
        }
        public void Instance(uint instanceCount) => Instance(0, instanceCount);

        public VertexArrayHandle vertexArrayNative;
        protected override void Delete() {
            GL.DeleteVertexArray(vertexArrayNative.Handle);
        }
        public override string ToString() => $"VertexArray {vertexArrayNative.Handle} '{Name}'{DisposedString}";
        string IPrimitive.Name => base.Name;
        public IShaderProgram Shader => shader;
        public long Order => shader.Order;
        public virtual void Tick(double DeltaTime) {
            elapsedTime += (ulong)(DeltaTime * 1e6);
        }
        private ulong elapsedTime = 0;
        public double GetElapsedTime() => (double)elapsedTime / 1e6;
        public virtual void BindDraw() {
            shader.Bind();
            GL.BindVertexArray(vertexArrayNative.Handle);
        }
        public void SubmitDraw() {
            BindDraw();
            //shader.SetUniform("time", () => (double)sw.ElapsedTicks / (double)Stopwatch.Frequency);
            DrawElementsType elementType = indexBuffer.stride.X switch {
                1 => DrawElementsType.UnsignedByte,
                2 => DrawElementsType.UnsignedShort,
                4 => DrawElementsType.UnsignedInt,
                _ => throw new InvalidOperationException("Indexbuffer stride must be 1, 2, or 4")
            };
            GL.DrawElementsInstancedBaseVertexBaseInstance(
                def.primitiveType,
                Count,
                elementType, indexBase, instanceCount, vertexBase, instanceBase);
        }
        public int Count => indexCount >= 0 ? indexCount : indexBuffer.count;
        public PassFlags Flags => 0;
    }
}
