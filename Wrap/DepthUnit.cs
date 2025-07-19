using OpenTK.Graphics.OpenGL;

namespace Argentian.Wrap {
    public class DepthUnit {
        public DepthFunction function = DepthFunction.Less;
        public bool write = true;
        public float rangeNear = 0.0f; // use this slice of the depth view range
        public float rangeFar = 1.0f;
        public float? clear = null;
        public static DepthUnit Clear => new DepthUnit {
            function = DepthFunction.Less,
            write = true,
            clear = 1.0f,
        };
        public static DepthUnit Write => new DepthUnit {
            function = DepthFunction.Less,
            write = true,
        };
        public static DepthUnit Test => new DepthUnit {
            function = DepthFunction.Less,
            write = false,
        };
        public void Bind() {
            GL.DepthFunc(function);
            GL.DepthMask(write);
            GL.DepthRange(rangeNear, rangeFar);
        }
    }
}
