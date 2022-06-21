using ACPIAML.Interupter;
using ACPILibs.AML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ACPILibs.Parser2
{
    public class Parser
    {
        private readonly Stream _source;

        public Parser(Stream s)
        {
            _source = s;
        }

        public ParseNode Parse()
        {
            return PreParse();
        }

        private ParseNode PreParse()
        {
            ParseNode root = new(null)
            {
                Name = "\\"
            };

            while (_source.Position < _source.Length)
            {
                ParseNode op = ParseFullOpCodeNode(root);
                root.Nodes.Add(op);
            }

            return root;
        }

        private ParseNode ParseFullOpCodeNode(ParseNode parent)
        {
            //Read the opcode
            ParseNode op = ReadOpCode(parent);
            OpCode info = op.Op;

            _source.Seek(op.DataStart, SeekOrigin.Begin);

            //Parse opcode arguments
            if (info.ParseArgs.Length > 0)
            {
                bool parseArguments = false;

                switch (info.Code)
                {
                    case OpCodeEnum.Byte:
                    case OpCodeEnum.Word:
                    case OpCodeEnum.DWord:
                    case OpCodeEnum.QWord:
                    case OpCodeEnum.String:
                        op.ConstantValue = ParseSimpleArgument(info.ParseArgs[0]);
                        break;

                    case OpCodeEnum.NamePath:
                        op.Arguments = new StackObject[1];
                        op.Arguments[0] = StackObject.Create(ReadNameString());
                        break;

                    default:
                        parseArguments = true;
                        break;
                }

                if (parseArguments) //If the opcode is not a constant
                {
                    op.Arguments = new StackObject[info.ParseArgs.Length];
                    for (int x = 0; x < info.ParseArgs.Length; x++)
                    {
                        switch (info.ParseArgs[x])
                        {
                            case ParseArgFlags.None:
                                break;

                            case ParseArgFlags.ByteData:
                            case ParseArgFlags.WordData:
                            case ParseArgFlags.DWordData:
                            case ParseArgFlags.CharList:
                            case ParseArgFlags.Name:
                            case ParseArgFlags.NameString:
                                {
                                    var arg = ParseSimpleArgument(info.ParseArgs[x]);
                                    if (arg != null)
                                    {
                                        op.Arguments[x] = arg;
                                    }
                                }
                                break;
                            case ParseArgFlags.DataObject:
                            case ParseArgFlags.TermArg:
                                {
                                    var arg = ParseFullOpCodeNode(parent); //parsenode
                                    //if (arg.Op.Name != "NamePath")
                                    //{
                                    //    op.Arguments[x] = StackObject.Create(arg);
                                    //}
                                    //else
                                    //{
                                    //    op.Arguments[x] = arg.Arguments[0];
                                    //    if ((string)op.Arguments[x].Value == "\\_OSI")
                                    //    {
                                    //        ;
                                    //    }
                                    //}
                                    op.Arguments[x] = StackObject.Create(arg);
                                }
                                break;

                            case ParseArgFlags.PackageLength:
                                var xx = op.Length = ReadPackageLength();
                                op.Arguments[x] = StackObject.Create(xx);
                                break;

                            case ParseArgFlags.FieldList:
                                while (_source.Position < op.End)
                                {
                                    op.Arguments[x] = StackObject.Create(ReadField(op));
                                }
                                break;

                            case ParseArgFlags.ByteList:
                                if (_source.Position < op.End)
                                {
                                    op.ConstantValue = StackObject.Create(ReadBytes((int)(op.End - _source.Position)));
                                }
                                break;

                            case ParseArgFlags.DataObjectList:
                            case ParseArgFlags.ObjectList:
                                //var startPosition = _source.Position;
                          
                                while (_source.Position < op.End)
                                {
                                    ParseNode child = ParseFullOpCodeNode(op);
                                    op.Nodes.Add(child);
                                }

                                break;

                            case ParseArgFlags.TermList:
                                //var startPosition = _source.Position;

                                while (_source.Position < op.End)
                                {
                                    ParseNode child = ParseFullOpCodeNode(op);
                                    op.Nodes.Add(child);
                                }

                                break;

                            case ParseArgFlags.Target:
                            case ParseArgFlags.SuperName:
                                ushort subOp = PeekUShort();
                                if (subOp == 0 || Definitions.IsNameRootPrefixOrParentPrefix((byte)subOp) || Definitions.IsLeadingChar((byte)subOp))
                                {
                                    var str = ReadNameString();
                                    op.Arguments[x] = StackObject.Create(str);
                                }
                                else
                                {
                                    var xxx = ParseFullOpCodeNode(op);
                                    if (xxx.Op.Name != "NamePath")
                                    {
                                        op.Arguments[x] = StackObject.Create(xxx);
                                    }
                                    else
                                    {
                                        op.Arguments[x] = xxx.Arguments[0];
                                    }
                                }
                                break;
                            case ParseArgFlags.SimpleName:
                            case ParseArgFlags.NameOrReference:
                                ushort subOp2 = PeekUShort();
                                if (subOp2 == 0 || Definitions.IsNameRootPrefixOrParentPrefix((byte)subOp2) || Definitions.IsLeadingChar((byte)subOp2))
                                {
                                    var str = ReadNameString();
                                    op.Arguments[x] = StackObject.Create(str);
                                }
                                else
                                {
                                    var xxx = ParseFullOpCodeNode(op);
                                    if (xxx.Op.Name != "NamePath")
                                    {
                                        op.Arguments[x] = StackObject.Create(xxx);
                                    }
                                    else
                                    {
                                        op.Arguments[x] = xxx.Arguments[0];
                                    }
                                }
                                break;

                            default:
                                Console.WriteLine("psargs.c / line 913 - Unknown arg: " + info.ParseArgs[x]);
                                break;
                        }
                    }
                }
            }

            //Parse the opcode
            if ((info.Flags & OpCodeFlags.Named) == OpCodeFlags.Named)
            {
                for (int x = 0; x < info.ParseArgs.Length; x++)
                {
                    if (info.ParseArgs[x] == ParseArgFlags.Name)
                    {
                        var name = op.Arguments[x].Value;
                        if (name == null)
                        {
                            throw new("Name should not be null");
                        }
                        op.Name = (string)name;
                        break;
                    }
                }
            }

            if (op.Op.Name == "Scope")
            {
                var orgPosition = op.DataStart;
                while (_source.Position < orgPosition + op.Length)
                {
                    ParseNode op2 = ParseFullOpCodeNode(op);
                    op.Nodes.Add(op2);
                }
            }

            return op;
        }

        private ParseNode ReadField(ParseNode parent)
        {
            OpCodeEnum opCode;
            switch ((OpCodeEnum)PeekByte())
            {
                case OpCodeEnum.FieldOffset:

                    opCode = OpCodeEnum.ReservedField; ;
                    _source.Seek(1, SeekOrigin.Current);
                    break;

                case OpCodeEnum.FieldAccess:

                    opCode = OpCodeEnum.AccessField;
                    _source.Seek(1, SeekOrigin.Current);
                    break;

                case OpCodeEnum.FieldConnection:

                    opCode = OpCodeEnum.Connection;
                    _source.Seek(1, SeekOrigin.Current);
                    break;

                case OpCodeEnum.FieldExternalAccess:

                    opCode = OpCodeEnum.ExternalAccessField;
                    _source.Seek(1, SeekOrigin.Current);
                    break;

                default:
                    opCode = OpCodeEnum.NamedField;
                    break;
            }

            ParseNode node = new(parent)
            {
                Op = OpCodeTable.GetOpcode((ushort)opCode)
            };

            switch (opCode)
            {
                case OpCodeEnum.NamedField:
                    node.Name = Read4ByteName();
                    node.ConstantValue = StackObject.Create(ReadPackageLength());
                    break;
                case OpCodeEnum.ReservedField:
                    node.ConstantValue = StackObject.Create(ReadPackageLength());
                    break;
                case OpCodeEnum.AccessField:
                    node.ConstantValue = StackObject.Create((ReadByte() | ((uint)ReadByte() << 8)));
                    break;
                case OpCodeEnum.ExternalAccessField:
                    node.ConstantValue = StackObject.Create((ReadByte() | ((uint)ReadByte() << 8) | ((uint)ReadByte() << 16)));
                    break;

                default:
                    throw new Exception("psargs.c / line 703");
            }

            return node;
        }

        private int ReadPackageLength()
        {
            int length;

            byte b0 = (byte)ReadByte();

            byte sz = (byte)((b0 >> 6) & 3);

            if (sz == 0)
            {
                length = b0 & 0x3F;
            }
            else if (sz == 1)
            {
                length = ((b0 & 0x0F) | ReadByte() << 4);
            }
            else if (sz == 2)
            {
                length = ((b0 & 0x0F) | ReadByte() << 4) | (ReadByte() << 12);
            }
            else if (sz == 3)
            {
                length = ((b0 & 0x0F) | ReadByte() << 4) | (ReadByte() << 12) | (ReadByte() << 20);
            }
            else
            {
                throw new NotImplementedException();
            }

            return length;
        }

        private StackObject? ParseSimpleArgument(ParseArgFlags arg)
        {
            switch (arg)
            {
                case ParseArgFlags.ByteData:
                    return StackObject.Create((byte)ReadByte());
                case ParseArgFlags.WordData:
                    return StackObject.Create(BitConverter.ToInt16(ReadBytes(2), 0));
                case ParseArgFlags.DWordData:
                    return StackObject.Create(BitConverter.ToInt32(ReadBytes(4), 0));
                case ParseArgFlags.QWordData:
                    return StackObject.Create(BitConverter.ToInt64(ReadBytes(8), 0));
                case ParseArgFlags.CharList: //Nullterminated string
                    string str = string.Empty;

                    byte read;
                    while ((read = (byte)ReadByte()) != 0)
                        str += (char)read;

                    return StackObject.Create(str);
                case ParseArgFlags.Name:
                case ParseArgFlags.NameString:
                    return StackObject.Create(ReadNameString());
            }

            return null;
        }
        private string ReadNameString()
        {
            var x = _source.Position; //for debugging
            var b = (char)ReadByte();
            bool is_absolute = false;
            int height = 0;
            if (b == '\\')
            {
                is_absolute = true;

                //todo: is this correct? this isn't in LAI
                b = (char)ReadByte();
            }
            else
            {
                while (b == '^')
                {
                    b = (char)ReadByte();
                    height++;
                }
            }

            int segmentNumber;
            if (b == '\0')
            {
                segmentNumber = 0;
            }
            else if (b == 0x2E)
            {
                //dual prefix
                segmentNumber = 2;
            }
            else if (b == 0x2F)
            {
                //dual prefix
                segmentNumber = ReadByte();
            }
            else
            {
                segmentNumber = 1; //default?
                _source.Position--;
            }
            var oldSegNum = segmentNumber;
            segmentNumber = (((int)_source.Position + 4 * oldSegNum) - (int) _source.Position) / 4;
            
           var end = _source.Position + 4 * segmentNumber;

            //var MaxLen = 1 //Leading \ for absolute paths.
            //    + height // Leading ^ characters
            //    + segmentNumber * 5 //Segments, seperated by dots.
            //    + 1; //Null-terminator

            string o = "";
            if (is_absolute)
            {
                o += "\\";
            }

            for (int i = 0; i < height; i++)
            {
                o += "^";
            }

            if (_source.Position != end)
            {
                for(; ; )
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var c = (char)ReadByte();
                        if (c == '\0')
                        {
                            //Some 1-byte strings end with a null byte
                            return o;
                        }
                        o += c;
                        if (!char.IsAscii(c))
                            throw new Exception("Expected ASCII character");


                    }
                    if (_source.Position == end)
                    {
                        break;
                    }
                    o += ".";
                }
            }


            return o;
        }

        private ParseNode ReadOpCode(ParseNode parent)
        {
            long pos = _source.Position;

            ushort op = ReadUShort();
            OpCode? info = OpCodeTable.GetOpcode(op);
            switch (info.Class)
            {
                case OpCodeClass.ASCII:
                case OpCodeClass.Prefix:
                    info = OpCodeTable.GetOpcode((ushort)OpCodeEnum.NamePath);
                    pos -= 1; //The op code byte is the data itself
                    break;
                case OpCodeClass.ClassUnknown:
                    Console.WriteLine("Unknown AML opcode: 0x" + op.ToString("X") + " at " + _source.Position);
                    break;
                default:
                    _source.Seek(info.CodeByteSize, SeekOrigin.Current);
                    break;
            }

            return new ParseNode(parent)
            {
                Op = info,
                Start = pos,
                DataStart = pos + info.CodeByteSize
            };
        }

        private string Read4ByteName()
        {
            byte[] dt = new byte[4];
            _source.Read(dt, 0, 4);

            return Encoding.ASCII.GetString(dt);
        }

        private byte[] ReadBytes(int num)
        {
            byte[] temp = new byte[num];
            _source.Read(temp, 0, num);

            return temp;
        }

        private byte PeekByte()
        {
            byte read = (byte)_source.ReadByte();

            _source.Seek(-1, SeekOrigin.Current);

            return read;
        }

        private ushort PeekUShort()
        {
            ushort code = (ushort)_source.ReadByte();
            if (code == Definitions.ExtendedOpCodePrefix)
            {
                code = (ushort)((code << 8) | (ushort)_source.ReadByte());

                _source.Seek(-2, SeekOrigin.Current);
            }
            else
            {
                _source.Seek(-1, SeekOrigin.Current);
            }

            return code;
        }

        private byte ReadByte()
        {
            return (byte)_source.ReadByte();
        }

        private ushort ReadUShort()
        {
            ushort code = (ushort)_source.ReadByte();
            if (code == Definitions.ExtendedOpCodePrefix)
            {
                code = (ushort)((code << 8) | (ushort)_source.ReadByte());
            }

            return code;
        }
    }
}
