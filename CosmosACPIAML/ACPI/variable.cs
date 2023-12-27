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
        public static void lai_swap_object(ref lai_variable first, ref lai_variable second)
        {
            lai_variable temp = first;
            first = second;
            second = temp;
        }
    }
}
