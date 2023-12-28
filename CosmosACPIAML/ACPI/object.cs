using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CosmosACPIAML.ACPI.LAI;

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
                    Cosmos.HAL.Global.debugger.Send("objectt.integer=" + objectt.integer);
                    outt = objectt.integer;
                    return 0;
                default:
                    lai_warn("lai_obj_get_integer() expects an integer, not a value of type " + objectt.type);
                    return 2;
            }
        }

        public static lai_api_error lai_obj_get_pkg(lai_variable objectt, long i, ref lai_variable outVar)
        {
            Cosmos.HAL.Global.debugger.Send("getting pkg " + i + " rc="+ objectt.pkg_rc + " pkg_items.Length=" + objectt.pkg_items.Length);

            if (objectt.type != LAI.LAI_PACKAGE)
                return lai_api_error.LAI_ERROR_TYPE_MISMATCH;

            if (i >= objectt.pkg_items.Length)
                return lai_api_error.LAI_ERROR_OUT_OF_BOUNDS;

            lai_exec_pkg_load(ref outVar, objectt, i);

            return lai_api_error.LAI_ERROR_NONE;
        }

        public static void lai_exec_pkg_load(ref lai_variable outVar, lai_variable pkg, long i)
        {
            LAI.LAI_ENSURE(pkg.type == LAI.LAI_PACKAGE, "pkg.type == LAI.LAI_PACKAGE");
            lai_exec_pkg_var_load(ref outVar, pkg.pkg_items, i);
        }

        public static void lai_exec_pkg_var_load(ref lai_variable outVar, lai_variable[] head, long i)
        {
            Cosmos.HAL.Global.debugger.Send("getting head[i] " + i + " type=" + head[i].type + " rc =" + head[i].pkg_rc + " pkg_items.Length=" + head[i].pkg_items.Length);

            var items = head[i].pkg_items;
            foreach (var item in items)
            {
                Cosmos.HAL.Global.debugger.Send("item");
                Cosmos.HAL.Global.debugger.Send("item.type=" + item.type);
                Cosmos.HAL.Global.debugger.Send("item.integer=" + item.integer);
            }

            outVar = head[i];
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
