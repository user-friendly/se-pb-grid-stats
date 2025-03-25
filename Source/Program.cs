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
        // Type and frequency should match.
        UpdateType MainLoopUpdateCheck = UpdateType.Update100;
        UpdateFrequency MainLoopUpdateFrequency = UpdateFrequency.Update100;

        const string GasTankDefinitionId = "OxygenTank";
        const string HydrogenTankSubId = "HydrogenTank";

        // Character dimensions of the display.
        const int displayWidth = 40;
        const int displayHeight = 16;

        StringBuilder details;

        IMyTextPanel display = null;

        List<IMyBatteryBlock> batteries = new List<IMyBatteryBlock>();
        List<IMyGasTank> tanks = new List<IMyGasTank>();

        ChangeIndicator IndicatorPower = new ChangeIndicator(3);
        ChangeIndicator IndicatorHydrogen = new ChangeIndicator(3);

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

        // TODO Move into its own class?
        const char ProgressBarFull = '█';
        // Could also be '▒' if desired.
        const char ProgressBarEmpty = '░';
        void RenderTextProgressBar(StringBuilder output, float percent, int width)
        {
            int fullSize = 0;
            if (percent > 0.0f)
            {
                fullSize = Math.Max(1, (int)(width * percent));
                output.Append(ProgressBarFull, fullSize);
            }
            output.Append(ProgressBarEmpty, width - fullSize)
                .AppendLine();
        }
        
        void RenderTextStat(StringBuilder output, float percent, string label = "Unknown", string suffix = null)
        {
            percent *= 100;
            output.AppendLine($"{label} is at {percent:G3} %{(suffix != null ? " " + suffix : "")}");
        }

        void RenderTextTimeSpan(StringBuilder output, TimeSpan ts)
        {
            // {ts.Days} days, {ts.Hours} h, {ts.Minutes} mins"
            if (ts.Days > 0)
            {
                output.Append($"{ts.Days} day{(ts.Hours > 0 ? ", " : "")}");
            }
            if (ts.Hours > 0)
            {
                output.Append($"{ts.Hours} hour{(ts.Minutes > 0 ? ", " : "")}");
            }
            if (ts.Minutes > 0)
            {
                output.Append($"{ts.Minutes} min{(ts.Seconds > 0 ? ", " : "")}");
            }
            if (ts.Seconds > 0)
            {
                output.Append($"{ts.Seconds} sec");
            }
        }

        public Program()
        {
            Runtime.UpdateFrequency = MainLoopUpdateFrequency;

            // Add an extra height for new lines.
            int maxCap = ((displayWidth * displayHeight) + displayHeight) * 2;
            details = new StringBuilder(0, maxCap + 1);

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

            if ((updateSource & MainLoopUpdateCheck) == 0)
            {
                return;
            }

            if (IsSetupMode) {
                Echo("In calibration mode.");
                if (display != null) {
                    Echo($"Display: {display.CustomName}");
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

            // Get all enabled hydrogen tanks.
            GridTerminalSystem.GetBlocksOfType(tanks, tank =>
            {
                return tank.Enabled
                    && tank.IsFunctional
                    && tank.IsSameConstructAs(Me)
                    && tank.BlockDefinition.TypeIdString.Contains(GasTankDefinitionId)
                    && tank.BlockDefinition.SubtypeName.Contains(HydrogenTankSubId);
            });

            details.AppendLine($"Active batteris, tanks: {batteries.Count.ToString()}, {tanks.Count.ToString()}")
                .AppendLine();

            float capacity = 0;
            float current = 0;
            float p = 0;

            float flow = 0.0f;

            // Calculate electrical power stat.
            foreach (var battery in batteries)
            {
                capacity += battery.MaxStoredPower;
                current += battery.CurrentStoredPower;

                // In MegaWatts (MW).
                flow += battery.CurrentInput - battery.CurrentOutput;
            }
            p = current / capacity;
            IndicatorPower.Step(current);
            RenderTextStat(details, p, "Battery power", IndicatorPower.GetIndicator());
            RenderTextProgressBar(details, p, displayWidth);
            
            TimeSpan ts = new TimeSpan();
            // The above battery input/output is in MW. The current power is in MWh.
            // Dividing the stored MWh by the consumed MW will give us the time it takes
            // to discharge the battery, in hours. Below, this given time is multiplied by
            // 60 minutes, to give us a more granular information display. The TS class
            // accepts whole numbers and not fractions, which the stored/consumed MW ends up
            // usually being in.
            if (flow < 0.0f)
            {
                ts = new TimeSpan(0, (int)(current / -flow * 60.0f), 0);
                details.Append($"Discharged in ");
            }
            else if (flow > 0.0f)
            {
                ts = new TimeSpan(0, (int)((capacity - current) / flow * 60.0f), 0);
                details.Append($"Recharged in ");
            }
            RenderTextTimeSpan(details, ts);
            details.AppendLine();
            details.AppendLine($"{current:F2}/{capacity:F2} MWh");

            details.AppendLine();

            // Calculate hydrogen stockpile stat.
            capacity = 0;
            current = 0;
            foreach (var tank in tanks)
            {
                capacity += tank.Capacity;
                current += tank.Capacity * (float) tank.FilledRatio;
            }
            p = current / capacity;
            IndicatorHydrogen.Step(current);
            
            // FIXME This is a total magic hack. And this convoluted thing comes from the fact that
            // I'm using the indicator class for something that it was not designed to be used for.
            flow = (IndicatorHydrogen.GetDelta() / (3.0f * (float) Runtime.TimeSinceLastRun.TotalSeconds));

            RenderTextStat(details, p, "Hydrogen", IndicatorHydrogen.GetIndicator());
            RenderTextProgressBar(details, p, displayWidth);

            if (flow != 0.0f)
            {
                ts = new TimeSpan();
                if (flow < 0.0f)
                {
                    ts = new TimeSpan(0, 0, (int)(current / -flow));
                    details.Append($"Depleted in ");
                }
                else if (flow > 0.0f)
                {
                    ts = new TimeSpan(0, 0, (int)((capacity - current) / flow));
                    details.Append($"Refilled in ");
                }
                RenderTextTimeSpan(details, ts);
                details.AppendLine();
            }
            details.AppendLine(FormatSi(current, true) + "/" + FormatSi(capacity));

            // RenderTextTestProps();

            if (display != null)
            {
                display.WriteText(details, false);
                Echo($"Stats output to display:\n{display.CustomName}");
                Echo($"Last run time (in ms): {Runtime.LastRunTimeMs}");
            }
            else
            {
                Echo("Error: could not find display. Set display name as programming block argument and run the script again.");
            }
        }

        string[] prefixeSI = { "K", "M", "G", "T" };
        string FormatSi(float number, bool omitPrefix = false)
        {
            int log10 = (int)Math.Log10(Math.Abs(number));
            if (log10 > 2)
            {
                // Gets you the index in prefixSi.
                int k = Math.Min(4, ((int)log10 / 3));
                // Divide `number` by this to get it in prefix units.
                int newNumber = (int)(number / Math.Pow(10, k * 3));
                if (omitPrefix)
                {
                    return $"{newNumber:N0}";
                }
                return $"{newNumber:N0} {prefixeSI[k - 1]}";
            }
            return number.ToString();
        }

        float testProp = 0;
        Random rand = new Random();
        void RenderTextTestProps()
        {
            RenderTextStat(details, 0.0f, "Empty prop");
            RenderTextProgressBar(details, 0.0f, displayWidth);

            if (testProp >= 1.0f)
            {
                testProp = 0.0f;
            }
            else
            {
                testProp += (float)rand.Next(200, 1500) / 10000.0f;
                if (testProp > 1.0f)
                {
                    testProp = 1.0f;
                }
            }
            RenderTextStat(details, testProp, "Test prop");
            RenderTextProgressBar(details, testProp, displayWidth);

            RenderTextStat(details, 1.0f, "Full prop");
            RenderTextProgressBar(details, 1.0f, displayWidth);
        }
    }
}
