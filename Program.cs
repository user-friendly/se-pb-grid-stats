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
    public partial class Program : MyGridProgram
    {
        IMyTextPanel display = null;
        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();

        // TODO Remove test var.
        //int c = 0;

        public Program()
        {
            // The constructor, called only once every session and
            // always before any other method is called. Use it to
            // initialize your script. 
            //     
            // The constructor is optional and can be removed if not
            // needed.
            // 
            // It's recommended to set Runtime.UpdateFrequency 
            // here, which will allow your script to run itself without a 
            // timer block.

            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            // TODO Remove test var.
            // c = 0;
            Echo("Information program initiated.");
        }

        public void Save()
        {
            // Called when the program needs to save its state. Use
            // this method to save your state to the Storage field
            // or some other means. 
            // 
            // This method is optional and can be removed if not
            // needed.
        }

        public void Main(string argument, UpdateType updateSource)
        {
            // If the update source is from a trigger or a terminal,
            // this is an interactive command.
            if ((updateSource & (UpdateType.Trigger | UpdateType.Terminal)) != 0)
            {
                display = GridTerminalSystem.GetBlockWithName(argument) as IMyTextPanel;
                return;
            }

            if ((updateSource & UpdateType.Update100) == 0)
            {
                return;
            }

            // Write the name of the running programmable block into its detail area
            string details = "PB: " + Me.CustomName + "\n";

            // Write the name of the grid containing the programmable block into its detail area
            details += "Grid: " + Me.CubeGrid.CustomName + "\n";

            // Get all enabled batteries, on the same (station?) grid.
            GridTerminalSystem.GetBlocksOfType(batteries, battery => {
                return battery.Enabled && battery.IsSameConstructAs(Me);
            });
            details += "Active station batteries: " + batteries.Count + "\n";

            float capacity = 0;
            float power = 0;
            foreach (IMyBatteryBlock battery in batteries)
            {
                capacity += battery.MaxStoredPower;
                power += battery.CurrentStoredPower;
            }

            float p = (power / capacity) * 100;
            details += String.Format("Battery power at {0:G2}%", p) + "\n";

            // TODO Remove test var.
            //details += "This is run: " + (c++) + "\n";

            Echo(details);

            if (display != null)
            {
                display.WriteText(details, false);
            }
            else
            {
                Echo("Error: could not find display. Set display name as programming block argument and run the script again.");
            }
        }
    }
}
