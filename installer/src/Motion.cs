// SPDX-License-Identifier: AGPL-3.0-or-later
// Copyright (C) 2026 MOSSLS

// Motion.cs — Material Design 3 Expressive spring physics & shared 60fps animation ticker.
// C# 5 syntax for inbox .NET Framework csc.exe compatibility.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace MosSuite
{
    internal struct SpringSpec
    {
        public float Damping;
        public float Stiffness;

        public SpringSpec(float damping, float stiffness)
        {
            Damping = damping;
            Stiffness = stiffness;
        }

        // Official M3 Expressive motion physics tokens (m3.material.io/styles/motion)
        public static readonly SpringSpec ExpressiveFastSpatial = new SpringSpec(0.60f, 800f);
        public static readonly SpringSpec ExpressiveDefaultSpatial = new SpringSpec(0.80f, 380f);
        public static readonly SpringSpec ExpressiveSlowSpatial = new SpringSpec(0.80f, 200f);
        public static readonly SpringSpec ExpressiveFastEffects = new SpringSpec(1.00f, 3800f);
        public static readonly SpringSpec ExpressiveDefaultEffects = new SpringSpec(1.00f, 1600f);
        public static readonly SpringSpec ExpressiveSlowEffects = new SpringSpec(1.00f, 800f);
    }

    internal sealed class SpringValue
    {
        public float Value;
        public float Target;
        public float Velocity;
        public SpringSpec Spec;

        public SpringValue(float initial, SpringSpec spec)
        {
            Value = initial;
            Target = initial;
            Velocity = 0f;
            Spec = spec;
        }

        public bool IsSettled
        {
            get
            {
                return Math.Abs(Value - Target) < 0.0008f && Math.Abs(Velocity) < 0.004f;
            }
        }

        public void Snap(float v)
        {
            Value = v;
            Target = v;
            Velocity = 0f;
        }

        public bool Step(float dt)
        {
            if (IsSettled)
            {
                Value = Target;
                Velocity = 0f;
                return false;
            }

            // Sub-step to guarantee numerical stability even for high stiffness (k=3800).
            int steps = dt > 0.008f ? 4 : 2;
            float h = dt / steps;
            float k = Spec.Stiffness;
            float c = 2f * Spec.Damping * (float)Math.Sqrt(k);

            for (int i = 0; i < steps; i++)
            {
                float x = Value - Target;
                float a = -k * x - c * Velocity;
                Velocity += a * h;
                Value += Velocity * h;
            }

            if (IsSettled)
            {
                Value = Target;
                Velocity = 0f;
                return true;
            }
            return true;
        }
    }

    internal sealed class RippleWave
    {
        public float X;
        public float Y;
        public float MaxRadius;
        public float Progress; // 0..1 expansion
        public float Alpha;    // 0..1 opacity multiplier
        public bool Released;

        public bool Step(float dt)
        {
            // Fast spatial expansion, smooth effect fade out after release.
            float speed = Released ? 4.2f : 3.2f;
            Progress += (1f - Progress) * Math.Min(1f, dt * speed * 3.0f) + dt * 0.65f;
            if (Progress > 1f) Progress = 1f;

            if (Released)
            {
                Alpha -= dt * 3.6f;
                if (Alpha <= 0f)
                {
                    Alpha = 0f;
                    return false;
                }
            }
            else
            {
                Alpha = Math.Min(1f, Alpha + dt * 8f);
            }
            return true;
        }
    }

    internal static class MotionClock
    {
        private static readonly Timer timer = new Timer();
        private static readonly Stopwatch watch = new Stopwatch();
        private static readonly List<Action<float>> listeners = new List<Action<float>>();
        private static double lastMs;
        private static bool initialized;

        public static void Subscribe(Action<float> onTick)
        {
            EnsureInit();
            if (!listeners.Contains(onTick)) listeners.Add(onTick);
            if (!timer.Enabled && listeners.Count > 0)
            {
                lastMs = watch.Elapsed.TotalMilliseconds;
                timer.Start();
            }
        }

        public static void Unsubscribe(Action<float> onTick)
        {
            listeners.Remove(onTick);
            if (listeners.Count == 0 && timer.Enabled)
            {
                timer.Stop();
            }
        }

        private static void EnsureInit()
        {
            if (initialized) return;
            initialized = true;
            watch.Start();
            timer.Interval = 15; // ~60fps
            timer.Tick += OnTick;
        }

        private static void OnTick(object sender, EventArgs e)
        {
            double now = watch.Elapsed.TotalMilliseconds;
            float dt = (float)((now - lastMs) / 1000.0);
            lastMs = now;
            if (dt <= 0f) dt = 0.016f;
            if (dt > 0.05f) dt = 0.05f;

            for (int i = listeners.Count - 1; i >= 0; i--)
            {
                if (i < listeners.Count)
                {
                    listeners[i](dt);
                }
            }
        }
    }
}
