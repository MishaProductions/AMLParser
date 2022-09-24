using ACPIAML.ACPI.Interupter;
using ACPIAML.Interupter;
using ACPILibs.Parser2;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace CosmosACPIAMl
{
    public class Program
    {
        static FileStream? _sdt;
        static BinaryReader? _reader;

        static uint _sdtLength;
        static void Main(string[] args)
        {
            //lenovo.aml: Taken from a Lenovo Yoga 20C0
            //test.aml: Simple aml code
            //qemu.aml: taken from qemu
            //gigabyte.dat: taken from Gigabyte A520M DS3H motherboard
            //elitebook.dat: Taken from HP elitebook 8470p

            _sdt = File.OpenRead(@"test.aml");
            _reader = new BinaryReader(_sdt);
            Stopwatch w = new();
            //STUFF
            {
                //ReadHeader();

                //w.Start();
                //var dsdt = new Parser(_sdt);
                //var root = dsdt.Parse();
                //w.Stop();
                //if (root != null)
                //{
                //    foreach (var item in root.Nodes)
                //    {
                //        DisplayNode(item, " ");
                //    }
                //}
                Console.WriteLine("Running interupter");
                _sdt = File.OpenRead(@"test.aml");
                _reader = new BinaryReader(_sdt);
                _reader.ReadBytes(36);
                Interupter i = new();
                i.AddTable(new Parser(_sdt));
                i.Start();
                
            }

            _sdt.Close();

            Console.WriteLine("Finished! It took "+w.Elapsed.ToString());
        }

        private static void DisplayNode(ParseNode item, string spacing)
        {
            if (item.Op.Name == "Scope")
            Console.WriteLine(spacing + "Scope: " + item.Name);
            else
                Console.WriteLine(spacing + "Node: " + item.Name);
            Console.WriteLine(spacing + " - OpCode: " + item.Op.ToString());
            Console.WriteLine(spacing + $" - Length: {item.Length}, DataStart: {item.DataStart}, DataEnd: {item.DataStart + item.Length}");
            if (item.Arguments != null)
            foreach (var arg in item.Arguments)
            {
                if(arg != null)
                {
                    var type = arg.Value == null ? "<null>" : arg.Value.GetType().Name;
                    Console.WriteLine(spacing + $" -- Argument: {ValueToString(arg)}, type: {type}");
                }
            }

            spacing += "  ";
            foreach (var n in item.Nodes)
            {
                DisplayNode(n, spacing);
            }
        }
        private static string ValueToString(StackObject val)
        {
            if (val.Type != StackObjectType.Null && val.Value == null)
            {
                throw new Exception("Type != Null while value == null");
            }
            //to make VS happy
            if (val.Value == null)
            {
                throw new Exception();
            }
            switch (val.Type)
            {
                case StackObjectType.Null:
                    return "<null>";
                case StackObjectType.ParseNode:
                    var node = (ParseNode)val.Value;
                    if (node.Op.ToString() == "DWord")
                    {
                        if (node.ConstantValue == null)
                            throw new Exception("Constant value should not be null");
                        if (node.ConstantValue.Value == null)
                            throw new Exception("Constant value should not be null");
                        return "Node: " + node.Name + ", val: "+EisaId.ToText((int)node.ConstantValue.Value);
                    }
                    else
                    {
                        return "Node: " + node.Name + ", OP: " + node.Op.ToString();
                    }
                case StackObjectType.String:
                    return (string)val.Value;
                case StackObjectType.Byte:
                    return "0x"+((byte)val.Value).ToString("X2");
                case StackObjectType.Word:
                    return "0x" + ((short)val.Value).ToString("X2");
                case StackObjectType.DWord:
                    return "0x" + ((int)val.Value).ToString("X4");
                case StackObjectType.QWord:
                    return "0x" + ((long)val.Value).ToString("X8");
                default:
                    break;
            }
            return "<unknown>";
        }
        static void ReadHeader()
        {
            if (_reader == null)
                throw new("reader should not be null");
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
