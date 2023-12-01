using Cosmos.Core;
using Cosmos.Core.Memory;
using Cosmos.Core.Multiboot;
using Cosmos.Core.Multiboot.Tags;
using Cosmos.HAL;
using System;
using ACPI = Cosmoss.Core.ACPI;
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
        protected unsafe override void OnBoot()
        {
            //Global.TextScreen = new TextScreen();
            //var x = new VBECanvas(new Mode(600, 800, ColorDepth.ColorDepth32));
            //x.Clear(Color.DarkGray);
            //x.DrawFilledRectangle(new Pen(Color.Gray),0,0,800,50);
            //x.DrawString("AMLParser revision v0.1", PCScreenFont.Default, new Pen(Color.White),new Sys.Graphics.Point(0,55));
            //x.DrawFilledRectangle(new Pen(Color.Gray), 0, 600-50, 800, 50);
            //x.Display();


            Cosmos.Core.Global.Init();
            Console.Clear();
            Console.WriteLine("Starting PCI");
            PCI.Setup();
            Console.WriteLine("Starting ACPI");
            //SerialPort.Enable(SerialPort.COM1);
            mDebugger.Send("ACPI Init");

            uint MbAddress = Multiboot2.GetMBIAddress();

            MB2Tag* Tag;

            for (Tag = (MB2Tag*)(MbAddress + 8); Tag->Type != 0; Tag = (MB2Tag*)((byte*)Tag + (Tag->Size + 7 & ~7)))
            {
                Console.WriteLine("TAG: " + Tag->Type);
            }

            foreach (var item in CPU.GetMemoryMap())
            {
                var x = $"Address: 0x{item.Address.ToString("X")}, Length: 0x{item.Length.ToString("X")}, Type: {item.Type}";
                mDebugger.Send(x);
                Console.WriteLine(x);
                //SerialPort.SendString(x + "\n");
            }
            Console.WriteLine("RAT is at 0x" + ((ulong)RAT.RamStart).ToString("X")+", ends at 0x"+ ((ulong)RAT.HeapEnd).ToString("X"));

            Console.WriteLine("mrat is at 0x" + ((ulong)RAT.mRAT).ToString("X"));
            try
            {
                ACPI.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine("ACPI Start error: " + e.Message);
            }
            Console.WriteLine("ACPI init done");

        }
    }
}
