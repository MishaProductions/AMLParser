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
            lai_variable temp = source;
            dest = temp;
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
