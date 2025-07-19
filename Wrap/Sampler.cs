using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Argentian.Wrap {
    public static partial class Extensions {
        public static Color4<Rgba> WithAlpha(this Color4<Rgba> c, float alpha) {
            var (r, g, b, _) = c;
            return new Color4<Rgba>(r, g, b, alpha);
        }
        public static float[] ToFloats(this Color4<Rgba> c) {
            return new float[] { c.X, c.Y, c.Z, c.W };
        }
        public static Vector4 ToVector4(this Color4<Rgba> c) {
            return new Vector4(c.X, c.Y, c.Z, c.W);
        }
    }
    public class Sampler: Disposable {
        public class Def {
            public Color4<Rgba> borderColor = new Color4<Rgba>();
            public TextureMagFilter magFilter = TextureMagFilter.Linear;
            public TextureMinFilter minFilter = TextureMinFilter.NearestMipmapLinear;
            public TextureWrapMode wrapS = TextureWrapMode.Repeat;
            public TextureWrapMode wrapT = TextureWrapMode.Repeat;
            public TextureWrapMode wrapR = TextureWrapMode.Repeat;
            public float minLod = -1000;
            public float maxLod = 1000;
            public float maxAnisotropy = 1.0f;
            public float lodBias = 0;
            public TextureCompareMode compareMode = TextureCompareMode.None;
            public DepthFunction compareFunc = DepthFunction.Less;
        }
        public readonly Def def;
        public Sampler(string name, Def def_): base(name) {
            def = def_;
            handle = new SamplerHandle(GL.CreateSampler());
            int samplerHandle = handle.Handle;
            GL.ObjectLabel(ObjectIdentifier.Sampler, (uint)samplerHandle, Name.Length, Name);
            GL.SamplerParameterf(samplerHandle, SamplerParameterF.TextureBorderColor, def.borderColor.ToFloats());
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureMagFilter, (int)def.magFilter);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureMinFilter, (int)def.minFilter);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureWrapS, (int)def.wrapS);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureWrapT, (int)def.wrapT);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureWrapR, (int)def.wrapR);
            GL.SamplerParameterf(samplerHandle, SamplerParameterF.TextureMinLod, def.minLod);
            GL.SamplerParameterf(samplerHandle, SamplerParameterF.TextureMaxLod, def.maxLod);
            GL.SamplerParameterf(samplerHandle, SamplerParameterF.TextureLodBias, def.lodBias);
            GL.SamplerParameterf(samplerHandle, SamplerParameterF.TextureMaxAnisotropy, def.maxAnisotropy);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureCompareMode, (int)def.compareMode);
            GL.SamplerParameteri(samplerHandle, SamplerParameterI.TextureCompareFunc, (int)def.compareFunc);
        }
        public SamplerHandle handle;
        protected override void Delete() {
            GL.DeleteSampler(handle.Handle);
        }
        public override string ToString() => $"Sampler {handle.Handle} '{Name}'{DisposedString}";
    }
}
