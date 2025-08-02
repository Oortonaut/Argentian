using System;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Argentian.Engine {
    public class InputEvent {
        public int player = 0;
        public string device = ""; //
        public DateTime realtime;
    }
    public class DrawEvent: InputEvent {}
    public class ButtonEvent: InputEvent {
        public enum Action: byte {
            Up = 0,
            Press = 1,
            Hold = 2,
            Release = 3,
        }
        // The time the key was pressed - subtract from realtime to get the
        // duration
        public readonly DateTime[] pressTime = Enumerable.Repeat(DateTime.MinValue, (int)Keys.LastKey).ToArray();
        public readonly Action[] actions = Enumerable.Repeat(Action.Up, (int)Keys.LastKey).ToArray();

        public bool IsPressed(int i) => actions[i] is Action.Press or Action.Hold;
        public bool IsHeld(int i) => actions[i] is Action.Hold;
        public bool IsReleased(int i) => actions[i] is Action.Release;
        public bool IsUp(int i) => actions[i] is Action.Up or Action.Release;
        public TimeSpan HeldDuration(int i) {
            if (IsPressed(i)) {
                return realtime - pressTime[i];
            } else {
                return TimeSpan.Zero;
            }
        }
        public TimeSpan Released(ButtonEvent prev, int i) {
            var pressed = IsPressed(i);
            var wasPressed = prev.IsPressed(i);
            if (wasPressed && !pressed) {
                return realtime - pressTime[i];
            } else {
                return TimeSpan.Zero;
            }
        }
    }
    public class ControllerEvent: ButtonEvent {
        public readonly int axisButtonOffset; // 1 for 1, pressed if off-center
        public readonly Vector2[] axes; // -1 to 1.
        public readonly int floatButtonOffset; // 1 for 1, pressed if > 0.5
        public readonly float[] triggers; // 0 to 1.
        public readonly int hatButtonOffset; // sets of 4 in CCW order
        public readonly Vector2i[] hats;

    }
    public class NetworkEvent: InputEvent {}
    public class InputQueue {
        
    }
}
