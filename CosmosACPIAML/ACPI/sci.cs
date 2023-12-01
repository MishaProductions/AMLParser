using System;
using System.Collections.Generic;
using System.Linq;
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
                if (lai_eval(null, handle, state) == 0)
                {
                    Console.WriteLine("evaluated \\_SB_._INI");
                }
                lai_finalize_state(state);
            }
            return 0;
        }
    }
}
