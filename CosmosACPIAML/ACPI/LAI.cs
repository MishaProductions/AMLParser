using Cosmos.Core;
using Cosmos.HAL;
using CosmosACPIAMl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using static Cosmoss.Core.ACPI;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        private const int LAI_NAMESPACE_ROOT = 1;
        private const int LAI_NAMESPACE_NAME = 2;
        private const int LAI_NAMESPACE_ALIAS = 3;
        private const int LAI_NAMESPACE_FIELD = 4;
        private const int LAI_NAMESPACE_METHOD = 5;
        private const int LAI_NAMESPACE_DEVICE = 6;
        private const int LAI_NAMESPACE_INDEXFIELD = 7;
        private const int LAI_NAMESPACE_MUTEX = 8;
        private const int LAI_NAMESPACE_PROCESSOR = 9;
        private const int LAI_NAMESPACE_BUFFER_FIELD = 10;
        private const int LAI_NAMESPACE_THERMALZONE = 11;
        private const int LAI_NAMESPACE_EVENT = 12;
        private const int LAI_NAMESPACE_POWERRESOURCE = 13;
        private const int LAI_NAMESPACE_BANKFIELD = 14;
        private const int LAI_NAMESPACE_OPREGION = 15;

        private const int LAI_POPULATE_STACKITEM = 1;
        private const int LAI_METHOD_STACKITEM = 2;
        private const int LAI_LOOP_STACKITEM = 3;
        private const int LAI_COND_STACKITEM = 4;
        private const int LAI_BUFFER_STACKITEM = 5;
        private const int LAI_PACKAGE_STACKITEM = 6;
        /// <summary>
        /// Parse a namespace leaf node (i.e., not a scope).
        /// </summary>
        private const int LAI_NODE_STACKITEM = 7;
        /// <summary>
        /// Parse an operator.
        /// </summary>
        private const int LAI_OP_STACKITEM = 8;
        /// <summary>
        /// Parse a method invocation.
        /// </summary>
        private const int LAI_INVOKE_STACKITEM = 9;
        /// <summary>
        /// Parse a return operand
        /// </summary>
        private const int LAI_RETURN_STACKITEM = 10;
        /// <summary>
        /// Parse a BankValue and FieldList
        /// </summary>
        private const int LAI_BANKFIELD_STACKITEM = 11;
        private const int LAI_VARPACKAGE_STACKITEM = 12;
        /// <summary>
        /// Evaluate constant data (and keep result). Primitive objects are parsed. Names are left unresolved. Operations (e.g. Add()) are not allowed.
        /// </summary>
        private const int LAI_DATA_MODE = 1;
        // Evaluate dynamic data (and keep result).
        //     Primitive objects are parsed.
        //     Names are resolved. Methods are executed.
        //     Operations are allowed and executed.
        private const int LAI_OBJECT_MODE = 2;
        /// <summary>
        ///  Like LAI_OBJECT_MODE, but discard the result.
        /// </summary>
        private const int LAI_EXEC_MODE = 3;
        private const int LAI_UNRESOLVED_MODE = 4;
        // Operation is expected to return a result (on the opstack).
        private const int LAI_MF_RESULT = 1;
        // Resolve names to namespace nodes.
        private const int LAI_MF_RESOLVE = 2;
        // Allow unresolvable names.
        private const int LAI_MF_NULLABLE = 4;
        // Parse method invocations.
        // Requires LAI_MF_RESOLVE.
        private const int LAI_MF_INVOKE = 8;

        private static byte[] lai_mode_flags = new byte[] { };


        // Name types: unresolved names and names of certain objects.
        private const int LAI_OPERAND_OBJECT = 1;
        private const int LAI_NULL_NAME = 2;
        private const int LAI_UNRESOLVED_NAME = 3;
        private const int LAI_RESOLVED_NAME = 4;
        private const int LAI_ARG_NAME = 5;
        private const int LAI_LOCAL_NAME = 6;
        private const int LAI_DEBUG_NAME = 7;

        private const int LAI_INTEGER = 1;
        private const int LAI_STRING = 2;
        private const int LAI_BUFFER = 3;
        private const int LAI_PACKAGE = 4;
        private const int LAI_HANDLE = 5;
        private const int LAI_LAZY_HANDLE = 6;
        /// <summary>
        /// Reference types: obtained from RefOf() or CondRefOf()
        /// </summary>
        private const int LAI_ARG_REF = 7;
        private const int LAI_LOCAL_REF = 8;
        private const int LAI_NODE_REF = 9;
        private const int LAI_STRING_INDEX = 10;
        private const int LAI_BUFFER_INDEX = 11;
        private const int LAI_PACKAGE_INDEX = 12;
        private const int LAI_COND_BRANCH = 1;

        #region opcodes
        private const int ZERO_OP = 0x00;
        private const int ONE_OP = 0x01;
        private const int ALIAS_OP = 0x06;
        private const int NAME_OP = 0x08;
        private const int BYTEPREFIX = 0x0A;
        private const int WORDPREFIX = 0x0B;
        private const int DWORDPREFIX = 0x0C;
        private const int STRINGPREFIX = 0x0D;
        private const int QWORDPREFIX = 0x0E;
        private const int SCOPE_OP = 0x10;
        private const int BUFFER_OP = 0x11;
        private const int PACKAGE_OP = 0x12;
        private const int VARPACKAGE_OP = 0x13;
        private const int METHOD_OP = 0x14;
        private const int EXTERNAL_OP = 0x15;
        private const int DUAL_PREFIX = 0x2E;
        private const int MULTI_PREFIX = 0x2F;
        private const int EXTOP_PREFIX = 0x5B;
        private const int ROOT_CHAR = 0x5C;
        private const int PARENT_CHAR = 0x5E;
        private const int LOCAL0_OP = 0x60;
        private const int LOCAL1_OP = 0x61;
        private const int LOCAL2_OP = 0x62;
        private const int LOCAL3_OP = 0x63;
        private const int LOCAL4_OP = 0x64;
        private const int LOCAL5_OP = 0x65;
        private const int LOCAL6_OP = 0x66;
        private const int LOCAL7_OP = 0x67;
        private const int ARG0_OP = 0x68;
        private const int ARG1_OP = 0x69;
        private const int ARG2_OP = 0x6A;
        private const int ARG3_OP = 0x6B;
        private const int ARG4_OP = 0x6C;
        private const int ARG5_OP = 0x6D;
        private const int ARG6_OP = 0x6E;
        private const int STORE_OP = 0x70;
        private const int REFOF_OP = 0x71;
        private const int ADD_OP = 0x72;
        private const int CONCAT_OP = 0x73;
        private const int SUBTRACT_OP = 0x74;
        private const int INCREMENT_OP = 0x75;
        private const int DECREMENT_OP = 0x76;
        private const int MULTIPLY_OP = 0x77;
        private const int DIVIDE_OP = 0x78;
        private const int SHL_OP = 0x79;
        private const int SHR_OP = 0x7A;
        private const int AND_OP = 0x7B;
        private const int OR_OP = 0x7D;
        private const int XOR_OP = 0x7F;
        private const int NAND_OP = 0x7C;
        private const int NOR_OP = 0x7E;
        private const int NOT_OP = 0x80;
        private const int FINDSETLEFTBIT_OP = 0x81;
        private const int FINDSETRIGHTBIT_OP = 0x82;
        private const int DEREF_OP = 0x83;
        private const int CONCATRES_OP = 0x84;
        private const int MOD_OP = 0x85;
        private const int NOTIFY_OP = 0x86;
        private const int SIZEOF_OP = 0x87;
        private const int INDEX_OP = 0x88;
        private const int MATCH_OP = 0x89;
        private const int DWORDFIELD_OP = 0x8A;
        private const int WORDFIELD_OP = 0x8B;
        private const int BYTEFIELD_OP = 0x8C;
        private const int BITFIELD_OP = 0x8D;
        private const int OBJECTTYPE_OP = 0x8E;
        private const int QWORDFIELD_OP = 0x8F;
        private const int LAND_OP = 0x90;
        private const int LOR_OP = 0x91;
        private const int LNOT_OP = 0x92;
        private const int LEQUAL_OP = 0x93;
        private const int LGREATER_OP = 0x94;
        private const int LLESS_OP = 0x95;
        private const int TOBUFFER_OP = 0x96;
        private const int TODECIMALSTRING_OP = 0x97;
        private const int TOHEXSTRING_OP = 0x98;
        private const int TOINTEGER_OP = 0x99;
        private const int TOSTRING_OP = 0x9C;
        private const int MID_OP = 0x9E;
        private const int COPYOBJECT_OP = 0x9D;
        private const int CONTINUE_OP = 0x9F;
        private const int IF_OP = 0xA0;
        private const int ELSE_OP = 0xA1;
        private const int WHILE_OP = 0xA2;
        private const int NOP_OP = 0xA3;
        private const int RETURN_OP = 0xA4;
        private const int BREAK_OP = 0xA5;
        private const int BREAKPOINT_OP = 0xCC;
        private const int ONES_OP = 0xFF;

        // Extended=opcodes
        private const int MUTEX = 0x01;
        private const int EVENT = 0x02;
        private const int CONDREF_OP = 0x12;
        private const int ARBFIELD_OP = 0x13;
        private const int STALL_OP = 0x21;
        private const int SLEEP_OP = 0x22;
        private const int ACQUIRE_OP = 0x23;
        private const int RELEASE_OP = 0x27;
        private const int SIGNAL_OP = 0x24;
        private const int WAIT_OP = 0x25;
        private const int RESET_OP = 0x26;
        private const int FROM_BCD_OP = 0x28;
        private const int TO_BCD_OP = 0x29;
        private const int REVISION_OP = 0x30;
        private const int DEBUG_OP = 0x31;
        private const int FATAL_OP = 0x32;
        private const int TIMER_OP = 0x33;
        private const int OPREGION = 0x80;
        private const int FIELD = 0x81;
        private const int DEVICE = 0x82;
        private const int PROCESSOR = 0x83;
        private const int POWER_RES = 0x84;
        private const int THERMALZONE = 0x85;
        private const int INDEXFIELD = 0x86; // ACPI spec v5.0 section 19.5.60
        private const int BANKFIELD = 0x87;

        // Field=Access=Type
        private const int FIELD_ANY_ACCESS = 0x00;
        private const int FIELD_BYTE_ACCESS = 0x01;
        private const int FIELD_WORD_ACCESS = 0x02;
        private const int FIELD_DWORD_ACCESS = 0x03;
        private const int FIELD_QWORD_ACCESS = 0x04;
        private const int FIELD_LOCK = 0x10;
        private const int FIELD_PRESERVE = 0x00;
        private const int FIELD_WRITE_ONES = 0x01;
        private const int FIELD_WRITE_ZEROES = 0x02;

        // Methods
        private const int METHOD_ARGC_MASK = 0x07;
        private const int METHOD_SERIALIZED = 0x08;

        // Match Comparison Type
        private const int MATCH_MTR = 0x0;
        private const int MATCH_MEQ = 0x1;
        private const int MATCH_MLE = 0x2;
        private const int MATCH_MLT = 0x3;
        private const int MATCH_MGE = 0x4;
        private const int MATCH_MGT = 0x5;
        #endregion

        List<lai_nsnode> ns_array = new List<lai_nsnode>();
        #region Parse Flags
        private const int LAI_REFERENCE_MODE = 5;
        private const int LAI_OPTIONAL_REFERENCE_MODE = 6;
        private const int LAI_IMMEDIATE_BYTE_MODE = 7;
        private const int LAI_IMMEDIATE_WORD_MODE = 8;
        private const int LAI_IMMEDIATE_DWORD_MODE = 9;
        private static int ReadFlags(int parse_mode)
        {
            //cosmos-specific function

            if (parse_mode == LAI_IMMEDIATE_BYTE_MODE)
            {
                return LAI_MF_RESULT;
            }
            else if (parse_mode == LAI_IMMEDIATE_WORD_MODE)
            {
                return LAI_MF_RESULT;
            }
            else if (parse_mode == LAI_IMMEDIATE_DWORD_MODE)
            {
                return LAI_MF_RESULT;
            }
            else if (parse_mode == LAI_EXEC_MODE)
            {
                return LAI_MF_RESOLVE | LAI_MF_INVOKE;
            }
            else if (parse_mode == LAI_UNRESOLVED_MODE)
            {
                return LAI_MF_RESULT;
            }
            else if (parse_mode == LAI_DATA_MODE)
            {
                return LAI_MF_RESULT;
            }
            else if (parse_mode == LAI_OBJECT_MODE)
            {
                return LAI_MF_RESULT | LAI_MF_RESOLVE | LAI_MF_INVOKE;
            }
            else if (parse_mode == LAI_REFERENCE_MODE)
            {
                return LAI_MF_RESULT | LAI_MF_RESOLVE;
            }
            else if (parse_mode == LAI_OPTIONAL_REFERENCE_MODE)
            {
                return LAI_MF_RESULT | LAI_MF_RESOLVE | LAI_MF_NULLABLE;
            }
            else
            {
                lai_panic("ReadFlags() unsupported support " + parse_mode);
            }
            return 0;
        }
        #endregion

        private static void lai_create_root()
        {
            var instance = lai_current_instance();

            instance.RootNode = new lai_nsnode();
            instance.RootNode.type = LAI_NAMESPACE_ROOT;
            instance.RootNode.name = "\\___";
            instance.RootNode.parent = null;

            // Create the predefined objects.
            var sb_node = new lai_nsnode();
            sb_node.type = LAI_NAMESPACE_DEVICE;
            sb_node.name = "_SB_";
            sb_node.parent = instance.RootNode;
            lai_install_nsnode(sb_node);

            var si_node = new lai_nsnode();
            si_node.type = LAI_NAMESPACE_DEVICE;
            si_node.name = "_SI_";
            si_node.parent = instance.RootNode;
            lai_install_nsnode(si_node);

            var gpe_node = new lai_nsnode();
            gpe_node.type = LAI_NAMESPACE_DEVICE;
            gpe_node.name = "_GPE";
            gpe_node.parent = instance.RootNode;
            lai_install_nsnode(gpe_node);

            var pr_node = new lai_nsnode();
            pr_node.type = LAI_NAMESPACE_DEVICE;
            pr_node.name = "_PR_";
            pr_node.parent = instance.RootNode;
            lai_install_nsnode(pr_node);

            var tz_node = new lai_nsnode();
            tz_node.type = LAI_NAMESPACE_DEVICE;
            tz_node.name = "_TZ_";
            tz_node.parent = instance.RootNode;
            lai_install_nsnode(tz_node);

            // Create the OS-defined objects.
            var osi_node = new lai_nsnode();
            osi_node.type = LAI_NAMESPACE_METHOD;
            osi_node.name = "_OSI";
            osi_node.parent = instance.RootNode;
            osi_node.method_flags = 0x1;
            osi_node.method_override = DoOSIMethod;
            lai_install_nsnode(osi_node);

            var os_node = new lai_nsnode();
            os_node.type = LAI_NAMESPACE_METHOD;
            os_node.name = "_OS_";
            os_node.parent = instance.RootNode;
            os_node.method_flags = 0x0;
            os_node.method_override = DoOSMethod;
            lai_install_nsnode(os_node);

            var rev_node = new lai_nsnode();
            rev_node.type = LAI_NAMESPACE_METHOD;
            rev_node.name = "_REV";
            rev_node.parent = instance.RootNode;
            rev_node.method_flags = 0x0;
            rev_node.method_override = DoRevMethod;
            lai_install_nsnode(rev_node);
        }
        private static int DoRevMethod(lai_variable args, ref lai_variable result)
        {
            lai_panic("No implemenation: DoRevMethod");
            return -1;
        }
        private static int DoOSMethod(lai_variable args, ref lai_variable result)
        {
            lai_panic("No implemenation: DoOSMethod");
            return -1;
        }
        private static int DoOSIMethod(lai_variable args, ref lai_variable result)
        {
            lai_panic("No implemenation: DoOSIMethod");
            return -1;
        }
        /// <summary>
        /// Creates the ACPI namespace. Requires the ability to scan for ACPI tables
        /// </summary>
        public static void lai_create_namespace()
        {
            lai_create_root();
            var instance = lai_current_instance();

            // Create the namespace with all the objects.
            var state = new lai_state();
            state.fadt = (FADTPtr*)Cosmoss.Core.ACPI.GetTable("FACP");

            if (state.fadt == null)
            {
                lai_panic("unable to find ACPI FACP");
            }

            var dsdt = (uint*)state.fadt->Dsdt;
            if (dsdt == null)
            {
                lai_panic("unable to find ACPI DSDT");
            }

            // Load the DSDT.
            var dsdt_table = lai_load_table(dsdt, 0);
            lai_populate(instance.RootNode, dsdt_table, state);
            lai_finalize_state(state);

            byte* ssdt = Cosmoss.Core.ACPI.GetTable("SSDT");
            if (ssdt != null)
            {
                var ssdt_table = lai_load_table(ssdt, 0);
                state = new();

                lai_populate(instance.RootNode, ssdt_table, state);
                lai_finalize_state(state);
            }

            // The PSDT is treated the same way as the SSDT.
            // Scan for PSDTs too for compatibility with some ACPI 1.0 PCs.
            byte* psdt = Cosmoss.Core.ACPI.GetTable("PSDT");
            if (psdt != null)
            {
                var psdt_table = lai_load_table(psdt, 0);
                state = new();

                lai_populate(instance.RootNode, psdt_table, state);
                lai_finalize_state(state);
            }

            lai_log("lai_create_namespace finished");
        }
        private static void lai_finalize_state(lai_state state)
        {
            while ((state.ctxstack_base.Count - 1) >= 0)
                lai_exec_pop_ctxstack_back(ref state);
            while ((state.blkstack_base.Count - 1) >= 0)
                lai_exec_pop_blkstack_back(ref state);
            while ((state.stack_base.Count - 1) >= 0)
                lai_exec_pop_stack_back(ref state);
            lai_exec_pop_opstack(ref state, (state.opstack_base.Count));
        }
        private static void lai_populate(lai_nsnode parent, lai_aml_segment amls, lai_state state)
        {
            var size = amls.table->header.Length - 36;
            var dsdtBytes = ((byte*)amls.table) + 36;
            lai_ctxitem populate_ctxitem = lai_exec_push_ctxstack(ref state, amls, dsdtBytes, parent, null); ;

            var blkitem = lai_exec_push_blkstack(ref state, 0, (int)size);
            //Kernel.PrintDebug(Convert.ToBase64String(dsdtBytes));
            lai_exec_push_stack(ref state, LAI_POPULATE_STACKITEM);

            int status = lai_exec_run(state);
            if (status != 0)
            {
                lai_log("lai_exec_run() failed in lai_populate()");
            }

            if (state.ctxstack_base.Count > 0)
            {
                lai_panic("ctxstack is not empty after lai_exec_run completeted");
            }

            if (state.stack_base.Count > 0)
            {
                lai_panic("stack_base is not empty after lai_exec_run completeted");
            }

            if (state.opstack_base.Count > 0)
            {
                lai_panic("opstack_base is not empty after lai_exec_run completeted");
            }
        }

        private static void lai_exec_pop_blkstack_back(ref lai_state state)
        {
            state.blkstack_base.RemoveAt(state.blkstack_base.Count - 1);
        }
        private static void lai_exec_pop_ctxstack_back(ref lai_state state)
        {
            state.ctxstack_base.RemoveAt(state.ctxstack_base.Count - 1);
        }
        private static void lai_exec_pop_stack_back(ref lai_state state)
        {
            // Removes the last item from the stack.
            state.stack_base.RemoveAt(state.stack_base.Count - 1);
        }
        private static void lai_exec_pop_opstack(ref lai_state state, int n)
        {
            state.opstack_base.RemoveRange(state.opstack_base.Count - n, n);
        }
        private static void lai_exec_pop_opstack_back(ref lai_state state)
        {
            state.opstack_base.RemoveAt(state.opstack_base.Count - 1);
        }
        private static int lai_exec_process(lai_state state)
        {
            lai_stackitem item = lai_exec_peek_stack_back(state);
            lai_ctxitem ctxitem = lai_exec_peek_ctxstack_back(state);
            lai_blkitem block = lai_exec_peek_blkstack_back(state);
            lai_aml_segment amls = ctxitem.amls;
            byte* method = ctxitem.code;

            lai_nsnode ctx_handle = ctxitem.handle;
            lai_invocation invocation = ctxitem.invocation;

            // Package-size encoding (and similar) needs to know the PC of the opcode.
            // If an opcode sequence contains a pkgsize, the sequence generally ends at:
            //     opcode_pc + pkgsize + opcode size.
            int opcode_pc = block.pc;
            int limit = block.limit;

            // PC relative to the start of the table.
            // This matches the offsets in the output of 'iasl -l'.
            var table_pc = sizeof(AcpiHeader) + opcode_pc;
            var table_limit_pc = sizeof(AcpiHeader) + block.limit;

            if (block.pc > block.limit)
            {
                lai_log("stack dump before crash:");
                foreach (var b in state.stack_base)
                {
                    lai_log($"k: {b.kind}, {b.op_opcode}");
                }
                lai_panic($"execution escaped out of code range. pc: " + block.pc + ", limit: " + block.limit);
            }
            //lai_log("lai_exec_process: pc: " + block.pc + ",limit=" + block.limit);


            if (item.kind == LAI_POPULATE_STACKITEM)
            {
                if (block.pc == block.limit)
                {
                    lai_exec_pop_blkstack_back(ref state);
                    lai_exec_pop_ctxstack_back(ref state);
                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_EXEC_MODE, ref state);
                }
            }
            else if (item.kind == LAI_METHOD_STACKITEM)
            {
                if (block.pc == block.limit)
                {
                    if (state.opstack_base.Count != 0)
                    {
                        lai_panic("opstack is not empty before return");
                    }

                    if (item.mth_want_result != 0)
                    {
                        lai_operand result = lai_exec_push_opstack(ref state);
                        result.tag = LAI_OPERAND_OBJECT;
                        result.objectt.type = LAI_INTEGER;
                        result.objectt.integer = 0;
                    }

                    // Clean up all per-method namespace nodes.
                    // TODO

                    lai_exec_pop_blkstack_back(ref state);
                    lai_exec_pop_ctxstack_back(ref state);
                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_EXEC_MODE, ref state);
                }
            }
            else if (item.kind == LAI_BUFFER_STACKITEM)
            {
                int k = (state.opstack_base.Count) - item.opstack_frame;

                LAI_ENSURE(k <= 1, "lai_exec_process: LAI_BUFFER_STACKITEM: k<=1. opstackptr: " + (state.opstack_base.Count) + ", frame: " + item.opstack_frame);
                if (k == 1)
                {
                    lai_log("buffer op: found object in opstack");
                    var size = new lai_variable();
                    lai_operand operand = lai_exec_get_opstack(state, item.opstack_frame)[0];
                    size = operand.objectt;
                    lai_exec_pop_opstack_back(ref state);

                    var result = new lai_variable();
                    int initial_size = block.limit - block.pc;
                    if (initial_size < 0)
                        lai_panic("buffer initializer has negative size");
                    byte[] buffer = new byte[initial_size];
                    for (int i = 0; i < initial_size; i++)
                    {
                        var b = method[block.pc + i];
                        buffer[i] = b;
                    }
                    result.buffer = buffer;
                    if (item.buf_want_result != 0)
                    {
                        lai_operand opstack_res = lai_exec_push_opstack(ref state);
                        opstack_res.tag = LAI_OPERAND_OBJECT;
                        opstack_res.objectt.buffer = buffer;
                        state.opstack_base[state.opstack_base.Count - 1] = opstack_res;
                    }

                    lai_exec_pop_blkstack_back(ref state);
                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_OBJECT_MODE, ref state);
                }
            }
            else if (item.kind == LAI_PACKAGE_STACKITEM || item.kind == LAI_VARPACKAGE_STACKITEM)
            {
                var frame = lai_exec_get_opstack(state, item.opstack_frame);
                if (item.pkg_phase == 0)
                {
                    int error;
                    if (item.kind == LAI_PACKAGE_STACKITEM)
                    {
                        error = lai_exec_parse(LAI_IMMEDIATE_BYTE_MODE, ref state);
                    }
                    else
                    {
                        error = lai_exec_parse(LAI_OBJECT_MODE, ref state);
                    }
                    item.pkg_phase++;
                    state.stack_base[state.stack_base.Count - 1] = item;
                    return error;
                }
                else if (item.pkg_phase == 1)
                {
                    lai_variable size = new lai_variable();
                    lai_exec_get_integer(state, frame[1], ref size);

                    lai_exec_pop_opstack_back(ref state);

                    lai_create_pkg(ref frame[0].objectt, size.integer);

                    item.pkg_phase++;
                    state.stack_base[state.stack_base.Count - 1] = item;
                    return 0;
                }

                if ((state.opstack_base.Count) == item.opstack_frame + 2)
                {
                    lai_operand package = frame[0];
                    LAI_ENSURE(package.tag == LAI_OPERAND_OBJECT, "frame[0] must be an object operand");
                    lai_operand initializer = frame[1];
                    LAI_ENSURE(initializer.tag == LAI_OPERAND_OBJECT, "frame[1] must be an object operand");

                    if (item.pkg_index == lai_exec_pkg_size(package.objectt))
                    {
                        lai_panic("package initializer overflows its size");
                    }
                    //pkg->pkg_ptr[i] = 
                    //package.objectt.pk

                    // lai_log("todo write stuff");

                    item.pkg_phase++;
                    state.stack_base[state.stack_base.Count - 1] = item;
                    lai_exec_pop_opstack_back(ref state);
                }
                LAI_ENSURE((state.opstack_base.Count) == item.opstack_frame + 1, "state->opstack_ptr == item->opstack_frame + 1");
                if (block.pc == block.limit)
                {
                    if (item.pkg_want_result == 0)
                    {
                        lai_exec_pop_opstack_back(ref state);
                    }

                    lai_exec_pop_blkstack_back(ref state);
                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_DATA_MODE, ref state);
                }
            }
            else if (item.kind == LAI_NODE_STACKITEM)
            {
                int k = (state.opstack_base.Count) - item.opstack_frame;
                if (item.node_arg_modes[k] == 0)
                {
                    lai_operand[] operands = lai_exec_get_opstack(state, item.opstack_frame);
                    lai_exec_reduce_node(item.node_opcode, state, operands, ctx_handle);
                    lai_exec_pop_opstack(ref state, k);

                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(item.node_arg_modes[k], ref state);
                }
            }
            else if (item.kind == LAI_OP_STACKITEM)
            {
                int k = state.opstack_base.Count - item.opstack_frame;
                if (item.op_arg_modes[k] == 0)
                {
                    lai_variable result = new lai_variable();
                    var operands = lai_exec_get_opstack(state, item.opstack_frame);
                    var error = lai_exec_reduce_op(item.op_opcode, state, operands, ref result);
                    if (error != 0)
                    {
                        return error;
                    }

                    lai_exec_pop_opstack(ref state, k);
                    if (item.op_want_result != 0)
                    {
                        var opstack_res = lai_exec_push_opstack(ref state);
                        opstack_res.tag = LAI_OPERAND_OBJECT;
                        opstack_res.objectt = result;
                    }
                    else
                    {
                        lai_var_finalize(result);
                    }

                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(item.op_arg_modes[k], ref state);
                }
            }
            else if (item.kind == LAI_INVOKE_STACKITEM)
            {
                lai_panic("LAI_INVOKE_STACKITEM: not implemented");
                return 1;
            }
            else if (item.kind == LAI_RETURN_STACKITEM)
            {
                int k = state.opstack_base.Count - item.opstack_frame;
                LAI_ENSURE(k <= 1, "k <= 1");

                if (k == 1)
                {
                    lai_variable result = new();
                    lai_operand[] operand = lai_exec_get_opstack(state, item.opstack_frame);
                    lai_exec_get_objectref(state, operand, ref result);
                    lai_exec_pop_opstack_back(ref state);

                    // Find the last LAI_METHOD_STACKITEM on the stack.
                    int m = 0;
                    lai_stackitem method_item;

                    while (true)
                    {
                        method_item = lai_exec_peek_stack(state, 1 + m);

                        if (method_item == null)
                        {
                            lai_panic("Return() outside of control method()");
                        }

                        if (method_item.kind == LAI_METHOD_STACKITEM)
                            break;
                        if (method_item.kind == LAI_COND_STACKITEM && method_item.kind != LAI_LOOP_STACKITEM)
                        {
                            lai_panic("Return() cannot skip item of type " + method_item.kind);
                        }

                        m++;
                    }

                    // Push the return value.
                    if (method_item.mth_want_result != 0)
                    {
                        lai_operand opstack_res = lai_exec_push_opstack(ref state);
                        opstack_res.tag = LAI_OPERAND_OBJECT;
                        opstack_res.objectt = result; //TODO: clone it
                    }

                    // Clean up all per-method namespace nodes.
                    // TODO

                    // Pop the LAI_RETURN_STACKITEM.
                    lai_exec_pop_stack_back(ref state);

                    // Pop all nested loops/conditions.
                    for (int i = 0; i < m; i++)
                    {
                        lai_stackitem pop_item = lai_exec_peek_stack_back(state);
                        LAI_ENSURE(pop_item.kind == LAI_COND_STACKITEM
                                   || pop_item.kind == LAI_LOOP_STACKITEM, "ensure case 23");
                        lai_exec_pop_blkstack_back(ref state);
                        lai_exec_pop_stack_back(ref state);
                    }

                    // Pop the LAI_METHOD_STACKITEM.
                    lai_exec_pop_ctxstack_back(ref state);
                    lai_exec_pop_blkstack_back(ref state);
                    lai_exec_pop_stack_back(ref state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_OBJECT_MODE, ref state);
                }
            }
            else if (item.kind == LAI_LOOP_STACKITEM)
            {
                lai_panic("LAI_LOOP_STACKITEM: not implemtented");
                return 1;
            }
            else if (item.kind == LAI_COND_STACKITEM)
            {
                if (item.cond_state == 0)
                {
                    int k = state.opstack_base.Count - item.opstack_frame;
                    // We are at the beginning of the condition and need to check the predicate.
                    LAI_ENSURE(k <= 1, "LAI_COND_STACKITEM k <= 1");
                    if (k == 1)
                    {
                        lai_variable predicate = new lai_variable();
                        lai_operand[] operand = lai_exec_get_opstack(state, item.opstack_frame);
                        lai_exec_get_integer(state, operand[0], ref predicate);
                        lai_exec_pop_opstack_back(ref state);

                        if (predicate.integer != 0)
                        {
                            item.cond_state = LAI_COND_BRANCH;
                        }
                        else
                        {
                            if (item.cond_has_else != 0)
                            {
                                item.cond_state = LAI_COND_BRANCH;
                                block.pc = item.cond_else_pc;
                                block.limit = item.cond_else_limit;
                            }
                            else
                            {
                                Console.WriteLine("if statement end 2");
                                lai_exec_pop_blkstack_back(ref state);
                                lai_exec_pop_stack_back(ref state);
                            }
                        }
                        return 0;
                    }
                    else
                    {
                        return lai_exec_parse(LAI_OBJECT_MODE, ref state);
                    }
                }
                else
                {
                    LAI_ENSURE(item.cond_state == LAI_COND_BRANCH, "item.cond_state == LAI_COND_BRANCH");
                    if (block.pc == block.limit)
                    {
                        Console.WriteLine("if statement end");
                        lai_exec_pop_blkstack_back(ref state);
                        lai_exec_pop_stack_back(ref state);
                        return 0;
                    }
                    else
                    {
                        return lai_exec_parse(LAI_EXEC_MODE, ref state);
                    }
                }
            }
            else if (item.kind == LAI_BANKFIELD_STACKITEM)
            {
                lai_panic("LAI_BANKFIELD_STACKITEM: not implemtented");
                return 1;
            }
            else
            {
                lai_log("lai_exec_process: " + item.kind + " not implemented");
                return 1;
            }
        }

        private static void lai_exec_get_objectref(lai_state state, lai_operand[] src, ref lai_variable objectt)
        {
            LAI_ENSURE(src[0].tag == LAI_OPERAND_OBJECT, "src[0].tag == LAI_OPERAND_OBJECT");
            lai_var_assign(ref objectt, src[0].objectt);
        }

        private static void lai_var_assign(ref lai_variable dest, lai_variable src)
        {
            dest = src;
        }

        private static void lai_var_finalize(lai_variable result)
        {
            // TODO
        }

        private static int lai_exec_pkg_size(lai_variable objectt)
        {
            if (objectt.pkg_items == null)
                throw new Exception("lai_exec_pkg_size: objectt.pkg_items is null");
            return objectt.pkg_items.Length;
        }
        private static void lai_create_pkg(ref lai_variable objectt, ulong n)
        {
            objectt.type = LAI_PACKAGE;
            objectt.pkg_rc = 1;
            objectt.pkg_items = new lai_variable[n];
        }
        private static void lai_exec_reduce_node(int opcode, lai_state state, lai_operand[] operands, lai_nsnode ctx_handle)
        {
            switch (opcode)
            {
                case NAME_OP:
                    {
                        lai_variable objectt = new lai_variable();
                        // ..lai_exec_get_objectref(state, &operands[1], objectt);
                        objectt = operands[0].objectt;

                        if (operands[0].tag != LAI_UNRESOLVED_NAME)
                        {
                            lai_panic("assertion failure: tag must be LAI_UNRESOLVED_NAME in lai_exec_reduce_node");
                        }
                        lai_amlname amln = new lai_amlname();
                        lai_amlname_parse(ref amln, operands[0].cosmos_aml, operands[0].unres_aml);

                        lai_nsnode node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_NAME;
                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        node.objectt = objectt;

                        lai_install_nsnode(node);
                        var item = lai_exec_peek_ctxstack_back(state);
                        if (item.invocation != null)
                        {
                            //todo
                            // item.invocation.per_method_list.Add(node.per_method_item);
                        }
                        break;
                    }
                case (EXTOP_PREFIX << 8) | OPREGION:
                    {
                        lai_variable basee = new lai_variable();
                        lai_variable size = new lai_variable();
                        lai_exec_get_integer(state, operands[2], ref basee);
                        lai_exec_get_integer(state, operands[3], ref size);

                        LAI_ENSURE(operands[0].tag == LAI_UNRESOLVED_NAME, "OpRegion Assert 1");
                        LAI_ENSURE(operands[1].tag == LAI_OPERAND_OBJECT
                                   && operands[1].objectt.type == LAI_INTEGER, "OpRegion Assert 2");

                        lai_amlname amln = new lai_amlname();
                        lai_amlname_parse(ref amln, operands[0].cosmos_aml, operands[0].unres_aml);

                        lai_nsnode node = new lai_nsnode();
                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        node.type = LAI_NAMESPACE_OPREGION;
                        node.op_address_space = operands[1].objectt.integer;
                        node.op_base = basee.integer;
                        node.op_length = size.integer;
                        lai_log("OpRegion node: " + node.name);
                        lai_install_nsnode(node);
                        break;
                    }
                default:
                    lai_panic("undefined opcode in lai_exec_reduce_node: " + opcode);
                    break;
            }
        }
        private static void lai_exec_get_integer(lai_state state, lai_operand src, ref lai_variable obj)
        {
            LAI_ENSURE(src.tag == LAI_OPERAND_OBJECT, "src.tag == LAI_OPERAND_OBJECT");
            obj = src.objectt;
        }
        private static void lai_do_resolve_new_node(ref lai_nsnode node, lai_nsnode ctx_handle, ref lai_amlname in_amln)
        {
            var amln = in_amln;

            lai_nsnode parent = ctx_handle;
            // ctx_handle needs to be resolved.

            if (parent.type == LAI_NAMESPACE_ALIAS)
            {
                lai_panic("lai_do_resolve_new_node: parent type cannot be LAI_NAMESPACE_ALIAS");
            }
            // Note: we do not care about amln->search_scopes here.
            //       As we are creating a new name, the code below already does the correct thing.
            if (amln.is_absolute)
            {
                while (parent.parent != null)
                {
                    parent = parent.parent;

                }
                if (parent.type != LAI_NAMESPACE_ROOT)
                {
                    lai_panic("lai_do_resolve_new_node: parent type cannot be LAI_NAMESPACE_ROOT");
                }


            }

            for (int i = 0; i < amln.height; i++)
            {
                if (parent.parent == null)
                {
                    //  LAI_ENSURE(parent->type == LAI_NAMESPACE_ROOT);
                    if (parent.type != LAI_NAMESPACE_ROOT)
                    {
                        lai_panic("lai_do_resolve_new_node: parent type cannot be LAI_NAMESPACE_ROOT (x2)");
                    }
                    break;
                }

                parent = parent.parent;
            }

            // Otherwise the new object has an empty name.
            if ((int)amln.end == (int)amln.it)
            {
                lai_panic("string reading cannot be over");
            }

            while (true)
            {
                string segment = "";
                for (int i = 0; i < 4; i++)
                    segment += (char)amln.it[i];
                amln.it += 4;

                if (amln.it == amln.end)
                {
                    node.name = segment;
                    node.parent = parent;
                    break;
                }
                else
                {
                    parent = lai_ns_get_child(parent, segment);
                    if (parent == null)
                    {
                        lai_panic("lai_ns_get_child() cannot retun null. segment is " + segment + ", parent name is ");
                    }
                    if (parent.type == LAI_NAMESPACE_ALIAS)
                    {
                        lai_log("resolution of new object name traverses Alias() -  this is not supported in ACPICA");
                        parent = parent.al_target;
                        if (parent.type == LAI_NAMESPACE_ALIAS)
                        {
                            lai_panic("parent type cannot be LAI_NAMESPACE_ALIAS (x3)");
                        }
                    }
                }
            }
        }
        private static lai_nsnode lai_ns_get_child(lai_nsnode parent, string name)
        {
            foreach (var item in parent.children)
            {
                if (item.name == name)
                {
                    return item;
                }
            }
            return null;
        }
        private static lai_operand[] lai_exec_get_opstack(lai_state state, int n)
        {
            if (n < (state.opstack_base.Count))
            {

            }
            else
            {
                lai_panic("fatal error: n < state.opstack_ptr must be true in lai_exec_get_opstack");
            }
            List<lai_operand> ops = new List<lai_operand>();
            for (int i = n; i < state.opstack_base.Count; i++)
            {
                ops.Add(state.opstack_base[i]);
            }
            return ops.ToArray();
        }
        private static bool lai_is_name(byte character)
        {
            if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z')
      || character == '_' || character == ROOT_CHAR || character == PARENT_CHAR
      || character == MULTI_PREFIX || character == DUAL_PREFIX)
                return true;

            else
                return false;
        }
        private static int lai_exec_parse(int parse_mode, ref lai_state state)
        {

            lai_ctxitem ctxitem = lai_exec_peek_ctxstack_back(state);
            lai_blkitem block = lai_exec_peek_blkstack_back(state);
            lai_aml_segment amls = ctxitem.amls;
            byte* method = ctxitem.code;

            lai_nsnode ctx_handle = ctxitem.handle;
            lai_invocation invocation = ctxitem.invocation;

            int pc = block.pc;
            int limit = block.limit;
            //lai_log("\n");
            //lai_log("PARSING NEW OPCODE");

            // Package-size encoding (and similar) needs to know the PC of the opcode.
            // If an opcode sequence contains a pkgsize, the sequence generally ends at:
            //     opcode_pc + pkgsize + opcode size.
            int opcode_pc = pc;

            // PC relative to the start of the table.
            // This matches the offsets in the output of 'iasl -l'.
            var table_pc = sizeof(AcpiHeader) + opcode_pc;
            var table_limit_pc = sizeof(AcpiHeader) + block.limit;

            if (!(pc < block.limit))
            {
                lai_panic($"execution escaped out of code range. pc: " + table_pc + ", limit: " + table_limit_pc);
            }

            // Whether we use the result of an expression or not.
            // If yes, it will be pushed onto the opstack after the expression is computed.
            int want_result = ReadFlags(parse_mode) & LAI_MF_RESULT;//LAI_MF_RESOLVE | LAI_MF_INVOKE;// lai_mode_flags[parse_mode] & LAI_MF_RESULT;

            //todo big if statement

            if (parse_mode == LAI_IMMEDIATE_BYTE_MODE)
            {
                byte value = 0;
                if (lai_parse_u8(ref value, method, ref pc, limit))
                {
                    lai_warn("lai_parse_u8 failed");
                    return 5;
                }
                lai_exec_commit_pc(ref state, pc);

                var result = lai_exec_push_opstack(ref state);
                result.tag = LAI_OPERAND_OBJECT;
                result.objectt.type = LAI_INTEGER;
                result.objectt.integer = value;
                state.opstack_base[state.opstack_base.Count - 1] = result;
                return 0;
            }
            else if (parse_mode == LAI_IMMEDIATE_WORD_MODE)
            {
                lai_warn("parse mode LAI_IMMEDIATE_WORD_MODE not implemented");
                return 5;
            }
            else if (parse_mode == LAI_IMMEDIATE_DWORD_MODE)
            {
                lai_warn("parse mode LAI_IMMEDIATE_DWORD_MODE not implemented");
                return 5;
            }

            // Process names. (todo)
            int oldpc = pc;
            if (lai_is_name(method[pc]))
            {
                lai_amlname amln = new lai_amlname();
                if (lai_parse_name(ref amln, method, ref pc, limit))
                {
                    lai_log("lai_parse_name failed");
                    return 1;
                }

                lai_exec_commit_pc(ref state, pc);
                // string path = lai_stringify_amlname(amln);
                // lai_log("parsing name " + path + " at " + pc);
                if (parse_mode == LAI_DATA_MODE)
                {
                    if (want_result != 0)
                    {
                        var opstack_res = lai_exec_push_opstack(ref state);
                        opstack_res.tag = LAI_OPERAND_OBJECT;
                        opstack_res.objectt.type = LAI_LAZY_HANDLE;
                        opstack_res.objectt.unres_ctx_handle = ctx_handle;
                        opstack_res.objectt.unres_aml_method = method;
                        opstack_res.objectt.unres_aml_pc = opcode_pc;
                        state.opstack_base[state.opstack_base.Count - 1] = opstack_res;
                    }
                }
                else if ((ReadFlags(parse_mode) & LAI_MF_RESOLVE) == 0)
                {
                    if (want_result != 0)
                    {
                        var opstack_res = lai_exec_push_opstack(ref state);
                        opstack_res.tag = LAI_UNRESOLVED_NAME;
                        opstack_res.unres_ctx_handle = ctx_handle;

                        opstack_res.unres_aml = oldpc;
                        opstack_res.cosmos_aml = method;

                        state.opstack_base[state.opstack_base.Count - 1] = opstack_res;
                    }
                }
                else
                {
                    lai_nsnode handle = lai_do_resolve(ctx_handle, ref amln);
                    if (handle == null)
                    {
                        if ((ReadFlags(parse_mode) & LAI_MF_NULLABLE) != 0)
                        {
                            if (want_result != 0)
                            {
                                var opstack_res = lai_exec_push_opstack(ref state);
                                opstack_res.tag = LAI_RESOLVED_NAME;
                                opstack_res.handle = null;
                                state.opstack_base[state.opstack_base.Count - 1] = opstack_res;
                            }
                        }
                        else
                        {
                            lai_warn("undefined reference " + lai_stringify_amlname(amln) + " in object mode, aborting");
                            return 5;
                        }
                    }
                    else if (handle.type == LAI_NAMESPACE_METHOD && (ReadFlags(parse_mode) & LAI_MF_INVOKE) != 0)
                    {
                        lai_stackitem node_item = lai_exec_push_stack(ref state, LAI_INVOKE_STACKITEM);
                        node_item.opstack_frame = (state.opstack_base.Count);
                        node_item.ivk_argc = handle.method_flags & METHOD_ARGC_MASK;
                        node_item.ivk_want_result = (byte)want_result;
                        state.stack_base[state.stack_base.Count - 1] = node_item;

                        lai_operand opstack_method = lai_exec_push_opstack(ref state);
                        opstack_method.tag = LAI_RESOLVED_NAME;
                        opstack_method.handle = handle;
                        state.opstack_base[state.opstack_base.Count - 1] = opstack_method;
                    }
                    else if ((ReadFlags(parse_mode) & LAI_MF_INVOKE) != 0)
                    {
                        // TODO: Get rid of this case again!
                        lai_variable var = new();
                        lai_exec_access(ref var, handle);
                        if (want_result != 0)
                        {
                            var res = lai_exec_push_opstack(ref state);
                            res.tag = LAI_OPERAND_OBJECT;
                            res.objectt = var;
                        }

                    }
                    else
                    {
                        // lai_log("parsing name " + path + " using the else statement");

                        if (want_result != 0)
                        {
                            var opstack_method = lai_exec_push_opstack(ref state);
                            opstack_method.tag = LAI_RESOLVED_NAME;
                            opstack_method.handle = handle;
                            state.opstack_base[state.opstack_base.Count - 1] = opstack_method;
                        }
                    }
                }
                return 0;
            }

            //General opcodes
            int opcode;
            if (method[pc] == EXTOP_PREFIX)
            {
                if (pc + 1 == block.limit)
                {
                    lai_panic("two-byte opcode on method boundary");
                }
                opcode = (EXTOP_PREFIX << 8) | method[pc + 1];
                pc += 2;
            }
            else
            {
                opcode = method[pc];
                pc++;
            }
            //lai_log("parsing opcode " + opcode + "[ at 0x" + table_pc.ToString("X") + "]");

            switch (opcode)
            {
                case ZERO_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);

                        if (parse_mode == LAI_DATA_MODE || parse_mode == LAI_OBJECT_MODE)
                        {
                            var result = lai_exec_push_opstack(ref state);
                            result.tag = LAI_OPERAND_OBJECT;
                            result.objectt.type = LAI_INTEGER;
                            result.objectt.integer = 0;
                            state.opstack_base[state.opstack_base.Count - 1] = result;
                        }
                        else if (parse_mode == LAI_REFERENCE_MODE || parse_mode == LAI_OPTIONAL_REFERENCE_MODE)
                        {
                            // In target mode, ZERO_OP generates a null target and not an integer!
                            var result = lai_exec_push_opstack(ref state);
                            result.tag = LAI_NULL_NAME;
                            state.opstack_base[state.opstack_base.Count - 1] = result;
                        }
                        else
                        {
                            lai_warn("Zero() in execution mode has no effect");
                            LAI_ENSURE(parse_mode == LAI_EXEC_MODE, "parse_mode == LAI_EXEC_MODE");
                        }
                        break;
                    }
                case ONE_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        if (parse_mode == LAI_DATA_MODE || parse_mode == LAI_OBJECT_MODE)
                        {
                            var result = lai_exec_push_opstack(ref state);
                            result.tag = LAI_OPERAND_OBJECT;
                            result.objectt.type = LAI_INTEGER;
                            result.objectt.integer = 1;
                            state.opstack_base[state.opstack_base.Count - 1] = result;
                        }
                        else
                        {
                            lai_warn("One() in execution mode has no effect");
                            LAI_ENSURE(parse_mode == LAI_EXEC_MODE, "parse_mode == LAI_EXEC_MODE");
                        }

                    }
                    break;
                case NOP_OP:
                    lai_exec_commit_pc(ref state, pc);
                    break;
                case BYTEPREFIX:
                case WORDPREFIX:
                case DWORDPREFIX:
                case QWORDPREFIX:
                    {
                        ulong value = 0;
                        switch (opcode)
                        {
                            case BYTEPREFIX:
                                {
                                    byte temp = 0;
                                    if (lai_parse_u8(ref temp, method, ref pc, limit))
                                    {
                                        lai_warn("failed to parse BYTEPREFIX");
                                        return 5;
                                    }
                                    value = temp;
                                    break;
                                }
                            case WORDPREFIX:
                                {
                                    ushort temp = 0;
                                    if (lai_parse_u16(ref temp, method, ref pc, limit))
                                    {
                                        return 5;
                                    }
                                    value = temp;
                                    break;
                                }
                            case DWORDPREFIX:
                                {
                                    uint temp = 0;
                                    if (lai_parse_u32(ref temp, method, ref pc, limit))
                                        return 5;
                                    value = temp;
                                    break;
                                }
                            case QWORDPREFIX:
                                {
                                    if (lai_parse_u64(ref value, method, ref pc, limit))
                                        return 5;
                                    break;
                                }
                            default:
                                lai_warn("Data type not implemnation: " + opcode);
                                break;
                        }
                        lai_exec_commit_pc(ref state, pc);
                        if (parse_mode == LAI_DATA_MODE || parse_mode == LAI_OBJECT_MODE)
                        {
                            lai_operand result = lai_exec_push_opstack(ref state);
                            result.tag = LAI_OPERAND_OBJECT;
                            result.objectt.type = LAI_INTEGER;
                            result.objectt.integer = value;
                            state.opstack_base[state.opstack_base.Count - 1] = result;
                        }
                        else
                        {
                            if (parse_mode != LAI_EXEC_MODE)
                            {
                                lai_panic("In order to read the *prefix opcodes, you must be in EXEC mode");
                            }
                        }
                        break;
                    }
                case NAME_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        lai_stackitem node_item = lai_exec_push_stack(ref state, LAI_NODE_STACKITEM);
                        node_item.node_opcode = opcode;
                        node_item.opstack_frame = (state.opstack_base.Count);

                        byte[] x = new byte[8];
                        x[0] = LAI_UNRESOLVED_MODE;
                        x[1] = LAI_OBJECT_MODE;
                        x[2] = 0;
                        node_item.node_arg_modes = x;

                        state.stack_base[state.stack_base.Count - 1] = node_item;
                        break;
                    }
                case STRINGPREFIX:
                    {
                        int data_pc;
                        int n = 0; // Length of null-terminated string.
                        while (pc + n < block.limit && method[pc + n] != 0)
                        {
                            n++;
                        }

                        if (pc + n == block.limit)
                        {
                            lai_panic("unterminated string in AML code");
                        }

                        data_pc = pc;
                        pc += n + 1;

                        lai_exec_commit_pc(ref state, pc);

                        if (parse_mode == LAI_DATA_MODE || parse_mode == LAI_OBJECT_MODE)
                        {
                            var opstack_res = lai_exec_push_opstack(ref state);
                            opstack_res.tag = LAI_OPERAND_OBJECT;

                            opstack_res.objectt.type = LAI_STRING;

                            string str = "";
                            for (int i = 0; i < n; i++)
                            {
                                str += method[data_pc + i];
                            }
                            Kernel.PrintDebug("stringprefix OP: " + str);
                            opstack_res.objectt.stringval = str;
                        }
                        else
                        {
                            if (parse_mode != LAI_EXEC_MODE)
                            {
                                lai_panic("parse_mode must be exec mode in stringprefix instruction");
                            }
                        }
                        break;
                    }
                case BUFFER_OP:
                    {
                        int data_pc = 0;
                        int encoded_size = 0; // Size of the buffer initializer.
                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit))
                            return 5;
                        data_pc = pc;
                        pc = opcode_pc + 1 + encoded_size;
                        lai_exec_commit_pc(ref state, pc);

                        lai_exec_push_blkstack(ref state, data_pc, opcode_pc + 1 + encoded_size);

                        var buf_item = lai_exec_push_stack(ref state, LAI_BUFFER_STACKITEM);
                        buf_item.opstack_frame = state.opstack_base.Count;
                        buf_item.buf_want_result = (byte)want_result;
                        break;
                    }
                case PACKAGE_OP:
                    {
                        int data_pc;
                        int encoded_size = 0;
                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit))
                            return 5;

                        data_pc = pc;
                        pc = opcode_pc + 1 + encoded_size;
                        lai_exec_commit_pc(ref state, pc);

                        // Note that not all elements of the package need to be initialized.
                        lai_exec_push_blkstack(ref state, data_pc, opcode_pc + 1 + encoded_size);

                        lai_stackitem pkg_item = lai_exec_push_stack(ref state, LAI_PACKAGE_STACKITEM);
                        pkg_item.opstack_frame = (state.opstack_base.Count);
                        pkg_item.pkg_index = 0;
                        pkg_item.pkg_want_result = (byte)want_result;
                        pkg_item.pkg_phase = 0;
                        state.stack_base[state.stack_base.Count - 1] = pkg_item;

                        lai_operand opstack_pkg = lai_exec_push_opstack(ref state);
                        opstack_pkg.tag = LAI_OPERAND_OBJECT;
                        state.opstack_base[state.opstack_base.Count - 1] = opstack_pkg;
                        break;
                    }
                case IF_OP:
                    {
                        int if_pc = 0;
                        int else_pc = 0;
                        int has_else = 0;
                        int if_size = 0;
                        int else_size = 0;

                        if (lai_parse_varint(ref if_size, method, ref pc, limit))
                            return 5;
                        if_pc = pc;
                        pc = opcode_pc + 1 + if_size;
                        if (pc < block.limit && method[pc] == ELSE_OP)
                        {
                            has_else = 1;
                            pc++;
                            if (lai_parse_varint(ref else_size, method, ref pc, limit))
                                return 5;
                            else_pc = pc;
                            pc = opcode_pc + 1 + if_size + 1 + else_size;
                        }

                        lai_exec_commit_pc(ref state, pc);

                        lai_blkitem blkitem = lai_exec_push_blkstack(ref state, if_pc, opcode_pc + 1 + if_size);

                        lai_stackitem cond_item = lai_exec_push_stack(ref state);
                        cond_item.kind = LAI_COND_STACKITEM;
                        cond_item.opstack_frame = state.opstack_base.Count;
                        cond_item.cond_state = 0;
                        cond_item.cond_has_else = has_else;
                        cond_item.cond_else_pc = else_pc;
                        cond_item.cond_else_limit = opcode_pc + 1 + if_size + 1 + else_size;
                        break;
                    }
                case ELSE_OP:
                    lai_panic("Else() outside of If()");
                    break;

                // Scope-like objects in the ACPI namespace.
                case SCOPE_OP:
                    {
                        int nested_pc;
                        var encoded_size = 0;
                        lai_amlname amln = new lai_amlname();
                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit))
                        {
                            lai_warn("FAILURE");
                            return 5;
                        }
                        if (lai_parse_name(ref amln, method, ref pc, limit))
                        {
                            lai_warn("FAILURE");
                            return 5;
                        }
                        nested_pc = pc;
                        pc = opcode_pc + 1 + encoded_size;
                        lai_exec_commit_pc(ref state, pc);

                        lai_nsnode scoped_ctx_handle = lai_do_resolve(ctx_handle, ref amln);
                        if (scoped_ctx_handle == null)
                        {
                            lai_warn("Could not resolve node referenced in Scope");
                            return 7;
                        }

                        lai_exec_push_ctxstack(ref state, amls, method, scoped_ctx_handle, null);

                        lai_exec_push_blkstack(ref state, nested_pc, opcode_pc + 1 + encoded_size);
                        var sz = opcode_pc + 1 + encoded_size;
                        lai_exec_push_stack(ref state, LAI_POPULATE_STACKITEM);
                        break;
                    }

                case METHOD_OP:
                    {
                        int encoded_size = 0;
                        lai_amlname amln = new lai_amlname();
                        byte flags = 0;
                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit)
                            || lai_parse_name(ref amln, method, ref pc, limit)
                            || lai_parse_u8(ref flags, method, ref pc, limit))
                        {
                            lai_warn("Method_OP: failed to parse");
                            return 5;
                        }
                        int nested_pc = pc;
                        pc = opcode_pc + 1 + encoded_size;
                        lai_exec_commit_pc(ref state, pc);
                        lai_nsnode node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_METHOD;
                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        node.method_flags = flags;
                        node.amls = amls;
                        node.pointer = method + nested_pc;
                        node.size = pc - nested_pc;
                        lai_install_nsnode(node);

                        break;
                    }
                case (EXTOP_PREFIX << 8) | OPREGION:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        lai_stackitem node_item = lai_exec_push_stack(ref state, LAI_NODE_STACKITEM);
                        node_item.node_opcode = opcode;
                        node_item.opstack_frame = (state.opstack_base.Count);

                        byte[] b = new byte[8];
                        b[0] = LAI_UNRESOLVED_MODE;
                        b[1] = LAI_IMMEDIATE_BYTE_MODE;
                        b[2] = LAI_OBJECT_MODE;
                        b[3] = LAI_OBJECT_MODE;
                        b[4] = 0;
                        node_item.node_arg_modes = b;

                        state.stack_base[state.stack_base.Count - 1] = node_item;
                        break;
                    }
                case (EXTOP_PREFIX << 8) | FIELD:
                    {
                        lai_log("Parsing field opcode at " + pc);
                        int pkgsize = 0;
                        lai_amlname region_amln = new lai_amlname();
                        if (lai_parse_varint(ref pkgsize, method, ref pc, limit))
                            return 5;

                        if (lai_parse_name(ref region_amln, method, ref pc, limit))
                            return 5;
                        int end_pc = opcode_pc + 2 + pkgsize;
                        lai_nsnode region_node = lai_do_resolve(ctx_handle, ref region_amln);
                        if (region_node == null)
                        {
                            lai_panic("error parsing field for non-existant OpRegion, ignoring...");
                            pc = end_pc;
                            break;
                        }
                        byte access_type = method[pc];
                        pc++;
                        lai_log("Read access type at " + pc + "(" + access_type + ")");

                        // parse FieldList
                        lai_amlname field_amln = new lai_amlname();
                        int curr_off = 0;
                        int skip_bits = 0;
                        while (pc < end_pc)
                        {
                            switch (method[pc])
                            {
                                case 0: // ReservedField
                                    {
                                        lai_log("read reservedfield");
                                        pc++;
                                        // TODO: Partially failing to parse a Field() is a bad idea.
                                        if (lai_parse_varint(ref skip_bits, method, ref pc, limit))
                                            return 5;
                                        curr_off += skip_bits;
                                        break;
                                    }
                                case 1:
                                    {
                                        lai_log("read AccessField");
                                        pc++;
                                        access_type = method[pc];
                                        pc += 2;
                                        break;
                                    }
                                case 2: // TODO: ConnectField
                                    {
                                        lai_panic("ConnectField parsing isn't implemented");
                                        break;
                                    }
                                default: // NamedField
                                    {       // TODO: Partially failing to parse a Field() is a bad idea.
                                        if (lai_parse_name(ref field_amln, method, ref pc, limit)
                                            || lai_parse_varint(ref skip_bits, method, ref pc, limit))
                                            return 5;

                                        lai_nsnode node = new lai_nsnode();
                                        node.type = LAI_NAMESPACE_FIELD;
                                        node.fld_region_node = region_node;
                                        node.fld_flags = access_type;
                                        node.size = skip_bits;
                                        node.offset = curr_off;
                                        lai_do_resolve_new_node(ref node, ctx_handle, ref field_amln);
                                        lai_install_nsnode(node);

                                        //todo invocation
                                        curr_off += skip_bits;
                                        break;
                                    }
                            }
                        }


                        lai_exec_commit_pc(ref state, pc);
                        break;
                    }
                case LOCAL0_OP:
                case LOCAL1_OP:
                case LOCAL2_OP:
                case LOCAL3_OP:
                case LOCAL4_OP:
                case LOCAL5_OP:
                case LOCAL6_OP:
                case LOCAL7_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        if (parse_mode == LAI_REFERENCE_MODE || parse_mode == LAI_OPTIONAL_REFERENCE_MODE)
                        {
                            lai_operand opstack_res = lai_exec_push_opstack(ref state);
                            opstack_res.tag = LAI_LOCAL_NAME;
                            opstack_res.index = opcode - LOCAL0_OP;
                        }
                        else
                        {
                            LAI_ENSURE(parse_mode == LAI_OBJECT_MODE, "parse_mode == LAI_OBJECT_MODE in local* opcodes");
                            lai_operand opstack_res = lai_exec_push_opstack(ref state);
                            opstack_res.tag = LAI_OPERAND_OBJECT;
                            lai_var_assign(ref opstack_res.objectt, invocation.local[opcode - LOCAL0_OP]);
                        }
                        break;
                    }
                case (EXTOP_PREFIX << 8) | DEVICE:
                    {
                        int nested_pc;
                        int encoded_size = 0;
                        lai_amlname amln = new lai_amlname();
                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit))
                            return 5;
                        if (lai_parse_name(ref amln, method, ref pc, limit))
                            return 5;

                        nested_pc = pc;
                        pc = opcode_pc + 2 + encoded_size;
                        lai_exec_commit_pc(ref state, pc);

                        lai_nsnode node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_DEVICE;
                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        lai_install_nsnode(node);

                        //todo invocation

                        lai_exec_push_ctxstack(ref state, amls, method, node, null);

                        int sz = opcode_pc + 2 + encoded_size;
                        lai_exec_push_blkstack(ref state, nested_pc, opcode_pc + 2 + encoded_size);

                        lai_exec_push_stack(ref state, LAI_POPULATE_STACKITEM);
                        break;
                    }
                case (EXTOP_PREFIX << 8) | PROCESSOR:
                    {
                        lai_amlname amln = new lai_amlname();
                        int pkgsize = 0;
                        byte cpu_id = 0;
                        uint pblk_addr = 0;
                        byte pblk_len = 0;

                        if (lai_parse_varint(ref pkgsize, method, ref pc, limit)
                || lai_parse_name(ref amln, method, ref pc, limit)
                || lai_parse_u8(ref cpu_id, method, ref pc, limit)
                || lai_parse_u32(ref pblk_addr, method, ref pc, limit)
                || lai_parse_u8(ref pblk_len, method, ref pc, limit))
                            return 5;
                        int nested_pc = pc;
                        pc = opcode_pc + 2 + pkgsize;
                        lai_exec_commit_pc(ref state, pc);

                        var node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_PROCESSOR;
                        node.cpu_id = cpu_id;
                        node.pblk_addr = pblk_addr;
                        node.pblk_len = pblk_len;

                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        lai_install_nsnode(node);
                        //todo invocation

                        lai_exec_push_ctxstack(ref state, amls, method, node, null);

                        lai_exec_push_blkstack(ref state, nested_pc, opcode_pc + 2 + pkgsize);
                        lai_exec_push_stack(ref state, LAI_POPULATE_STACKITEM);
                        break;
                    }
                case (EXTOP_PREFIX << 8) | MUTEX:
                    {
                        lai_amlname amln = new lai_amlname();
                        if (lai_parse_name(ref amln, method, ref pc, limit))
                            return 5;

                        // skip over trailing 0x02
                        pc++;
                        lai_exec_commit_pc(ref state, pc);

                        lai_nsnode node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_MUTEX;
                        lai_do_resolve_new_node(ref node, ctx_handle, ref amln);
                        lai_install_nsnode(node);

                        //todo invocation
                        break;
                    }
                case ADD_OP:
                case SUBTRACT_OP:
                case MOD_OP:
                case MULTIPLY_OP:
                case AND_OP:
                case OR_OP:
                case XOR_OP:
                case SHR_OP:
                case SHL_OP:
                case NAND_OP:
                case NOR_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);

                        lai_stackitem op_item = lai_exec_push_stack(ref state);
                        op_item.kind = LAI_OP_STACKITEM;
                        op_item.op_opcode = opcode;
                        op_item.opstack_frame = state.opstack_base.Count;
                        op_item.op_arg_modes = new byte[4];
                        op_item.op_arg_modes[0] = LAI_OBJECT_MODE;
                        op_item.op_arg_modes[1] = LAI_OBJECT_MODE;
                        op_item.op_arg_modes[2] = LAI_REFERENCE_MODE;
                        op_item.op_arg_modes[3] = 0;
                        op_item.op_want_result = (byte)want_result;
                        break;
                    }
                case RETURN_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);

                        lai_stackitem node_item = lai_exec_push_stack(ref state);
                        node_item.kind = LAI_RETURN_STACKITEM;
                        node_item.opstack_frame = state.opstack_base.Count;
                        break;
                    }
                case STORE_OP:
                case COPYOBJECT_OP:
                case NOT_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        lai_stackitem node_item = lai_exec_push_stack(ref state);
                        node_item.kind = LAI_OP_STACKITEM;
                        node_item.op_opcode = opcode;
                        node_item.op_arg_modes = new byte[4];
                        node_item.op_arg_modes[0] = LAI_OBJECT_MODE;
                        node_item.op_arg_modes[1] = LAI_REFERENCE_MODE;
                        node_item.op_arg_modes[2] = 0;
                        node_item.op_want_result = (byte)want_result;
                        break;
                    }

                case LAND_OP:
                case LOR_OP:
                case LEQUAL_OP:
                case LLESS_OP:
                case LGREATER_OP:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        lai_stackitem op_item = lai_exec_push_stack(ref state);
                        op_item.kind = LAI_OP_STACKITEM;
                        op_item.op_opcode = opcode;
                        op_item.opstack_frame = state.opstack_base.Count;
                        op_item.op_arg_modes = new byte[4];
                        op_item.op_arg_modes[0] = LAI_OBJECT_MODE;
                        op_item.op_arg_modes[1] = LAI_OBJECT_MODE;
                        op_item.op_arg_modes[2] = LAI_REFERENCE_MODE;
                        op_item.op_arg_modes[3] = 0;
                        op_item.op_want_result = (byte)want_result;
                        break;
                    }
                default:
                    lai_panic("Unknown opcode: 0x" + opcode.ToString("X"));
                    break;


            }
            return 0;
        }

        // lai_exec_access() loads an object from a namespace node.
        private static void lai_exec_access(ref lai_variable var, lai_nsnode src)
        {
            switch (src.type)
            {
                case LAI_NAMESPACE_NAME:
                    // lai_var_assign(var, src.objectt);
                    var = src.objectt;
                    break;
                case LAI_NAMESPACE_FIELD:
                case LAI_NAMESPACE_INDEXFIELD:
                case LAI_NAMESPACE_BANKFIELD:
                    lai_read_opregion(ref var, src);
                    break;
                case LAI_NAMESPACE_BUFFER_FIELD:
                    //lai_read_buffer(var, src);
                    lai_panic("TODO: lai_read_buffer");
                    break;
                case LAI_NAMESPACE_EVENT: /* fall through */
                case LAI_NAMESPACE_MUTEX: /* fall through */
                case LAI_NAMESPACE_OPREGION: /* fall through */
                case LAI_NAMESPACE_THERMALZONE: /* fall through */
                case LAI_NAMESPACE_PROCESSOR: /* fall through */
                case LAI_NAMESPACE_POWERRESOURCE: /* fall through */
                case LAI_NAMESPACE_DEVICE:
                    var.type = LAI_HANDLE;
                    var.handle = src;
                    break;
                default:
                    lai_panic("unexpected type %d of named object in lai_exec_access()" + src.type);
                    break;
            }
        }

        private static bool lai_parse_u8(ref byte output, byte* code, ref int pc, int limit)
        {
            if (pc + 1 > limit)
                return true;

            output = code[pc];
            pc++;
            return false;
        }
        private static bool lai_parse_u16(ref ushort outt, byte* method, ref int pc, int limit)
        {
            if (pc + 2 > limit)
                return true;
            outt = (ushort)((method[pc]) | ((method[pc + 1]) << 8));
            pc += 2;
            return false;
        }
        private static bool lai_parse_u32(ref uint output, byte* code, ref int pc, int limit)
        {
            if (pc + 4 > limit)
                return true;

            output = (code[pc]) | (((uint)code[pc + 1]) << 8) | (((uint)code[pc + 2]) << 16) | (((uint)code[pc + 3]) << 24); ;
            pc += 4;
            return false;
        }
        private static bool lai_parse_u64(ref ulong output, byte* code, ref int pc, int limit)
        {
            if (pc + 8 > limit)
                return true;
            output = ((ulong)code[pc]) | (((ulong)code[pc + 1]) << 8)
                   | (((ulong)code[pc + 2]) << 16) | (((ulong)code[pc + 3]) << 24)
                   | (((ulong)code[pc + 4]) << 32) | (((ulong)code[pc + 5]) << 40)
                   | (((ulong)code[pc + 6]) << 48) | (((ulong)code[pc + 7]) << 56);
            pc += 8;
            return false;
        }
        private static bool lai_parse_varint(ref int outvar, byte* code, ref int pc, int limit)
        {
            if (pc + 1 > limit)
                return true;

            var sz = (code[pc] >> 6) & 3;
            if (sz == 0)
            {
                outvar = (int)(code[pc] & 0x3F);
                pc++;
                return false;
            }
            else if (sz == 1)
            {
                if (pc + 2 > limit)
                    return true;
                outvar = (int)(code[pc] & 0x0F) | (int)(code[pc + 1] << 4);
                pc += 2;
                return false;
            }
            else if (sz == 2)
            {
                if (pc + 3 > limit)
                    return true;
                outvar = ((code[pc] & 0x0F) | (code[pc + 1] << 4)) | (code[pc + 2] << 12);
                pc += 3;
                return false;
            }
            else
            {
                if (sz != 3)
                {
                    lai_panic("lai_parse_varint: SZ must be 3");
                }
                if (pc + 4 > limit)
                    return true;
                outvar = (int)(code[pc] & 0x0F) | (int)(code[pc + 1] << 4)
                       | (int)(code[pc + 2] << 12) | (int)(code[pc + 3] << 20);
                pc += 4;
                return false;
            }
        }
        private static lai_nsnode lai_do_resolve(lai_nsnode ctx_handle, ref lai_amlname in_amln)
        {
            lai_amlname amln = in_amln;
            lai_nsnode current = ctx_handle;
            LAI_ENSURE(current.type != LAI_NAMESPACE_ALIAS, "lai_do_resolve: current->type != LAI_NAMESPACE_ALIAS");

            if (amln.search_scopes)
            {
                string segment = "";
                for (int i = 0; i < 4; i++)
                    segment += (char)amln.it[i];
                amln.it += 4;
                lai_log("Resolving " + segment + " by searching through scopes");
                while (current != null)
                {
                    lai_nsnode node = lai_ns_get_child(current, segment);
                    if (node == null)
                    {
                        current = current.parent;
                        continue;
                    }

                    if (node.type == LAI_NAMESPACE_ALIAS)
                    {
                        node = node.al_target;
                        LAI_ENSURE(node.type != LAI_NAMESPACE_ALIAS, "node->type != LAI_NAMESPACE_ALIAS");
                    }
                    // lai_log("resolution returns " + lai_stringify_amlname(amln));
                    return node;
                }
                return null;
            }
            else
            {
                if (amln.is_absolute)
                {
                    while (current.parent != null)
                        current = current.parent;
                    LAI_ENSURE(current.type == LAI_NAMESPACE_ROOT, "current->type == LAI_NAMESPACE_ROOT x2");
                }

                for (int i = 0; i < amln.height; i++)
                {
                    if (current.parent == null)
                    {
                        LAI_ENSURE(current.type == LAI_NAMESPACE_ROOT, "current->type == LAI_NAMESPACE_ROOT x3");
                        break;
                    }
                    current = current.parent;
                }

                if (amln.it == amln.end)
                {
                    return current;
                }

                while (amln.it != amln.end)
                {
                    string segment = "";
                    for (int i = 0; i < 4; i++)
                        segment += (char)amln.it[i];
                    amln.it += 4;

                    current = lai_ns_get_child(current, segment);
                    if (current == null)
                        return null;
                }

                if (current.type == LAI_NAMESPACE_ALIAS)
                {
                    current = current.al_target;
                    LAI_ENSURE(current.type == LAI_NAMESPACE_ROOT, "current->type == LAI_NAMESPACE_ROOT x4");
                }
                return current;
            }
        }
        private static void LAI_ENSURE(bool condition, string message)
        {
            if (!condition)
            {
                lai_panic("Assertion failed: " + message);
            }
        }
        private static string lai_stringify_amlname(lai_amlname in_amln)
        {
            // Make a copy to avoid rendering the original object unusable.
            var amln = in_amln.Clone();

            var num_segs = (amln.end - amln.it) / 4;
            var max_length = 1 // Leading \ for absolute paths.
                        + amln.height // Leading ^ characters.
                        + num_segs * 5 // Segments, seperated by dots.
                        + 1; // Null-terminator.

            string ret = "";

            if (amln.is_absolute)
            {
                ret += "\\";
            }

            for (int i = 0; i < amln.height; i++)
            {
                ret += "^";
            }

            if (amln.it != amln.end)
            {
                while (true)
                {
                    for (int i = 0; i < 4; i++)
                        ret += (char)amln.it[i];

                    amln.it += 4;

                    if (amln.it == amln.end)
                        break;
                    ret += ".";
                }
            }

            if (ret.Length <= max_length)
            {

            }
            else
            {
                lai_panic("read too much of the string. the string is " + ret);
            }

            return ret;
        }
        private static bool lai_parse_name(ref lai_amlname amln, byte* method, ref int pc, int limit)
        {
            pc += lai_amlname_parse(ref amln, method, pc);
            return false;
        }
        private static int lai_amlname_parse(ref lai_amlname amln, byte* data, int startIndex)
        {
            amln.is_absolute = false;
            amln.height = 0;


            byte* begin = data + startIndex;
            byte* it = begin;

            if (*it == '\\')
            {
                // First character is \ for absolute paths.
                amln.is_absolute = true;
                it++;
            }
            else
            {
                // Non-absolute paths can be prefixed by a number of ^.
                while (*it == '^')
                {
                    amln.height++;
                    it++;
                }
            }

            // Finally, we parse the name's prefix (which determines the number of segments).
            int num_segs;
            if (*it == '\0')
            {
                it++;
                num_segs = 0;
            }
            else if (*it == DUAL_PREFIX)
            {
                it++;
                num_segs = 2;
            }
            else if (*it == MULTI_PREFIX)
            {
                it++;
                num_segs = *it;
                if (!(num_segs > 2))
                {
                    lai_panic("assertion failed: num_segs > 2");
                }
                it++;
            }
            else
            {
                if (!lai_is_name(*it)) { lai_panic("assertion failed: lai_is_name(*it)"); }
                num_segs = 1;
            }

            amln.search_scopes = !amln.is_absolute && amln.height == 0 && num_segs == 1;
            amln.it = it;
            amln.end = it + 4 * num_segs;
            return (int)(amln.end - begin);
        }
        private static void lai_exec_commit_pc(ref lai_state state, int pc)
        {
            //  var block = lai_exec_peek_blkstack_back(state);
            // block.pc = pc;
            //just in case
            state.blkstack_base[state.blkstack_base.Count - 1].pc = pc;
        }
        private static lai_blkitem lai_exec_peek_blkstack_back(lai_state state)
        {
            if ((state.ctxstack_base.Count - 1) < 0)
                return null;

            return state.blkstack_base[state.blkstack_base.Count - 1];
        }
        private static lai_ctxitem lai_exec_peek_ctxstack_back(lai_state state)
        {
            if ((state.ctxstack_base.Count - 1) < 0)
                return null;

            return state.ctxstack_base[state.ctxstack_base.Count - 1];
        }
        /// <summary>
        /// Returns the last item of the stack.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        private static lai_stackitem lai_exec_peek_stack_back(lai_state state)
        {
            return lai_exec_peek_stack(state, 0);
        }
        /// <summary>
        /// Returns the n-th item from the top of the stack.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="vn"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        private static lai_stackitem lai_exec_peek_stack(lai_state state, int n)
        {
            if ((state.stack_base.Count - 1) - n < 0)
            {
                return null;
            }
            return state.stack_base[state.stack_base.Count - 1 - n];
        }
        private static lai_stackitem lai_exec_push_stack(ref lai_state state, int kind = 0)
        {
            state.stack_base.Add(new lai_stackitem() { vaild = 1, kind = kind });

            return state.stack_base[state.stack_base.Count - 1];
        }
        private static lai_blkitem lai_exec_push_blkstack(ref lai_state state, int pc, int limit)
        {
            state.blkstack_base.Add(new lai_blkitem() { pc = pc, limit = limit });

            return state.blkstack_base[state.blkstack_base.Count - 1];
        }
        private static lai_ctxitem lai_exec_push_ctxstack(ref lai_state state, lai_aml_segment seg, byte* code, lai_nsnode handle, lai_invocation inno)
        {
            var x = new lai_ctxitem() { amls = seg, code = code, handle = handle, invocation = inno };
            state.ctxstack_base.Add(x);

            return x;
        }
        public static lai_operand lai_exec_push_opstack(ref lai_state state)
        {
            state.opstack_base.Add(new lai_operand() { objectt = new lai_variable() });

            return state.opstack_base[state.opstack_base.Count - 1];
        }
        private static lai_aml_segment lai_load_table(void* ptr, int index)
        {
            var x = new lai_aml_segment();
            x.table = (acpi_aml_t*)ptr;
            x.index = index;

            return x;
        }
        private static string lai_stringify_node_path(lai_nsnode node)
        {
            if (node.parent == null)
            {
                // Handle the trivial case.
                LAI_ENSURE(node.type == LAI_NAMESPACE_ROOT, "lai_stringify_node_path: node.type == LAI_NAMESPACE_ROOT");
                return "\\";
            }

            lai_nsnode current;
            int num_segs = 0;
            for (current = node; current.parent != null; current = current.parent)
            {
                num_segs++;
            }

            int length = num_segs * 5; // Leading dot (or \) and four chars per segment.
            // Build the string from right to left.
            string x = "";
            var n = length;
            for (current = node; current.parent != null; current = current.parent)
            {
                n -= 4;
                x = current.name + x;
                n -= 1;
                x = "." + x;
            }
            LAI_ENSURE(n == 0, "lai_stringify_node_path: N has to be 0");
            x = "\\" + x.Substring(1);
            return x;
        }
        private static void lai_panic(string error)
        {
            Cosmos.System.Kernel.PrintDebug("LAI: ERROR: " + error);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("LAI PANIC: " + error);
            Console.WriteLine("This is a critical error. Press the ENTER key to skip. Not Recommended");
            //SerialPort.SendString("LAI ERROR: " + error);
            Console.ForegroundColor = ConsoleColor.White;
            Console.ReadLine();
        }
        private static void lai_warn(string warn)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARN: " + warn);
            Console.ForegroundColor = ConsoleColor.White;
            //SerialPort.SendString("LAI WARN: " + warn);
            Cosmos.System.Kernel.PrintDebug("LAI: WARN: " + warn);
        }
        private static void lai_log(string msg)
        {
            if (msg == "\n")
            {
                Console.WriteLine(msg);
                //Kernel.PrintDebug(msg);
            }
            else
            {
                Console.WriteLine("LAI: " + msg);
                // Kernel.PrintDebug("LAI: " + msg);
            }
            //SerialPort.SendString("LAI: " + msg);
        }
    }
    public delegate int lai_method_override(lai_variable args, ref lai_variable result);
    public unsafe class lai_nsnode
    {
        public string name;
        public int type;
        public lai_nsnode parent;
        public lai_nsnode al_target;
        public lai_variable objectt;
        public List<lai_nsnode> children = new List<lai_nsnode>();
        public int method_flags;
        public lai_method_override method_override;
        public lai_aml_segment amls;
        public byte* pointer;
        public lai_nsnode fld_region_node;
        public ulong fld_offset;
        public ulong fld_size;
        public byte fld_flags;
        /// <summary>
        /// AML offset
        /// </summary>
        public int offset;
        public int size;
        internal ulong op_address_space;
        internal object op_base;
        internal ulong op_length;
        internal byte cpu_id;
        internal byte pblk_len;
        internal uint pblk_addr;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct acpi_aml_t
    {
        public AcpiHeader header;
    }
    public unsafe class lai_aml_segment
    {
        public acpi_aml_t* table;
        // Index of the table (e.g., for SSDTs).
        public int index;
    }
    public unsafe class lai_variable
    {
        public int type;
        /// <summary>
        /// For Name()
        /// </summary>
        public ulong integer;


        public int iref_index;

        public lai_nsnode handle;
        public int index;
        internal byte[] buffer;

        public lai_variable[] pkg_items;
        internal int pkg_rc;
        internal int unres_aml_pc;
        internal byte* unres_aml_method;
        internal lai_nsnode unres_ctx_handle;

        public string stringval;
    }
    public class lai_invocation
    {
        public lai_variable[] arg = new lai_variable[7];
        public lai_variable[] local = new lai_variable[8];

        //todo: per_method_list
    }
    public unsafe class lai_ctxitem
    {
        public lai_aml_segment amls;
        public byte* code;
        public lai_nsnode handle;
        public lai_invocation invocation;
    }
    /// <summary>
    /// The block stack stores a program counter (PC) and PC limit.
    /// </summary>
    public class lai_blkitem
    {
        public int pc;
        public int limit;
    }

    public class lai_state
    {
        public List<lai_ctxitem> ctxstack_base = new List<lai_ctxitem>();
        public List<lai_blkitem> blkstack_base = new List<lai_blkitem>();
        public List<lai_stackitem> stack_base = new List<lai_stackitem>();
        public List<lai_operand> opstack_base = new List<lai_operand>();
        internal unsafe FADTPtr* fadt;

        public int ctxstack_capacity;
        public int blkstack_capacity;
        public int stack_capacity;
        public int opstack_capacity;

        public int ctxstack_ptr; // Stack to track the current context.
        public int blkstack_ptr; // Stack to track the current block.
        public int stack_ptr; // Stack to track the current execution state.
        public int opstack_ptr;

        //TODO
    }

    public class lai_stackitem
    {
        public int kind;
        // For stackitem accepting arguments.
        public int opstack_frame;
        public byte mth_want_result;
        public int cond_state;
        public int cond_has_else;
        public int cond_else_pc;
        public int cond_else_limit;
        public int loop_state;
        /// <summary>
        /// Loop predicate PC.
        /// </summary>
        public int loop_pred;
        public byte buf_want_result;
        public int pkg_index;
        /// <summary>
        /// 0: Parse size, 1: Create Object, 2: Enumerate items
        /// </summary>
        public int pkg_phase;
        public byte pkg_want_result;
        public int op_opcode;
        public byte[] op_arg_modes;
        public byte op_want_result;
        public int node_opcode;
        public byte[] node_arg_modes;
        public int ivk_argc;
        public byte ivk_want_result;

        /// <summary>
        /// 1 if vaild, 0 if not
        /// </summary>
        public int vaild;
    }
    public unsafe class lai_amlname
    {
        public bool is_absolute;
        public int height;
        public bool search_scopes;

        public byte* it;
        public byte* end;

        internal lai_amlname Clone()
        {
            return new lai_amlname() { end = end, it = it, search_scopes = search_scopes, height = height, is_absolute = is_absolute };
        }
    }

    public unsafe class lai_operand
    {
        public int tag;
        public lai_variable objectt;
        public int index;
        public lai_nsnode unres_ctx_handle;
        /// <summary>
        /// offset
        /// </summary>
        public int unres_aml;
        public byte* cosmos_aml;
        public lai_nsnode handle;
    }
}
