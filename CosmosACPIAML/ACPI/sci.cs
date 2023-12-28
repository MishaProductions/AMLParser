using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        /// <summary>
        /// lai_enable_acpi(): Enables ACPI SCI
        /// </summary>
        /// <param name="mode">0: legacy PIC, 1: IOAPIC</param>
        /// <returns>0 on success</returns>
        public static int lai_enable_acpi(int mode)
        {
            var instance = lai_current_instance();

            /* first run \._SB_._INI */
            var handle = lai_resolve_path(null, "\\_SB_._INI");

            if (handle == null)
            {
                Console.WriteLine("\\_SB_._INI is null");
            }
            else
            {
                lai_state state = new lai_state();
                lai_variable result = new lai_variable();
                if (lai_eval(ref result, handle, state) == 0)
                {
                    Console.WriteLine("evaluated \\_SB_._INI");
                }
                lai_finalize_state(state);
            }

            /* _STA/_INI for all devices */
            handle = lai_resolve_path(null, "\\_SB_");
            lai_init_children(handle);
            return 0;
        }

        private static ulong lai_evaluate_sta(lai_nsnode node)
        {
            // If _STA not present, assume 0x0F as ACPI spec says.
            ulong sta = 0x0f;

            lai_nsnode handle = lai_resolve_path(node, "_STA");
            if (handle != null)
            {
                lai_state state = new lai_state();
                lai_variable result = new lai_variable();

                var err = lai_eval(ref result, handle, state);
                if (err != 0)
                {
                    Console.WriteLine("could not evaluate _STA, ignoring device");
                }

                if (lai_obj_get_integer(result, ref sta) != 0)
                {
                    lai_panic("_STA returned non-integer object");
                }
            }

            return sta;
        }

        private static void lai_init_children(lai_nsnode parent)
        {
            lai_nsnode handle;
            foreach (var node in parent.children)
            {
                if(node.type == LAI_NAMESPACE_DEVICE)
                {
                    ulong sta = lai_evaluate_sta(node);

                    /* if device is present, evaluate its _INI */
                    if ((sta & 1) != 0)
                    {
                        handle = lai_resolve_path(node, "_INI");

                        if (handle != null)
                        {
                            var state = new lai_state();
                            lai_variable result = new lai_variable();
                            if (lai_eval(ref result, handle, state) == 0)
                            {
                                Console.WriteLine("evaluated " + lai_stringify_node_path(handle));
                            }
                            lai_finalize_state(state);
                        }
                    }

                    /* if functional and/or present, enumerate the children */
                    if ((sta & 1) != 0 || (sta & 8) != 0)
                    {
                        lai_init_children(node);
                    }
                }
            }
        }
    }
}
