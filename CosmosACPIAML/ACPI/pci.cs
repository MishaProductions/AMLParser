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
        public const string ACPI_PCI_ROOT_BUS_PNP_ID = "PNP0A03";
        public const string ACPI_PCIE_ROOT_BUS_PNP_ID = "PNP0A08";

        public enum lai_api_error
        {
            LAI_ERROR_NONE,
            LAI_ERROR_OUT_OF_MEMORY,
            LAI_ERROR_TYPE_MISMATCH,
            LAI_ERROR_NO_SUCH_NODE,
            LAI_ERROR_OUT_OF_BOUNDS,
            LAI_ERROR_EXECUTION_FAILURE,

            LAI_ERROR_ILLEGAL_ARGUMENTS,

            /* Evaluating external inputs (e.g., nodes of the ACPI namespace) returned an unexpected result.
             * Unlike LAI_ERROR_EXECUTION_FAILURE, this error does not indicate that
             * execution of AML failed; instead, the resulting object fails to satisfy some
             * expectation (e.g., it is of the wrong type, has an unexpected size, or consists of
             * unexpected contents) */
            LAI_ERROR_UNEXPECTED_RESULT,

            // Error given when end of iterator is reached, nothing to worry about
            LAI_ERROR_END_REACHED,

            LAI_ERROR_UNSUPPORTED,
        };

        public struct acpi_resource
        {
            byte type;
            ulong _base; // valid for everything
            ulong length; // valid for I/O and MMIO
            byte address_space; // these are valid --
            byte bit_width; // -- only for --
            byte bit_offset; // -- generic registers
            byte irq_flags; // valid for IRQs
        };

        public struct lai_ns_iterator
        {
            public long i;
        }

        public struct lai_ns_child_iterator
        {
            public long i;
            public lai_nsnode parent;
        }

        public static int lai_pci_route(ref acpi_resource dest, byte seg, byte bus, byte slot, byte function)
        {
            Console.WriteLine("Searching route for " + seg + ":" + bus + ":" + ":" + slot + ":" + function);

            PCIDevice device = PCI.GetDevice(bus, slot, function);
            if (device != null && device.DeviceExists)
            {
                byte pin = device.ReadRegister8(0x3D);

                if (pin == 0 || pin > 4)
                    return 1;

                Console.WriteLine("Pin is " +  pin);

                if (lai_pci_route_pin(ref dest, seg, bus, slot, function, pin) != lai_api_error.LAI_ERROR_NONE)
                    return 1;
                return 0;
            }
            return 1;
        }

        public static lai_api_error lai_pci_route_pin(ref acpi_resource dest, ushort seg, byte bus, byte slot, byte function, byte pin)
        {
            lai_state state = new();
            lai_init_state(ref state);

            if (pin == 0 || pin > 4)
                return lai_api_error.LAI_ERROR_NO_SUCH_NODE; // LAI_ENSURE equivalent

            // Adjusting pin number for ACPI
            pin--;

            Console.WriteLine("Finding bus...");

            // Find the PCI bus in the namespace
            lai_nsnode handle = lai_pci_find_bus(seg, bus, ref state);
            if (handle == null)
                return lai_api_error.LAI_ERROR_NO_SUCH_NODE;

            // Read the PCI routing table
            lai_nsnode prt_handle = lai_resolve_path(handle, "_PRT");
            if (prt_handle == null)
            {
                Console.WriteLine("host bridge has no _PRT");
                return lai_api_error.LAI_ERROR_NO_SUCH_NODE;
            }

            // TODO

            return lai_api_error.LAI_ERROR_NONE;
        }

        public static lai_nsnode lai_pci_find_bus(ushort seg, byte bus, ref lai_state state)
        {
            lai_variable pci_pnp_id = new lai_variable();
            lai_variable pcie_pnp_id = new lai_variable();
            lai_eisaid(ref pci_pnp_id, ACPI_PCI_ROOT_BUS_PNP_ID);
            lai_eisaid(ref pcie_pnp_id, ACPI_PCIE_ROOT_BUS_PNP_ID);

            lai_nsnode sb_handle = lai_resolve_path(null, "\\_SB_");
            if (sb_handle == null) return null; // Ensuring sb_handle is not null

            var iter = new lai_ns_child_iterator(); // Initialize the iterator
            lai_nsnode node;

            while ((node = lai_ns_child_iterate(iter)) != null)
            {
                
            }

            return null;
        }
    }
}
