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
            handle = GL.CreateSampler();
            GL.ObjectLabel(ObjectIdentifier.Sampler, ( uint ) handle.Handle, Name.Length, Name);
            GL.SamplerParameterf(handle, SamplerParameterF.TextureBorderColor, def.borderColor.ToFloats());
            GL.SamplerParameteri(handle, SamplerParameterI.TextureMagFilter, (int)def.magFilter);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureMinFilter, (int)def.minFilter);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureWrapS, (int)def.wrapS);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureWrapT, (int)def.wrapT);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureWrapR, (int)def.wrapR);
            GL.SamplerParameterf(handle, SamplerParameterF.TextureMinLod, def.minLod);
            GL.SamplerParameterf(handle, SamplerParameterF.TextureMaxLod, def.maxLod);
            GL.SamplerParameterf(handle, SamplerParameterF.TextureLodBias, def.lodBias);
            GL.SamplerParameterf(handle, SamplerParameterF.TextureMaxAnisotropy, def.maxAnisotropy);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureCompareMode, (int)def.compareMode);
            GL.SamplerParameteri(handle, SamplerParameterI.TextureCompareFunc, (int)def.compareFunc);
        }
        public SamplerHandle handle;
        protected override void Delete() {
            GL.DeleteSampler(handle);
        }
        public override string ToString() => $"Sampler {handle.Handle} '{Name}'{DisposedString}";
    }
}
