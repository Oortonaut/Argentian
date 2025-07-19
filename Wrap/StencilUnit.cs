using OpenTK.Graphics.OpenGL;

namespace Argentian.Wrap {
    public class StencilUnit {
        public record Face (
            StencilOp fail = StencilOp.Keep,
            StencilOp zfail = StencilOp.Keep,
            StencilOp pass = StencilOp.Keep,
            uint test = 0,
            uint testMask = uint.MaxValue,
            uint writeMask = uint.MaxValue,
            StencilFunction function = StencilFunction.Always) {
            public void Bind(TriangleFace face) {
                GL.StencilFuncSeparate(face, function, (int)test, testMask);
                GL.StencilOpSeparate(face, fail, zfail, pass);
                GL.StencilMaskSeparate(face, writeMask);
            }
        }
        public Face front = new();
        public Face back = new();
        public uint? clear = null;
        public void Bind() {
            front.Bind(TriangleFace.Front);
            back.Bind(TriangleFace.Back);
        }
    }
}
