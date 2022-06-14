using ACPILibs.Parser2;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosACPIAMl
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Create parser...");

            var stream = File.OpenRead("../../../pciIrqDsdt.aml");

            var root = new Parser(stream);

            Console.WriteLine("Parsing file...");

            var node = root.Parse();
            
            Console.ReadKey();
        }
    }
}
