using ACPIAML.Interupter;
using ACPILibs.AML;
using ACPILibs.Parser2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ACPIAML.ACPI.Interupter
{
    public class Interupter
    {
        public ParseNode RootNode;
        public Interupter()
        {
            RootNode = new ParseNode(null) { Name = "\\" };
            //RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_SB_" });
            //RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_SI_" });
            //RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_GPE" });
            //RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_PR_" });
            //RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_TZ_" });

            //OS specific
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_OSI", Override = OSIOverride });
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_OS_" });
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_REV" });

            //self test
            var x = ResolvePath(RootNode.Nodes[1], "\\");
            if (x != RootNode)
            {
                throw new Exception("self test failed");
            }
        }
        public string[] SupportedOSes = new string[]
        {
            "Windows 2000", /* Windows 2000 */
            "Windows 2001", /* Windows XP */
            "Windows 2001 SP1", /* Windows XP SP1 */
            "Windows 2001.1", /* Windows Server 2003 */
            "Windows 2006", /* Windows Vista */
            "Windows 2006.1", /* Windows Server 2008 */
            "Windows 2006 SP1", /* Windows Vista SP1 */
            "Windows 2006 SP2", /* Windows Vista SP2 */
            "Windows 2009", /* Windows 7 */
            "Windows 2012", /* Windows 8 */
            "Windows 2013", /* Windows 8.1 */
            "Windows 2015" /* Windows 10 */
        };
        private StackObject OSIOverride(ParseNode[] args)
        {
            uint ret = 0;
            var str = (string)args[0].ConstantValue.Value;
            Console.WriteLine("_OSI: " + str);
            foreach (var item in SupportedOSes)
            {
                if (item == str)
                {
                    ret = 0xFFFFFFFF;
                    break;
                }
            }

            return StackObject.Create((int)ret);
        }

        public void AddTable(Parser t)
        {
            var v = t.Parse();
            foreach (var item in v.Nodes)
            {
                if (item.Name != null)
                {
                    var n = GetNode(item.Name);
                    if (n != null)
                    {
                        foreach (var subNodes in item.Nodes)
                        {
                            n.Nodes.Add(subNodes);
                        }
                    }
                    else
                    {
                        RootNode.Nodes.Add(item);
                    }
                }
            }
        }
        public void Start()
        {
            //first run \._SB_._INI
            var handle = ResolvePath(RootNode, "\\_SB_._INI");
            DumpMethod(handle);
            if (handle != null)
            {
                Execute(handle, new(), new());
            }
            else
            {
                Console.WriteLine("warn: unable to find \\_SB_\\_INI");
            }

            //handle = ResolvePath(RootNode, "\\_SB_");
            //if (handle == null)
            //{
            //    throw new Exception("_SB_ should exist.");
            //}
            ////_STA/_INI for all devices
            //InitChildren(handle);

            ////tell the firmware about the IRQ mode
            //handle = ResolvePath(RootNode, "\\_PIC");
            //if (handle != null)
            //{
            //    Execute(handle, new(), new());
            //}
            //else
            //{
            //    Console.WriteLine("warn: unable to find \\_PIC");
            //}
        }

        private void DumpMethod(ParseNode? handle)
        {
            if (handle != null)
            {
                Console.WriteLine("Method - " + handle.Name);

                foreach (var node in handle.Nodes)
                {
                    ;
                }
            }
        }

        private void InitChildren(ParseNode handle)
        {
            foreach (var item in handle.Nodes)
            {
                if (item.Op.Name == "Device")
                {
                    var sta = EvalSTA(item);
                }
            }
        }
        private ulong EvalSTA(ParseNode node)
        {
            // If _STA not present, assume 0x0F as ACPI spec says.
            ulong STA = 0x0F;

            var handle = ResolvePath(node, "_STA");
            if (handle != null)
            {
                var r = Execute(handle, new MethodState(), new());
                if (r == null) throw new Exception("_STA returned null");
                if (r.Type != StackObjectType.DWord) throw new Exception("_STA returned invaild type");
                STA = (ulong)r.Value;
            }

            return STA;
        }
        public Dictionary<string, StackObject> fields = new Dictionary<string, StackObject>();
        private StackObject Execute(ParseNode method, MethodState state, List<ParseNode> args)
        {
            if (method.Op == null)
            {
                return method.Override(args.ToArray());
            }
            if (method.Op.Name == "Method" || method.Op.Name == "If" || method.Op.Name == "Else" || method.Op.Name == "Return")
            {

            }
            else
            {
                return ExecSpecialOp(method, state, args);
            }
            int i = 0;
            i += args.Count;

            for (; i < method.Nodes.Count; i++)
            {
                ParseNode? nextOp = null;
                if (i >= method.Nodes.Count - 1)
                { }
                else
                {
                    nextOp = method.Nodes[i + 1];
                }

                ParseNode? prevOp = null;
                if (i != 0)
                {
                    prevOp = method.Nodes[i - 1];
                }

                ParseNode currentOp = method.Nodes[i];

                if (currentOp.Op.Name == "If")
                {
                    //check if we have else statement
                    var elseStatement = false;
                    if (nextOp != null)
                    {
                        if (nextOp.Name == "Else")
                        {
                            elseStatement = true;
                        }
                    }

                    var MethodToCall = (ParseNode)currentOp.Arguments[1].Value;

                    var nodes = (ParseNode)MethodToCall;
                    List<ParseNode> args2 = new List<ParseNode>();
                    var z = 0;
                    if (currentOp.Nodes.Count != 0)
                    {

                        while (true)
                        {
                            var op = currentOp.Nodes[z];
                            if (op.Op.Class == OpCodeClass.Argument)
                            {
                                args2.Add(op);
                                i++;
                                z++;

                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    var val = Execute(MethodToCall, state, args2);
                    if ((int)val.Value != 0)
                    {
                        List<ParseNode> args3 = new List<ParseNode>();
                        var zz = 0;
                        if (currentOp.Nodes.Count != 0)
                        {

                            //while (true)
                            //{
                            //    var op = currentOp.Nodes[z].Nodes[zz];
                            //    if (op.Op.Class == OpCodeClass.Argument)
                            //    {
                            //        args3.Add(op);
                            //        zz++;
                            //    }
                            //    else
                            //    {
                            //        break;
                            //    }
                             
                            //}
                        }

                        var x=  Execute(currentOp, state, args3);
                    }
                    else
                    {
                        if (elseStatement)
                        {
                            var x = Execute(nextOp, state, new());
                        }
                    }
                }
                else if (currentOp.Op.Name == "Return")
                {
                    if (currentOp.Arguments.Length > 0)
                    {
                        return currentOp.Arguments[0];
                    }
                    else
                    {
                        return null;
                    }
                }
                else if (currentOp.Op.Name == "Store")
                {
                    var val = currentOp.Arguments[0];
                    var var = currentOp.Arguments[1];
                    
                    if (var.Value is string s)
                    {
                        var field = ResolvePath(RootNode, s);
                        if(field == null)
                        {
                            throw new Exception("Cannot resolve field: " + field + ", maybe its not in the root node?");
                        }
                        Console.WriteLine("Writing " + val.ToString() + " to " + s);
                        if (fields.ContainsKey(s))
                        {
                            fields[s] = val;
                        }
                        else
                        {
                            fields.Add(s, val);
                        }
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
                else if (currentOp.Op.Name == "String")
                {
                    if (currentOp.Arguments != null)
                    Console.WriteLine("Skipped string opcode: " + (string)currentOp.Arguments[1].Value);
                    else
                        Console.WriteLine("Skipped invaild string opcode");
                }
                else
                {
                    throw new Exception("Unknown opcode: " + currentOp.Op.Name);
                }
            }

            return null;
        }

        private StackObject ExecSpecialOp(ParseNode method, MethodState state, List<ParseNode> args)
        {
            if(method.Arguments.Length>0)
            {
                if (method.Op.Name == "NamePath")
                {
                    ;
                    var o = ResolvePath(RootNode, (string)method.Arguments[0].Value);
                    if (o == null)
                    {
                        throw new Exception("Invaild OpCode: Found Namepath instruction, however could not find it's value: " + (string)method.Arguments[0].Value);
                    }

                    return Execute(o, new MethodState(), args);
                }
            }
            if (method.Op.Class != OpCodeClass.Execute)
                throw new Exception("Attempt to execute non executable code");

            if (method.Op.Name == "ConditionalReferenceOf")
            {
                var obj = (string)method.Arguments[0].Value;
                var outRegister = ((ParseNode)method.Arguments[1].Value).Op.Name;

                var o = ResolvePath(RootNode, obj);
                if (o == null)
                {
                    throw new Exception("Unable to create object: " + obj);
                }

                //todo: do this correctly
                state.LocalRegisters[outRegister] = StackObject.Create(o);
                return StackObject.Create(1);
            }
            else
            {
                throw new NotImplementedException("Special OpCode not implemented: " + method.Op.Name);
            }
            return null;
        }

        public ParseNode? ResolvePath(ParseNode node, string path)
        {
            var pathIdx = 0;
            var ptr = node;
            bool startswithSlash = path.StartsWith("\\");

            if (path == "\\")
            {

                while (ptr.Parent != null)
                {
                    ptr = ptr.Parent;
                }
                pathIdx++;
            }
            else
            {
                int height = 0;


                while (path[pathIdx] == '^')
                {
                    height++;
                    pathIdx++;
                }

                for (int i = 0; i < height; i++)
                {
                    if (ptr.Parent != null)
                    {
                        if (node.Parent == RootNode)
                        {
                            break;
                        }
                        else
                        {
                            throw new Exception("parent should only be null if root node");
                        }
                    }
                    ptr = node.Parent;
                }
            }

            if (pathIdx >= path.Length - 1)
            {
                return ptr;
            }

            while (true)
            {
                char[] segment = new char[4];
                int k;
                for (k = 0; k < 4; k++)
                {
                    if (!Definitions.IsName((byte)path[pathIdx]))
                    {
                        break;
                    }
                    if (!startswithSlash) { startswithSlash = true; }
                    else { pathIdx++; }
                    segment[k] = path[pathIdx];


                }

                // ACPI pads names with trailing underscores.
                while (k < 4)
                {
                    segment[k++] = '_';
                }

                ptr = GetChild(ptr, new string(segment));
                if (ptr == null)
                {
                    return null;
                }

                if (pathIdx >= path.Length - 1)
                {
                    break;
                }
                pathIdx++;
            }

            return ptr;
        }

        private ParseNode? GetChild(ParseNode ptr, string v)
        {
            foreach (var item in ptr.Nodes)
            {
                if (item.Name == v)
                {
                    return item;
                }
            }
            return null;
        }

        private List<ParseNode> GetAllNodes(ParseNode root, List<ParseNode>? n = null)
        {
            if (n == null)
            {
                n = new List<ParseNode>();
            }
            foreach (var item in root.Nodes)
            {
                GetAllNodes(item, n);
                n.Add(item);
            }
            return n;
        }
        private ParseNode GetNode(string name)
        {
            foreach (var item in RootNode.Nodes)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }
            return null;
        }
    }
    internal class MethodState
    {
        public Dictionary<string, StackObject> LocalRegisters = new Dictionary<string, StackObject>();

        public MethodState()
        {
            for (int i = 0; i < 8; i++)
            {
                LocalRegisters.Add("Local" + i, null);
            }
        }
    }
}
