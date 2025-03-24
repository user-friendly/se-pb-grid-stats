using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Sandbox.Definitions;
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
using VRage.ObjectBuilders;
using VRageMath;

namespace IngameScript
{
    public partial class Program : MyGridProgram
    {
        const string GasTankDefinitionId = "OxygenTank";
        const string HydrogenTankSubId = "HydrogenTank";

        // Character dimensions of the display.
        const int displayWidth = 40;
        const int displayHeight = 16;

        StringBuilder details;

        IMyTextPanel display = null;

        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        const string SetupModeRequest = "CALIBRATE";
        bool IsSetupMode = false;

        void InitTestScreenText()
        {
            details.Append('+', displayWidth).Append('\n');
            for (int i = 0; i < (displayHeight - 2); i++)
            {
                details.Append('+')
                    .Append('-', displayWidth - 2)
                    .Append("+\n");
            }
            details.Append('+', displayWidth).Append('\n');
        }

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update100;

            // Add an extra height for new lines.
            int maxCap = (displayWidth * displayHeight) + displayHeight;
            details = new StringBuilder(maxCap, maxCap + 1);

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
                if (display != null && argument.Equals(SetupModeRequest))
                {
                    IsSetupMode = true;
                    details.Clear();
                    InitTestScreenText();
                    display.WriteText(details, false);
                    return;
                }
                else
                {
                    IsSetupMode = false;
                }

                display = GridTerminalSystem.GetBlockWithName(argument) as IMyTextPanel;
                return;
            }

            if ((updateSource & UpdateType.Update100) == 0)
            {
                return;
            
            }

            if (IsSetupMode) {
                Echo("In calibration mode.");
                if (display != null) {
                    Echo(String.Format("Display: {0}", display.CustomName));
                }
                else
                {
                    Echo("Error: no display connected.");
                }
                return;
            }

            // Clear up the display string buffer.
            details.Clear();

            // Write the name of the running programmable block into its detail area
            details.Append("PB: ")
                .AppendLine(Me.CustomName);

            // Write the name of the grid containing the programmable block into its detail area
            details.Append("Grid: ")
                .AppendLine(Me.CubeGrid.CustomName);

            // Get all enabled batteries, on the same (station?) grid.
            GridTerminalSystem.GetBlocksOfType(batteries, battery => {
                return battery.Enabled
                    && battery.IsFunctional
                    && battery.IsSameConstructAs(Me);
            });

            details.Append("Active batteries: ")
                .AppendLine(batteries.Count.ToString());

            // Get all enabled hydrogen tanks.
            GridTerminalSystem.GetBlocksOfType(tanks, tank =>
            {
                return tank.Enabled
                    && tank.IsFunctional
                    && tank.IsSameConstructAs(Me)
                    && tank.BlockDefinition.TypeIdString.Contains(GasTankDefinitionId)
                    && tank.BlockDefinition.SubtypeName.Contains(HydrogenTankSubId);
            });

            details.Append("Active tanks: ")
                .AppendLine(tanks.Count.ToString());

            //foreach (var tank in tanks)
            //{
            //    details.AppendLine("Hydro tank: " + tank.CustomName);
            //}

            float capacity = 0;
            float current = 0;
            float p = 0;

            // Calculate electrical power stat.
            foreach (var battery in batteries)
            {
                capacity += battery.MaxStoredPower;
                current += battery.CurrentStoredPower;
            }
            p = current / capacity;
            details.AppendFormat("Battery power is at {0:G3} %\n", (p * 100));
            details.Append('█', Math.Max(1, (int)(displayWidth * p)))
                .AppendLine();

            // Calculate hydrogen stockpile stat.
            capacity = 0;
            current = 0;
            foreach (var tank in tanks)
            {
                capacity += tank.Capacity;
                current += tank.Capacity * (float) tank.FilledRatio;
            }
            p = current / capacity;
            details.AppendFormat("Hydrogen is at {0:G3} %\n", (p * 100));
            details.Append('█', Math.Max(1, (int)(displayWidth * p)))
                .AppendLine();

            Echo(details.ToString());

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
