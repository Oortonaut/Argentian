using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Argentian.Wrap {
    public interface IShaderProgram {
        // Debugging and reporting
        public string Name { get; }
        // User must order passes
        public long Order { get; }
        // Called once per distinct material
        public void Bind();
        public void SetTexture(string name, Texture image, Sampler sampler, uint unit = 0);
        public void SetShaderStorageBlock(string name, Buffer block);
        public void SetUniformBlock(string name, Buffer block);
        public void SetUniform<T>(string name, T value) where T : struct;
    }
    public class ShaderProgram: Disposable, IShaderProgram {
        public class Def {
            // TODO: vertex/fragment defines for permutations
            public List<string> vertex = new();
            public List<string> fragment = new();
            public List<string> headers = new();
        }
 
        internal string output = "";
        public long Order { get; set; } = 0;
        public enum TextureBindMode {
            Increment,
            External,
            Layout
        } 
        TextureBindMode unitMode = TextureBindMode.Increment;
        readonly Def def;
        public delegate ShaderHandle ShaderMaker(ShaderType type, string key, List<string> headers);
        //                                                type        source  headers
        public ShaderProgram(string name_, Def def_, ShaderMaker makeShaderObject) :
            this(name_, def_, MakeShaderObjects(def_, makeShaderObject)) { }
        public ShaderProgram(string name_, Def def_) :
            this(name_, def_, MakeShaderObjects(def_, LoadShaderObject)) { }
        public ShaderProgram(string name_, Def def_, IEnumerable<ShaderHandle> shaderObjects) : base(name_) {
            def = def_;
            handle = GL.CreateProgram();
            foreach (var shader in shaderObjects) {
                GL.AttachShader(handle, shader);
            }
            GL.LinkProgram(handle);
            GL.GetProgramInfoLog(handle, out string log);
            DumpLog(log);
            foreach(var shader in shaderObjects) {
                GL.DetachShader(handle, shader);
            }
            GL.ObjectLabel(ObjectIdentifier.Program, ( uint ) handle.Handle, Name.Length, Name);
        }
        public ProgramHandle handle;
        protected override void Delete() {
            GL.DeleteProgram(handle);
        }
        public override string ToString() => $"Program {handle.Handle} '{Name}'{DisposedString}";
        static List<ShaderHandle> MakeShaderObjects(Def def, ShaderMaker makeShaderObject) {
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
        public Dictionary<string, TextureBinding> textureBindings = new();
        uint nextUnit = 0;
        int GLGetUniformInt(uint location) {
            int result = 0;
            GL.GetnUniformi(handle, (int)location, 1, ref result);
            return result;
        }
        public void SetTexture(string name, Texture image, Sampler sampler, uint unit_ = 0) {
            int location_ = GL.GetUniformLocation(handle, name);
            if (location_ < 0) {
                // throw new InvalidDataException($"Couldn't find Texture {name}");
                return;
            }
            uint location = ( uint ) location_;
            uint unit = unitMode switch {
                TextureBindMode.External => unit_,
                TextureBindMode.Layout => (uint)GLGetUniformInt(location),
                TextureBindMode.Increment => nextUnit++,
                _ => throw new ArgumentException(nameof(unitMode)),
            };
            SetUniform(location, (int)unit);

            textureBindings[name] = new TextureBinding { location = location, image = image, sampler = sampler, unit = unit };
        }
        // TODO: Support for BufferTarget
        public struct BufferRangeBinding {
            public BufferTargetARB target;
            public uint index;
            public Buffer buffer;
            public long offset;
            public int size;
        }
        public Dictionary<string, BufferRangeBinding> bufferBindings = new();
        public void SetShaderStorageBlock(string name, Buffer block) {
            uint index = Index(ProgramInterface.ShaderStorageBlock, name);
            bufferBindings[name] = new BufferRangeBinding { target = BufferTargetARB.ShaderStorageBuffer, index = index, buffer = block, offset = 0, size = block.size };
        }
        public void SetUniformBlock(string name, Buffer block) {
            uint index = Index(ProgramInterface.UniformBlock, name);
            bufferBindings[name] = new BufferRangeBinding { target = BufferTargetARB.UniformBuffer, index = index, buffer = block, offset = 0, size = block.size };
        }
        public struct UniformBinding {
            public uint location;
            public dynamic value;
        }
        public Dictionary<string, UniformBinding> uniformBindings = new();
        public void SetUniform<T>(string name, T value) where T : struct {
            // TODO: Set fixed uniforms using GL.GenProgramPipeline();
            int location = GL.GetUniformLocation(handle, name);
            if (location >= 0) {
                uniformBindings[name] = new UniformBinding { location = (uint)location, value = value };
            }
        }
        // TODO - this state needs to go into some command-list or thread specific state.
        static ProgramHandle cmdList_boundHandle = default;
        public void Bind() {
            bool validate = false;
            if (handle != cmdList_boundHandle) {
                cmdList_boundHandle = handle;
                GL.UseProgram(handle);
                validate = true;
            }
            foreach (var (name, binding) in bufferBindings) {
                GL.BindBufferRange(binding.target, binding.index, binding.buffer.handle, (IntPtr)binding.offset, binding.size);
            }
            foreach (var (name, binding) in textureBindings) {
                GL.BindTextureUnit(binding.unit, binding.image.handle);
                GL.BindSampler(binding.unit, binding.sampler.handle);
            }
            foreach(var (name, binding) in uniformBindings) {
                SetUniform(binding.location, binding.value);
            }
            if (validate) {
                GL.ValidateProgram(handle);
            }
        }
        public void SetUniform(uint location, float value) {
            GL.ProgramUniform1f(handle, (int)location, value);
        }
        public void SetUniform(uint location, double value) {
            GL.ProgramUniform1d(handle, (int)location, value);
        }
        public void SetUniform(uint location, uint value) {
            GL.ProgramUniform1ui(handle, (int)location, value);
        }
        public void SetUniform(uint location, int value) {
            GL.ProgramUniform1i(handle, (int)location, value);
        }
        public void SetUniform(uint location, Vector2 value) {

            GL.ProgramUniform2f(handle, (int)location, value);
        }
        public int Location(ProgramInterface iface, string name) {
            return GL.GetProgramResourceLocation(handle, iface, name);
        }
        public int Value(ProgramInterface iface, uint index, ProgramResourceProperty prop) {
            int nameLength = 0;
            int result = 0;
            GL.GetProgramResourcei(handle, iface, index, 1, prop, 1024, ref nameLength, ref result);
            return result;
        }
        public uint Index(ProgramInterface iface, string name) {
            return GL.GetProgramResourceIndex(handle, iface, name);
        }
        public uint LocationIndex(ProgramInterface iface, string name) {
            return (uint)GL.GetProgramResourceLocationIndex(handle, iface, name);
        }
        public string GetName(ProgramInterface iface, uint index) {
            int nameLength = 0;
            GL.GetProgramResourceName(handle, iface, index, 1024, ref nameLength, out string name);
            return name;
        }
        // public IEnumerable<(string name, int result)> Values(ProgramInterface iface, ProgramResourceProperty prop) {
        //     int count = Count(iface);
        //     for (int i = 0; i < count; ++i) {
        //         int value = Value(iface, i, prop);
        //         if (value >= 0) {
        //             yield return (Name(iface, i), value);
        //         }
        //     }
        // }
        // Dictionary<string, (uint index, ActiveUniformType type)> GetActiveBlocks() {
        //     var result = new Dictionary<string, (int, ActiveUniformType)>();
        //     int count = Count(ProgramInterface.Uniform);
        //     for (ushort i = 0; i < count; ++i) {
        //         int location = Value(ProgramInterface.Uniform, i, ProgramResourceProperty.Location);
        //     }
        //     return result;
        // }

        static Dictionary<string, int> sourceFiles = new() { { "prefix", 0 } };
        static Dictionary<int, string> sourceNames = new() { {  0, "prefix"} };
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
        public static void DumpLog(string log) {
            foreach (var line in log.Split(new[] { '\r', '\n' })) {
                var output = line;
                if (output.Any()) {
                    int paren = output.IndexOf('(');
                    if (paren > 0) {
                        int id = int.Parse(output.Substring(0, paren));
                        if (sourceNames.ContainsKey(id)) {
                            output = sourceNames[id] + output.Substring(paren);
                        }
                    }
                    System.Console.WriteLine(output);
                }
            }
        }
        // TODO - make this match what we request from OpenTK
        public static string version = "#version 450 core";
        public unsafe static ShaderHandle LoadShaderObject(ShaderType type, string key, List<string> headers) {
            ShaderHandle shader = GL.CreateShader(type);
            GL.ObjectLabel(ObjectIdentifier.Shader, (uint)shader.Handle, key.Length, key);
            var sources = new List<string>();
            sources.Add(version);
            foreach (var header in headers) {
                AddSource(header);
            }
            AddSource(key);

            IntPtr[] sourceArray = sources.Select(source => Marshal.StringToCoTaskMemAnsi(source)).ToArray();
            int[] sourceLength = sources.Select(x => x.Length).ToArray();
            fixed (IntPtr* bptr = sourceArray)
            {
                GL.ShaderSource(shader, sources.Count, (byte**)bptr, sourceLength);
            }
            GL.CompileShader(shader);
            foreach (var m in sourceArray)
            {
                Marshal.FreeCoTaskMem(m);
            }

            GL.GetShaderInfoLog(shader, out string log);
            DumpLog(log);
            return shader;

            void AddSource(string key) {
                var (source, path) = Core.Config.ReadFile(Core.Config.shaderPath, key);
                sources.Add($"\n#line 1 {SourceFile(path)}\n{source}");
            }
        }
    }
}
