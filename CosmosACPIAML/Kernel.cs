using Cosmos.HAL;
using Cosmos.HAL.Debug;
using Cosmos.System.Graphics;
using Cosmos.System.Graphics.Fonts;
using Cosmoss.Core;
using System;
using System.Collections.Generic;
using System.Text;
using Sys = Cosmos.System;

namespace CosmosACPIAMl
{
    public class Kernel : Sys.Kernel
    {

        protected override void BeforeRun()
        {
            Console.WriteLine("Cosmos booted successfully. Type a line of text to get it echoed back.");
        }

        protected override void Run()
        {
            Console.Write("Input: ");
            var input = Console.ReadLine();
            Console.Write("Text typed: ");
            Console.WriteLine(input);
        }
        protected override void OnBoot()
        {
            Global.TextScreen = new TextScreen();
            Cosmos.Core.Global.Init();
            Console.Clear();
            Console.WriteLine("Starting PCI");
            PCI.Setup();
            Console.WriteLine("Starting ACPI");
            mDebugger.Send("ACPI Init");
            try
            {
                ACPI.Start();
            }
            catch(Exception e)
            {
                Console.WriteLine("ACPI Start error: "+e.Message);
            }
            Console.WriteLine("ACPI init done");
            while (true) { }
        }
    }
}
