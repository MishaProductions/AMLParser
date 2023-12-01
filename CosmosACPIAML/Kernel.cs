﻿using Cosmos.Core;
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
        protected override void OnBoot()
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
            foreach (var item in CPU.GetMemoryMap())
            {
                var x = $"Address: {item.Address}, Length: {item.Length}, Type: {item.Type}";
                mDebugger.Send(x);
                Console.WriteLine(x);
                //SerialPort.SendString(x + "\n");
            }
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