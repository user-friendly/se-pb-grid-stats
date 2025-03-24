using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    public class ChangeIndicator
    {
        const string Neutral = "—";
        const string Increase = "▲";
        const string Decrease = "▼";

        // Time in ticks.
        int maxTime = 60;

        int time = 0;
        float delta = 0.0f;

        float previous = 0.0f;
        float previousDelta = 0.0f;

        public ChangeIndicator() { }

        public ChangeIndicator(int timeFrame)
        {
            maxTime = timeFrame;
        }

        public void Step(float current, int t = 1)
        {
            if (time >= maxTime)
            {
                time = 0;
                previousDelta = delta;
                delta = 0.0f;
            }

            time += t;
            delta += current - previous;
            previous = current;
        }

        public string GetIndicator()
        {
            return previousDelta == 0.0f ? Neutral : (previousDelta > 0.0f ? Increase : Decrease);
        }

        public float GetDelta()
        {
            return previousDelta;
        }
    }
}
