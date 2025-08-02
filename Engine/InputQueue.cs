using System;
using System.Collections.Concurrent;
using System.Linq;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Argentian.Engine {
    public static partial class Extensions {
        public static Vector2 AdjustForDeadzone(this Vector2 the, float Deadzone) {
            if (the.LengthSquared > Deadzone * Deadzone) {
                return (the - Deadzone) / (1 - Deadzone);
            } else {
                return Vector2.Zero;
            }
        }
            
    }
    public class InputEvent(string source, DateTime time) {
        public string Source = source; //
        public DateTime Time = time;
    }
    public class DrawEvent(string source, DateTime time): InputEvent(source, time) {}
    public class PlayerEvent(int player, string source, DateTime time): InputEvent(source, time) {
        public int Player = player;
    }
    public class KeyboardStateEvent(int player, string source, DateTime time): PlayerEvent(player, source, time) {
        public readonly bool[] held;
        public readonly bool[] wasHeld;
    }
    public class ControllerStateEvent(int player, string source, DateTime time): PlayerEvent(player, source, time) {
        public readonly Vector2[] sticks; // -1 to 1.
        public readonly bool[] stickButtons; // one per stick (off-center)

        public readonly float[] triggers; // 0 to 1, resetting
        public readonly bool[] triggerButtons; // one per trigger

        public readonly Vector2i[] hats; // -1, 0, or 1
        public readonly bool[] hatButtons;
    }
    public class NetworkEvent(string source, DateTime time): InputEvent(source, time) {}
    public class InputQueue {
        public InputQueue() {}

        public void Enqueue(InputEvent Event) {
            events.Enqueue(Event);
        }

        // Typically the game thread will empty the queue in
        // batches
        public InputEvent? Dequeue() {
            if (events.TryDequeue(out var e)) {
                return e;
            } else {
                return null;
            }
        }

        ConcurrentQueue<InputEvent> events = new();
    }
    public struct ButtonInput {
        private TimeSpan pressDuration;
        private bool down;
        private bool wasDown;
        public bool Value => down;
        public TimeSpan HoldTime => down ? pressDuration : TimeSpan.Zero;
        public bool IsPressed => down && !wasDown;
        public TimeSpan HeldTime => !down && wasDown ? pressDuration : TimeSpan.Zero;

        public void Tick(TimeSpan duration, bool nextHeld) {
            wasDown = down;
            down = nextHeld;
            if (down) {
                if (!wasDown) {
                    pressDuration = TimeSpan.Zero;
                } else {
                    pressDuration += duration;
                }
            } else if (wasDown) {
                // include the release frame in the hold time
                // so that it's always at least one frame
                pressDuration += duration;
            }
        }
    }
    public struct AxisInput {
        public AxisInput(float deadzone = 0.1f) {
            Deadzone = deadzone;
        }
        public ButtonInput Button = new();
        private Vector2 value = Vector2.Zero;
        public Vector2 Value => value;
        public TimeSpan HoldTime => Button.HoldTime;
        public bool IsPressed => Button.IsPressed;
        public TimeSpan HeldTime => Button.HeldTime;

        public float Deadzone = 0.1f;
        public void Tick(TimeSpan duration, Vector2 nextValue) {
            value = nextValue.AdjustForDeadzone(Deadzone);
            bool down = value.LengthSquared > 0;
            Button.Tick(duration, down);
        }
    }
}
