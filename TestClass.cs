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
    // 
    // You can use this test class in the main Program class
    // and the Mdk SDK will figure out how to bundle it.
    // It is basically like what Rollup JS does. It is called
    // mixins or something. Old school C marcros/preprocessing.
    // 
    public class TestClass
    {
        public int GetNumber()
        {
            return 42;
        }
    }
}