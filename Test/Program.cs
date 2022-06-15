using ACPILibs.Parser2;
using System;
using System.Collections.Generic;
using System.Text;

namespace CosmosACPIAMl
{
    public class Program
    {
        static FileStream _sdt;
        static BinaryReader _reader;

        static uint _sdtLength;
        static void Main(string[] args)
        {
            _sdt = File.OpenRead(@"test.aml");
            _reader = new BinaryReader(_sdt);

            //STUFF
            {
                ReadHeader();

                var root = new Parser(_sdt).Parse();
                if (root != null)
                {
                    foreach (var item in root.Nodes)
                    {
                        DisplayNode(item, " ");
                    }
                }
            }

            _sdt.Close();

            Console.WriteLine("Finished!");
            Console.ReadKey();
        }

        private static void DisplayNode(ParseNode item, string spacing)
        {
            if (item.Op.Name == "Scope")
            Console.WriteLine(spacing + "Scope: " + item.Name);
            else
                Console.WriteLine(spacing + "Node: " + item.Name);
            Console.WriteLine(spacing + " - OpCode: " + item.Op.ToString());
            Console.WriteLine(spacing + $" - Length: {item.Length}, DataStart: {item.DataStart}, DataEnd: {item.DataStart + item.Length}");
            foreach (var arg in item.Arguments)
            {
                Console.WriteLine(spacing + " -- Argument: " + ValueToString(arg) + $"({arg.GetType().Name})");
            }

            spacing += "  ";
            foreach (var n in item.Nodes)
            {
                DisplayNode(n, spacing);
            }
        }
        private static string ValueToString(object val)
        {
            if (val == null)
                return "null";

            if (val is string)
                return "\"" + val.ToString() + "\"";

            if (val is byte)
                return "0x" + ((byte)val).ToString("X2");

            if (val.GetType().IsArray)
            {
                Array ar = (Array)val;

                string rt = "";

                for (int x = 0; x < ar.Length; x++)
                    rt += ValueToString(ar.GetValue(x)) + (x < ar.Length - 1 ? ", " : string.Empty);

                return rt;
            }

            if (val is ParseNode)
            {
                ParseNode node = (ParseNode)val;

                if (node.ConstantValue != null)
                    return ValueToString(node.ConstantValue);
            }

            return val.ToString();
        }
        static void ReadHeader()
        {
            Console.WriteLine("SDT header:");

            //Signature
            Console.WriteLine("\tSignature: " + Encoding.ASCII.GetString(_reader.ReadBytes(4)));

            //Length
            _sdtLength = _reader.ReadUInt32();
            Console.WriteLine("\tLendth: " + _sdtLength.ToString() + " / " + _sdtLength.ToString("X2"));

            //Revision
            Console.WriteLine("\tRevision: " + _reader.ReadByte().ToString());

            //Checksum
            Console.WriteLine("\tChecksum: " + _reader.ReadByte().ToString());

            //OEM ID
            Console.WriteLine("\tOEM ID: " + Encoding.ASCII.GetString(_reader.ReadBytes(6)));

            //OEMTableID
            Console.WriteLine("\tOEMTableID: " + Encoding.ASCII.GetString(_reader.ReadBytes(8)));

            //OEMRevision
            Console.WriteLine("\tOEMRevision: " + _reader.ReadUInt32().ToString());

            //OEMRevision
            Console.WriteLine("\tCreatorID: " + _reader.ReadUInt32().ToString());

            //OEMRevision
            Console.WriteLine("\tCreatorRevision: " + _reader.ReadUInt32().ToString());
        }
    }
}
