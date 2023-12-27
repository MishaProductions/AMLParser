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

        public static int lai_pci_route(ref acpi_resource dest, byte seg, byte bus, byte slot, byte function)
        {
            Global.debugger.Send("Searching route for " + seg + ":" + bus + ":" + ":" + slot + ":" + function);

            PCIDevice device = PCI.GetDevice(bus, slot, function);
            if (device != null && device.DeviceExists)
            {
                byte pin = device.ReadRegister8(0x3D);

                if (pin == 0 || pin > 4)
                    return 1;

                Global.debugger.Send("Pin is " +  pin);

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

            Global.debugger.Send("Finding bus...");

            // Find the PCI bus in the namespace
            lai_nsnode handle = lai_pci_find_bus(seg, bus, ref state);
            if (handle == null)
                return lai_api_error.LAI_ERROR_NO_SUCH_NODE;

            Global.debugger.Send("handle=" + handle.name);

            Global.debugger.Send("Finding _PRT...");

            // Read the PCI routing table
            lai_nsnode prt_handle = lai_resolve_path(handle, "_PRT");
            if (prt_handle == null)
            {
                Global.debugger.Send("host bridge has no _PRT");
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

            Global.debugger.Send("Finding _SB_...");

            lai_nsnode sb_handle = lai_resolve_path(null, "\\_SB_");
            if (sb_handle == null) return null; // Ensuring sb_handle is not null

            Global.debugger.Send("_SB_ found!");

            Global.debugger.Send("Iterating...");

            foreach (var node in sb_handle.children)
            {
                Global.debugger.Send("Iterating inside...");

                if (lai_check_device_pnp_id(node, pci_pnp_id, state) != 0
                && lai_check_device_pnp_id(node, pcie_pnp_id, state) != 0)
                {
                    Global.debugger.Send("Process each node for matching PCI or PCIe IDs...");
                    continue;
                }

                Cosmos.HAL.Global.debugger.Send("debug j");
                lai_variable bus_number = new lai_variable();
                ulong bbn_result = 0;
                lai_nsnode bbn_handle = lai_resolve_path(node, "_BBN");
                if (bbn_handle != null)
                {
                    Global.debugger.Send("Process _BBN method");
                    if (lai_eval(bus_number, bbn_handle, state) != 0)
                    {
                        lai_warn("failed to evaluate _BBN");
                        continue;
                    }
                    lai_obj_get_integer(bus_number, ref bbn_result);
                }
                Cosmos.HAL.Global.debugger.Send("debug l");
                lai_variable seg_number = new lai_variable();
                ulong seg_result = 0;
                lai_nsnode seg_handle = lai_resolve_path(node, "_SEG");
                if (seg_handle != null)
                {
                    Global.debugger.Send("Process _SEG method");
                    if (lai_eval(seg_number, seg_handle, state) != 0)
                    {
                        lai_warn("failed to evaluate _SEG");
                        continue;
                    }
                    lai_obj_get_integer(seg_number, ref seg_result);
                }

                if (seg_result == seg && bbn_result == bus)
                {
                    return node;
                }
            }

            return null;
        }
    }
}
