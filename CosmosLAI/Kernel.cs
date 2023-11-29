using Cosmos.Core;
using Cosmos.HAL;
using Cosmoss.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Sys = Cosmos.System;

namespace CosmosLAI
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
            Cosmos.Core.Global.Init();
            Console.Clear();
            Console.WriteLine("Starting PCI");
            PCI.Setup();
            Console.WriteLine("Starting ACPI");
            //SerialPort.Enable(SerialPort.COM1);
            mDebugger.Send("ACPI Init");
            foreach (var item in CPU.GetMemoryMap())
            {
                var x = $"Address: {item.Address}, Length: {item.Length}, Type: {item.Type}";
                mDebugger.Send(x);
                Console.WriteLine(x);
                //SerialPort.SendString(x + "\n");
            }
            try
            {
                ACPINew.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("ACPI Start error: " + e.Message);
            }
            Console.WriteLine("ACPI init done");
        }
    }
}
