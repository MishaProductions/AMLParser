using Cosmos.HAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_var_move(ref lai_variable destination, lai_variable source)
        {
            // Move-by-swap idiom. This handles move-to-self operations correctly.
            lai_variable temp = new();
            lai_swap_object(ref temp, ref source);
            lai_swap_object(ref destination, ref temp);
            lai_var_finalize(temp);
        }

        public static void lai_exec_pkg_store(ref lai_variable in_var, ref lai_variable pkg, ulong i)
        {
            LAI_ENSURE(pkg.type == LAI_PACKAGE, "pkg.type == LAI_PACKAGE");
            lai_exec_pkg_var_store(ref in_var, ref pkg.pkg_items, i);
        }

        private static void lai_exec_pkg_var_store(ref lai_variable in_var, ref lai_variable[] head, ulong i)
        {
            head[i] = in_var;
        }

        public static void lai_swap_object(ref lai_variable first, ref lai_variable second)
        {
            lai_variable temp = first;
            first = second;
            second = temp;
        }

        public static void lai_var_assign(ref lai_variable dest, lai_variable src)
        {
            // Make a local shallow copy of the AML object.
            lai_variable temp = src;
            lai_var_move(ref dest, temp);
        }
    }
}
