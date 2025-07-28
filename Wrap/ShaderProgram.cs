using OpenTK;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;

namespace Argentian.Wrap {
    public interface IShaderProgram {
        // Debugging and reporting
        public string Name { get; }
        // User must order passes
        public long Order { get; }
        // Called once per distinct material
        public void Bind();
        public void SetTexture(string name, Texture image, Sampler sampler, uint unit = 0);
        public void SetShaderStorageBlock(string name, GpuBuffer block);
        public void SetUniformBlock(string name, GpuBuffer block);
        public void SetUniform<T>(string name, T value) where T : struct;
        public void SetUniform<T>(string name, T[] value) where T : struct;
    }
    public class ShaderProgram: Disposable, IShaderProgram {
        public class Def {
            // TODO: vertex/fragment defines for permutations
            public List<string> vertex = new();
            public List<string> fragment = new();
            public List<string> headers = new();
            public TextureBindMode unitMode = TextureBindMode.Increment;
        }

        internal string output = "";
        public long Order { get; set; } = 0;
        public enum TextureBindMode {
            Increment,
            External,
            Layout
        }
        readonly Def def;
        public delegate ShaderHandle MakeShaderFn(ShaderType type, string key, List<string> headers);
        //                                                type        source  headers
        public ShaderProgram(string name_, Def def_, MakeShaderFn makeShaderObject):
            this(name_, def_, MakeShaderObjects(def_, makeShaderObject)) {
        }
        public ShaderProgram(string name_, Def def_):
            this(name_, def_, MakeShaderObjects(def_, LoadShaderObject)) {
        }
        public ShaderProgram(string name_, Def def_, IEnumerable<ShaderHandle> shaderObjects): base(name_) {
            def = def_;
            handle = new ProgramHandle(GL.CreateProgram());
            int programHandle = handle.Handle;
            foreach (var shader in shaderObjects) {
                GL.AttachShader(programHandle, shader.Handle);
            }
            GL.LinkProgram(programHandle);
            GL.GetProgramInfoLog(programHandle, out string log);
            DumpLog($"ShaderProgram {Name}", log);
            foreach (var shader in shaderObjects) {
                GL.DetachShader(programHandle, shader.Handle);
            }
            GL.ObjectLabel(ObjectIdentifier.Program, (uint)programHandle, Name.Length, Name);
        }
        public ProgramHandle handle;
        protected override void Delete() {
            GL.DeleteProgram(handle.Handle);
        }
        public override string ToString() => $"Program {handle.Handle} '{Name}'{DisposedString}";
        static List<ShaderHandle> MakeShaderObjects(Def def, MakeShaderFn makeShaderObject) {
            var vsos = def.vertex.Select(path => makeShaderObject(ShaderType.VertexShader, path, def.headers));
            var fsos = def.fragment.Select(path => makeShaderObject(ShaderType.FragmentShader, path, def.headers));
            var result = new List<ShaderHandle>(vsos);
            result.AddRange(fsos);
            return result;
        }

        // #### Texture
        public struct TextureBinding {
            public uint location;
            public uint unit;
            public Texture image;
            public Sampler sampler;
        }
        Dictionary<string, TextureBinding> textureBindings = new();
        uint nextUnit = 0;
        int GetUniformInt(uint location) {
            int result = 0;
            GL.GetnUniformi(handle.Handle, (int)location, Marshal.SizeOf<int>(), ref result);
            return result;
        }
        public unsafe Vector3i GetUniformVector3i(uint location) {
            Vector3i result = new();
            GL.GetUniformiv(handle.Handle, (int)location, &result.X);
            return result;
        }
        public void SetTexture(string name, Texture image, Sampler sampler, uint unit_ = 0) {
            int location_ = Location(ProgramInterface.Uniform, name);
            if (location_ < 0) {
                // throw new InvalidDataException($"Couldn't find Texture {name}");
                return;
            }
            uint location = (uint)location_;
            uint unit = def.unitMode switch {
                TextureBindMode.External => unit_,
                TextureBindMode.Layout => (uint)GetUniformInt(location),
                TextureBindMode.Increment => nextUnit++,
                _ => throw new ArgumentException(nameof(def.unitMode)),
            };

            uniformBindings[name] = new UniformBinding {
                location = location, value = (int)unit, dirty = true
            };
            textureBindings[name] = new TextureBinding {
                location = location, image = image, sampler = sampler, unit = unit
            };
        }
        // TODO: Support for BufferTarget
        public struct BufferRangeBinding {
            public BufferTarget target;
            public uint index;
            public GpuBuffer buffer;
            public long offset;
            public int size;
        }
        Dictionary<string, BufferRangeBinding> bufferBindings = new();
        public void SetShaderStorageBlock(string name, GpuBuffer buffer) {
            int index = Index(ProgramInterface.ShaderStorageBlock, name);
            if (index >= 0) {
                bufferBindings[name] = new BufferRangeBinding {
                    target = BufferTarget.ShaderStorageBuffer,
                    index = (uint)index,
                    buffer = buffer,
                    offset = 0,
                    size = buffer.length
                };
            } else {
                Trace.TraceError($"Couldn't find ShaderStorageBlock {name}");
            }
        }
        public void SetUniformBlock(string name, GpuBuffer buffer) {
            int index = Index(ProgramInterface.UniformBlock, name);
            if (index >= 0) {
                bufferBindings[name] = new BufferRangeBinding {
                    target = BufferTarget.UniformBuffer,
                    index = (uint)index,
                    buffer = buffer,
                    offset = 0,
                    size = buffer.length
                };
            } else {
                Trace.TraceError($"Couldn't find UniformBlock {name}");
            }
        }
        public enum When {
            Once,
            Frame,
            Done
        };
        public struct UniformBinding {
            public uint location;
            public dynamic value;
            public bool dirty;
        }
        public Dictionary<string, UniformBinding> uniformBindings = new();
        public void SetUniform<T>(string name, T value) where T : struct {
            // TODO: Set fixed uniforms using GL.GenProgramPipeline();
            int location = Location(ProgramInterface.Uniform, name);
            if (location >= 0) {
                uniformBindings[name] = new UniformBinding {
                    location = (uint)location, value = value, dirty = true
                };
            } else {
                Trace.TraceError($"Couldn't find Uniform {name}");
            }
        }
        public void SetUniform<T>(string name, T[] value) where T : struct {
            // TODO: Set fixed uniforms using GL.GenProgramPipeline();
            int location = Location(ProgramInterface.Uniform, name);
            if (location >= 0) {
                uniformBindings[name] = new UniformBinding {
                    location = (uint)location, value = value, dirty = true
                };
            } else {
                Trace.TraceError($"Couldn't find Uniform {name}");
            }
        }

        // TODO - this state needs to go into some command-list or thread specific state.
        static ProgramHandle cmdList_boundHandle = default;
        public void Bind() {
            bool validate = false;
            if (handle != cmdList_boundHandle) {
                cmdList_boundHandle = handle;
                GL.UseProgram(handle.Handle);
                validate = true;
            }
            nextUnit = 0;
            foreach (var name in uniformBindings.Keys) {
                var binding = uniformBindings[name];
                if (binding.dirty) {
                    try {
                        ProgramUniform(binding.location, binding.value);
                        binding.dirty = false;
                        // have to rewrite values in C#
                        uniformBindings[name] = binding;
                    } catch (RuntimeBinderException e) {
                        throw new InvalidOperationException($"uniform {name}: Error: Bad Type {binding.value.GetType()}. Add to ProgramUniform set.", e);
                    }
                }
            }
            foreach (var (name, binding) in bufferBindings) {
                GL.BindBufferRange(binding.target, binding.index, binding.buffer.native.Handle, (IntPtr)binding.offset, binding.size);
            }
            foreach (var (name, binding) in textureBindings) {
                GL.BindTextureUnit(binding.unit, binding.image.native.Handle);
                GL.BindSampler(binding.unit, binding.sampler.handle.Handle);
            }
            if (validate) {
                GL.ValidateProgram(handle.Handle);
                validate = false;
            }
        }
        public void ProgramUniform(uint location, float value) { GL.ProgramUniform1f(handle.Handle, (int)location, value); }
        public unsafe void ProgramUniform(uint location, float[] value) {
            fixed (float* v = &value[0]) {
                GL.ProgramUniform1fv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, double value) { GL.ProgramUniform1d(handle.Handle, (int)location, value); }
        public unsafe void ProgramUniform(uint location, double[] value) {
            fixed (double* v = &value[0]) {
                GL.ProgramUniform1dv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, uint value) { GL.ProgramUniform1ui(handle.Handle, (int)location, value); }
        public unsafe void ProgramUniform(uint location, uint[] value) {
            fixed (uint* v = &value[0]) {
                GL.ProgramUniform1uiv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, int value) { GL.ProgramUniform1i(handle.Handle, (int)location, value); }
        public unsafe void ProgramUniform(uint location, int[] value) {
            fixed (int* v = &value[0]) {
                GL.ProgramUniform1iv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector2 value) { GL.ProgramUniform2f(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector2[] value) {
            fixed (float* v = &value[0].X) {
                GL.ProgramUniform2fv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector2i value) { GL.ProgramUniform2i(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector2i[] value) {
            fixed (int* v = &value[0].X) {
                GL.ProgramUniform2iv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector3 value) { GL.ProgramUniform3f(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector3[] value) {
            fixed (float* v = &value[0].X) {
                GL.ProgramUniform3fv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector3i value) { GL.ProgramUniform3i(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector3i[] value) {
            fixed (int* v = &value[0].X) {
                GL.ProgramUniform3iv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector4 value) { GL.ProgramUniform4f(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector4[] value) {
            fixed (float* v = &value[0].X) {
                GL.ProgramUniform4fv(handle.Handle, (int)location, value.Length, v);
            }
        }
        public void ProgramUniform(uint location, Vector4i value) { GL.ProgramUniform4i(handle.Handle, (int)location, 1, in value); }
        public unsafe void ProgramUniform(uint location, Vector4i[] value) {
            fixed (int* v = &value[0].X) {
                GL.ProgramUniform4iv(handle.Handle, (int)location, value.Length, v);
            }
        }

        // # Queries
        // ## Program properties
        public int Program(ProgramProperty pname) {
            GL.GetProgrami(handle.Handle, pname, out int result);
            return result;
        }
        // ## Interface properties
        public int Interface(ProgramInterface iface, ProgramInterfacePName name) {
            GL.GetProgramInterfacei(handle.Handle, iface, name, out int result);
            return result;
        }
        // ## Identifying resources
        public int Location(ProgramInterface iface, string name) => GL.GetProgramResourceLocation(handle.Handle, iface, name);
        public int Index(ProgramInterface iface, string name) => (int)GL.GetProgramResourceIndex(handle.Handle, iface, name);
        public int LocationIndex(ProgramInterface iface, string name) => GL.GetProgramResourceLocationIndex(handle.Handle, iface, name);
        // ## Resource values
        public int Value(ProgramInterface iface, uint index, ProgramResourceProperty prop) {
            int result = 0;
            GL.GetProgramResourcei(handle.Handle, iface, index, 1, ref prop, 1024, out int nameLength, ref result);
            return result;
        }
        public string GetName(ProgramInterface iface, uint index) {
            GL.GetProgramResourceName(handle.Handle, iface, index, 1024, out int nameLength, out string name);
            return name;
        }
        public IEnumerable<(string name, int result)> Values(ProgramInterface iface, ProgramResourceProperty prop) {
            int resourceCount = Interface(iface, ProgramInterfacePName.ActiveResources);
            for (uint i = 0; i < resourceCount; ++i) {
                int value = Value(iface, i, prop);
                if (value >= 0) {
                    yield return (GetName(iface, i), value);
                }
            }
        }
        public IEnumerable<(string name, int result)> Uniforms => Values(ProgramInterface.Uniform, ProgramResourceProperty.Location);
        // Dictionary<string, (uint index, ActiveUniformType type)> GetActiveBlocks() {
        //     var result = new Dictionary<string, (int, ActiveUniformType)>();
        //     int resourceCount = Count(ProgramInterface.Uniform);
        //     for (ushort i = 0; i < resourceCount; ++i) {
        //         int location = Value(ProgramInterface.Uniform, i, ProgramResourceProperty.Location);
        //     }
        //     return result;
        // }

        static Dictionary<string, int> sourceFiles = new() {
            {
                "prefix", 0
            }
        };

        static Dictionary<int, string> sourceNames = new() {
            {
                0, "prefix"
            }
        };

        static int SourceFile(string file) {
            if (sourceFiles.ContainsKey(file)) {
                return sourceFiles[file];
            } else {
                int k = sourceFiles.Count;
                sourceFiles[file] = k;
                sourceNames[k] = file;
                return k;
            }
        }
        public static void DumpLog(string key, string log) {
            foreach (var line in log.Split(new[] {
                         '\r', '\n'
                     })) {
                var output = line;
                if (output.Any()) {
                    int paren = output.IndexOf('(');
                    if (paren > 0) {
                        int id = int.Parse(output.Substring(0, paren));
                        if (sourceNames.ContainsKey(id)) {
                            var outname = sourceNames[id].Replace('/', '\\');
                            output = outname + output.Substring(paren);
                        }
                    }
                    System.Console.WriteLine(output);
                    System.Diagnostics.Debug.WriteLine(output);
                }
            }
            System.Console.WriteLine($"Compiled {key}");
            System.Diagnostics.Debug.WriteLine($"Compiled {key}");
        }

        // TODO - make this match what we request from OpenTK
        public static string version = "#version 460 core";
        public unsafe static ShaderHandle LoadShaderObject(ShaderType type, string key, List<string> headers) {
            int shaderHandle = GL.CreateShader(type);
            GL.ObjectLabel(ObjectIdentifier.Shader, (uint)shaderHandle, key.Length, key);
            var sources = new List<string>();
            sources.Add(version);
            foreach (var header in headers) {
                AddSource(header);
            }
            AddSource(key);

            int[] sourceLength = sources.Select(x => x.Length).ToArray();
            GL.ShaderSource(shaderHandle, sources.Count, sources.ToArray(), sourceLength);
            GL.CompileShader(shaderHandle);

            GL.GetShaderInfoLog(shaderHandle, out string log);
            DumpLog($"ShaderObject {key}", log);

            return new ShaderHandle(shaderHandle);

            void AddSource(string key) {
                var (source, path) = Core.Config.ReadFile(Core.Config.shaderPath, key);
                sources.Add($"\n#line 1 {SourceFile(path)}\n{source}");
            }
        }
    }
}
