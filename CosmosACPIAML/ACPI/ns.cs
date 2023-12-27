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

        public static int lai_check_device_pnp_id(lai_nsnode dev, lai_variable pnp_id, lai_state state)
        {
            lai_variable id = new lai_variable();
            int ret = 1;

            lai_nsnode hid_handle = lai_resolve_path(dev, "_HID");
            
            if (hid_handle != null)
            {
                Cosmos.HAL.Global.debugger.Send("_HID found!");

                if (lai_eval(id, hid_handle, state) != 0)
                {
                    LAI.lai_warn("could not evaluate _HID of device");
                }
                else
                {
                    Cosmos.HAL.Global.debugger.Send("id.type == 0");
                    LAI.LAI_ENSURE(id.type == 0, "id.type == 0");
                }
            }

            if (id.type == 0)
            {
                Cosmos.HAL.Global.debugger.Send("id.type == 0");

                lai_nsnode cid_handle = lai_resolve_path(dev, "_CID");
                if (cid_handle != null)
                {
                    if (lai_eval(id, cid_handle, state) != 0)
                    {
                        LAI.lai_warn("could not evaluate _CID of device");
                        return 1;
                    }
                    else
                    {
                        Cosmos.HAL.Global.debugger.Send("id.type == 0");
                        LAI.LAI_ENSURE(id.type == 0, "id.type == 0");
                    }
                }
            }

            Cosmos.HAL.Global.debugger.Send("id.type=" + id.type + " pnp_id.type=" + pnp_id.type);

            if (id.type == LAI_INTEGER && pnp_id.type == LAI_INTEGER)
            {
                Cosmos.HAL.Global.debugger.Send("id.type == LAI_INTEGER && pnp_id.type == LAI_INTEGER");

                if (id.integer == pnp_id.integer)
                {
                    ret = 0; // IDs match
                }
            }
            else if (id.type == LAI_STRING && pnp_id.type == LAI_STRING)
            {
                Cosmos.HAL.Global.debugger.Send("id.type == LAI_STRING && pnp_id.type == LAI_STRING");

                if (id.stringval == pnp_id.stringval)
                {
                    ret = 0; // String IDs match
                }
            }

            lai_var_finalize(id);
            return ret;
        }

        public static lai_nsnode lai_ns_child_iterate(lai_ns_child_iterator iter)
        {
            while (iter.i < iter.parent.children.Count)
            {
                lai_nsnode n = iter.parent.children[(int)(iter.i++)];
                if (n != null)
                    return n;
            }

            return null;
        }
    }

    public class lai_instance
    {
        public lai_nsnode RootNode;
    }
}
