using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        private static lai_instance global_instance = new lai_instance();
        public static lai_instance lai_current_instance()
        {
            return global_instance;
        }
        private static void lai_install_nsnode(lai_nsnode node)
        {
            if (node.parent != null)
            {
                foreach (var child in node.parent.children)
                {
                    if (child.name == node.name)
                    {
                        lai_warn("LAI: Attempt was made to insert duplicate namespace node: " + node.name + ", ignoring");
                        return;
                    }
                }
                node.parent.children.Add(node);
            }
            else
            {
                lai_panic("lai_install_nsnode: Node parrent cannot be null. Node name is " + node.name);
            }
        }
        private static void lai_uninstall_nsnode(lai_nsnode node)
        {
            //todo
        }

        private static lai_nsnode lai_ns_get_root()
        {
            return lai_current_instance().RootNode;
        }

        // ...

        public static lai_nsnode lai_resolve_path(lai_nsnode ctx_handle, string path)
        {
            lai_nsnode current = ctx_handle;
            if (current == null)
            {
                current = lai_current_instance().RootNode;
            }
            int offset = 0;
            if (path[0] == '\\')
            {
                while (current.parent != null)
                {
                    current = current.parent;
                }
                if (current.type != LAI_NAMESPACE_ROOT)
                {
                    lai_panic("expected namespace root in lai_resolve path");
                }
                offset++;
            }
            else
            {
                int height = 0;
                while (path[offset] == '^')
                {
                    height++;
                    offset++;
                }

                for (int i = 0; i < height; i++)
                {
                    if (current.parent == null)
                    {
                        if (current.type != LAI_NAMESPACE_ROOT)
                        {
                            lai_panic("expected namespace root in lai_resolve path");
                        }
                    }
                    current = current.parent;
                }
            }

            if (offset >= path.Length)
            {
                return current;
            }

            for (; ; )
            {
                string segment = "";
                int k;
                for (k = 0; k < 4; k++)
                {
                    if (!lai_is_name((byte)path[offset]))
                        break;
                    segment += path[offset++];
                }

                // ACPI pads names with trailing underscores.
                while (k < 4)
                {
                    segment += "_";
                    k++;
                }

                current = lai_ns_get_child(current, segment);
                if (current == null)
                {
                    return null;
                }

                if (current.type == LAI_NAMESPACE_ALIAS)
                {
                    current = current.al_target;
                }

                if (offset >= path.Length)
                {
                    break;
                }

                if (path[offset] != '.')
                {
                    lai_panic("expected pathptr to have . but got " + path[offset] + ". Requested path was " + path + " segment is " + segment + ", k is " + k);
                }

                offset++;
            }

            return current;
        }

        public static lai_nsnode lai_ns_child_iterate(lai_ns_child_iterator iter)
        {
            // TODO

            return null; // Equivalent to returning NULL in C
        }
    }

    public class lai_instance
    {
        public lai_nsnode RootNode;
    }
}
