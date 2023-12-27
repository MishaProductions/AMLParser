using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_create_buffer(lai_variable objectt, int size)
        {
            objectt.type = LAI_BUFFER;
            objectt.buffer = new byte[size];
        }

        public static int lai_obj_get_integer(lai_variable objectt, ref ulong outt)
        {
            switch(objectt.type)
            {
                case LAI_INTEGER:
                    outt = objectt.integer;
                    return 0;
                default:
                    lai_warn("lai_obj_get_integer() expects an integer, not a value of type " + objectt.type);
                    return 2;
            }
        }

        public static void lai_obj_clone(ref lai_variable dest, lai_variable source)
        {
            // Clone into a temporary object.
            Cosmos.HAL.Global.debugger.Send("debug j");
            lai_variable temp = new lai_variable();

            switch (source.type)
            {
                case LAI_STRING:
                    Cosmos.HAL.Global.debugger.Send("lai_clone_string begin");
                    lai_clone_string(ref temp, source);
                    Cosmos.HAL.Global.debugger.Send("lai_clone_string end");
                    break;
                case LAI_BUFFER:
                    Cosmos.HAL.Global.debugger.Send("lai_clone_buffer begin");
                    lai_clone_buffer(ref temp, source);
                    Cosmos.HAL.Global.debugger.Send("lai_clone_buffer end");
                    break;
                case LAI_PACKAGE:
                    Cosmos.HAL.Global.debugger.Send("lai_clone_package begin");
                    lai_clone_package(ref temp, source);
                    Cosmos.HAL.Global.debugger.Send("lai_clone_package end");
                    break;
                default:
                    Cosmos.HAL.Global.debugger.Send("source.type=" + source.type);
                    break;
            }

            Cosmos.HAL.Global.debugger.Send("temp.type=" + temp.type);

            if (temp.type == 0)
            {
                // Afterwards, swap to the destination. This handles copy-to-self correctly.
                lai_swap_object(ref dest, ref temp);
                lai_var_finalize(temp);
            }
            else
            {
                // For others objects: just do a shallow copy.
                lai_var_assign(ref dest, source);
            }
        }

        public static void lai_clone_string(ref lai_variable dest, lai_variable source)
        {
            dest.stringval = new string(source.stringval.ToCharArray());
        }

        public static void lai_clone_buffer(ref lai_variable dest, lai_variable source)
        {
            long size = source.buffer.Length;
            Array.Copy(source.buffer, dest.buffer, size);
        }

        public static void lai_clone_package(ref lai_variable dest, lai_variable src)
        {
            long n = src.pkg_items.Length;
            for (long i = 0; i < n; i++)
                lai_obj_clone(ref dest.pkg_items[(int)i], src.pkg_items[(int)i]);
        }
    }
}
