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
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_OSI" });
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_OS_" });
            RootNode.Nodes.Add(new ParseNode(RootNode) { Name = "_REV" });

            //self test
            var x = ResolvePath(RootNode.Nodes[1], "\\");
            if (x != RootNode)
            {
                throw new Exception("self test failed");
            }
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
            /* first run \._SB_._INI */
            var x = ResolvePath(RootNode, "\\_SB_._INI");
            if (x != null)
            {
                Execute(x, new());
            }


            //var allNodes = GetAllNodes(RootNode);
            //foreach (var item in allNodes)
            //{
            //    if(item.Op != null)
            //    {
            //        if (item.Op.Name == "Device")
            //        {
            //            if (item.Parent == null)
            //                throw new NullReferenceException();


            //        }
            //    }
            //}
        }

        private StackObject Execute(ParseNode method, MethodState state)
        {
            if (method.Op.Name == "Method" || method.Op.Name == "If" || method.Op.Name == "Else")
            {
               
            }
            else
            {
                return ExecSpecialOp(method, state);
            }
            for (int i = 0; i < method.Nodes.Count; i++)
            {
                ParseNode? nextOp = null;
                if (i >= method.Nodes.Count-1)
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

                    var x = (ParseNode)currentOp.Arguments[1].Value;
                    var val = Execute(x, state);
                    if ((bool)val.Value == true)
                    {
                        return Execute(currentOp, state);
                    }
                    else
                    {
                        if (elseStatement)
                        {
                            return Execute(nextOp, state);
                        }
                    }
                }
                else if (currentOp.Op.Name == "Return")
                {
                    if (currentOp.Arguments.Count > 0)
                    {
                        return currentOp.Arguments[0];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    throw new Exception("Unknown opcode: " + currentOp.Op.Name);
                }
            }

            return null;
        }

        private StackObject ExecSpecialOp(ParseNode method, MethodState state)
        {
            if (method.Op.Class != OpCodeClass.Execute)
                throw new Exception("Attempt to execute non executable code");

            if (method.Op.Name == "ConditionalReferenceOf")
            {
                var obj = (string)method.Arguments[0].Value;
                var outRegister = method.Nodes[0].Op.Name;

                var o = ResolvePath(RootNode, obj);
                if (o == null)
                {
                    throw new Exception("Unable to create object: " + obj);
                }

                //todo: do this correctly
                state.LocalRegisters[outRegister] = StackObject.Create(o);
                return StackObject.Create(true);
            }
            else
            {
                throw new NotImplementedException("Special OpCode not implemented: "+method.Op.Name);
            }
            return null;
        }

        public ParseNode? ResolvePath(ParseNode node, string path)
        {
            var pathIdx = 0;
            var ptr = node;

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
                    pathIdx++;
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
                LocalRegisters.Add("Local"+i, null);
            }
        }
    }
}
