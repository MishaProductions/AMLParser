using Cosmoss.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosLAI.LAI
{
    internal unsafe static partial class lai
    {
        private static lai_instance global_instance = new();
        public static lai_instance lai_current_instance()
        {
            return global_instance;
        }

        public static uint lai_hash_string(string str)
        {
            // Simple djb2 hash function. TODO: Replace by SipHash for DoS resilience.
            uint x = 5381;
            for (int i = 0; i < str.Length; i++)
                x = ((x << 5) + x) + str[i];
            return x;
        }

        public static void lai_install_nsnode(lai_nsnode node)
        {
            global_instance.ns_array.Add(node);

            // Insert the node into its parent's hash table.
            if (node.parent != null)
            {
                uint h = lai_hash_string(node.name);
                if (node.parent.children.ContainsKey(h))
                {
                    Console.WriteLine("node exists! " + node.name);
                    throw new Exception("node exists! " + node.name);
                }
                node.parent.children.Add(h, node);
            }
        }
        private static lai_nsnode lai_ns_get_child(lai_nsnode parent, string name)
        {
            uint h = lai_hash_string(name);
            return (lai_nsnode)parent.children[h];
        }
        private static int lai_amlname_parse(ref lai_amlname amln, byte* ptr, int startIndex)
        {
            amln.is_absolute = false;
            amln.height = 0;
            byte* begin = ptr + startIndex;
            byte* it = begin;
            if (*it == '\\')
            {
                // First character is \ for absolute paths.
                amln.is_absolute = true;
                it++;
            }
            else
            {
                // Non-absolute paths can be prefixed by a number of ^.
                while (*it == '^')
                {
                    amln.height++;
                    it++;
                }
            }
            // Finally, we parse the name's prefix (which determines the number of segments).
            int num_segs;
            if (*it == '\0')
            {
                it++;
                num_segs = 0;
            }
            else if (*it == DUAL_PREFIX)
            {
                it++;
                num_segs = 2;
            }
            else if (*it == MULTI_PREFIX)
            {
                it++;
                num_segs = *it;
                if (!(num_segs > 2))
                {
                    lai_panic("assertion failed: num_segs > 2");
                }
                it++;
            }
            else
            {
                if (!lai_is_name(*it)) { lai_panic("assertion failed: lai_is_name(*it)"); }
                num_segs = 1;
            }
            amln.search_scopes = !amln.is_absolute && amln.height == 0 && num_segs == 1;
            amln.it = it;
            amln.end = it + 4 * num_segs;
            return (int)(amln.end - begin);

        }
        private static bool lai_amlname_done(lai_amlname amln)
        {
            return amln.it == amln.end;
        }
        private static string lai_amlname_iterate(lai_amlname name)
        {
            string result = "";
            for (int i = 0; i < 4; i++)
                result += name.it[i];
            name.it += 4;
            return result;
        }

        private static lai_nsnode lai_do_resolve(lai_nsnode handle, lai_amlname amln)
        {
            lai_nsnode current = handle;
            if (amln.search_scopes)
            {
                var segment = lai_amlname_iterate(amln);

                while (current != null)
                {
                    lai_nsnode node = lai_ns_get_child(current, segment);
                    if (node == null)
                    {
                        current = current.parent;
                        continue;
                    }

                    if (node.type == lai_nsnode_type.Alias)
                    {
                        node = node.al_target;
                    }

                    return node;
                }

                return null;
            }
            else
            {
                if (amln.is_absolute)
                {
                    while (current.parent != null)
                    {
                        current = current.parent;
                    }
                }

                for (int i = 0; i < amln.height; i++)
                {
                    if(current.parent == null)
                    {
                        if (current.type != lai_nsnode_type.Root)
                        {
                            lai_panic("case #234 current type is not root");
                        }
                        break;
                    }

                    current = current.parent;
                }

                if (lai_amlname_done(amln))
                {
                    return current;
                }

                while (!lai_amlname_done(amln))
                {
                    var segment = lai_amlname_iterate(amln);
                    current = lai_ns_get_child(current, segment);
                    if (current == null)
                    {
                        return null;
                    }
                }
                
                if (current.type == lai_nsnode_type.Alias)
                {
                    current = current.al_target;
                }

                return current;
            }
        }
        public static lai_nsnode lai_create_root()
        {
            global_instance.root_node = new();
            global_instance.root_node.type = lai_nsnode_type.Root;
            global_instance.root_node.name = "\\___";

            lai_nsnode sb = new();
            sb.type = lai_nsnode_type.Device;
            sb.name = "_SB_";
            sb.parent = global_instance.root_node;
            lai_install_nsnode(sb);

            lai_nsnode si = new();
            si.type = lai_nsnode_type.Device;
            si.name = "_SI_";
            si.parent = global_instance.root_node;
            lai_install_nsnode(si);

            lai_nsnode gpe = new();
            gpe.type = lai_nsnode_type.Device;
            gpe.name = "_GPE_";
            gpe.parent = global_instance.root_node;
            lai_install_nsnode(gpe);

            // Create nodes for compatibility with ACPI 1.0.
            lai_nsnode pr = new();
            pr.type = lai_nsnode_type.Device;
            pr.name = "_PR_";
            pr.parent = global_instance.root_node;
            lai_install_nsnode(pr);

            lai_nsnode tz = new();
            tz.type = lai_nsnode_type.Device;
            tz.name = "_TZ_";
            tz.parent = global_instance.root_node;
            lai_install_nsnode(tz);


            // TODO: create OS defined objects

            return global_instance.root_node;
        }

        public static void lai_create_namespace()
        {
            // todo: is_hw_reduced
            lai_nsnode root = lai_create_root();

            var state = new lai_state();
            var dsdt = (uint*)ACPINew.FADT->Dsdt;
            if (dsdt == null)
            {
                lai_panic("unable to find ACPI DSDT");
            }

            lai_populate(root, dsdt, state);
        }

        public static void lai_panic(string message)
        {
            Console.WriteLine("LAI PANIC: " + message);
            while (true) { }
        }
    }

    public class lai_instance
    {
        public lai_nsnode root_node;
        public List<lai_nsnode> ns_array = new();
    }

    public class lai_nsnode
    {
        public lai_nsnode_type type;
        public string name;
        public lai_nsnode parent;
        public lai_nsnode al_target;
        public Hashtable children = new Hashtable();
    }

    public class lai_state
    {
        public List<lai_ctxitem> ctxstack = new();
        public List<lai_blkitem> blkstack = new();
        public List<lai_stackitem> stack = new();
        public List<lai_operand> opstack = new();
    }

    public struct acpi_aml
    {

    }

    public enum lai_nsnode_type
    {
        Root = 1,
        Name,
        Alias,
        Field,
        Method,
        Device,
        IndexField,
        Mutex,
        Processor,
        BufferField,
        ThermalZone,
        Event,
        PowerResource,
        BankField,
        OpRegion
    }
}
