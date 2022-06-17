using ACPIAML.Interupter;
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
                var type = arg.Value == null ? "<null>" : arg.Value.GetType().Name;
                Console.WriteLine(spacing + $" -- Argument: {ValueToString(arg)}, type: {type}");
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
                        return "Node: " + node.Name + ", val: "+EisaId.ToText((long)node.ConstantValue.Value);
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
