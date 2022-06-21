using ACPILibs.Parser2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACPIAML.Interupter
{
    public class StackObject
    {
        private StackObject()
        {

        }

        public object? Value;
        public StackObjectType Type;

        public static StackObject Create(string s)
        {
            return new() { Type = StackObjectType.String, Value = s };
        }

        public static StackObject Create(bool s)
        {
            return new() { Type = StackObjectType.Bool, Value = s };
        }
        public static StackObject Create(ParseNode s)
        {
            return new() { Type = StackObjectType.ParseNode, Value = s };
        }
        public static StackObject Create(byte s)
        {
            return new() { Type = StackObjectType.Byte, Value = s };
        }
        public static StackObject Create(short s)
        {
            return new() { Type = StackObjectType.Word, Value = s };
        }
        public static StackObject Create(int s)
        {
            return new() { Type = StackObjectType.DWord, Value = s };
        }
        public static StackObject Create(long s)
        {
            return new() { Type = StackObjectType.QWord, Value = s };
        }
        public static StackObject Create(byte[] s)
        {
            return new() { Type = StackObjectType.ByteArray, Value = s };
        }

        public override string ToString()
        {
            switch (Type)
            {
                case StackObjectType.Null:
                    return "Null";
                case StackObjectType.ParseNode:
                    return (Value as ParseNode).ToString();
                case StackObjectType.String:
                    return (Value as string);
                case StackObjectType.Byte:
                    return ((byte)Value).ToString();
                case StackObjectType.ByteArray:
                    return ((byte[])Value).ToString();
                case StackObjectType.Word:
                    return ((short)Value).ToString();
                case StackObjectType.DWord:
                    return ((int)Value).ToString();
                case StackObjectType.QWord:
                    return ((long)Value).ToString();
                case StackObjectType.Bool:
                    return ((bool)Value).ToString();
                default:
                    return "?";
            }
        }
    }
    public enum StackObjectType
    {
        Null,
        ParseNode,
        String,
        Byte,
        ByteArray,
        /// <summary>
        /// short
        /// </summary>
        Word,
        /// <summary>
        /// int
        /// </summary>
        DWord,
        /// <summary>
        /// long
        /// </summary>
        QWord,
        Bool
    }
}
