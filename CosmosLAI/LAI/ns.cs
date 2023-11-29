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
            Console.WriteLine("LAI PANIC: "+message);
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
        public Hashtable children = new Hashtable();
    }

    public class lai_state
    {
        public List<lai_ctxitem> ctxstack = new();
        public List<lai_blkitem> blkstack = new();
        public List<lai_stackitem> stack = new();
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
