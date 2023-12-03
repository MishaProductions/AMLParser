using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_read_opregion(ref lai_variable destination, lai_nsnode field)
        {
            if (field.type == LAI_NAMESPACE_FIELD || field.type == LAI_NAMESPACE_INDEXFIELD)
            {
                lai_read_field(ref destination, field);
            }
            else if (field.type == LAI_NAMESPACE_BANKFIELD)
            {
                lai_read_bankfield(destination, field);
            }
            else
            {
                lai_panic("unknown field read");
            }
        }

        private static void lai_read_bankfield(lai_variable destination, lai_nsnode field)
        {
            lai_log("lai_read_bankfield: TODO");
        }

        private static void lai_read_field(ref lai_variable destination, lai_nsnode field)
        {
            ulong bytes = (field.fld_size + 7) / 8;
            if (bytes > 8)
            {
                lai_create_buffer(destination, (int)bytes);
                //lai_read_field_internal(destination.buffer, field);
            }
            lai_log("lai_read_field: TODO");

        }
    }
}
