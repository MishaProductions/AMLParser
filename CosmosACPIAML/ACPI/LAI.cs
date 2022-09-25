using Cosmos.Core;
using Cosmos.System.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Cosmoss.Core.ACPI;

namespace Cosmoss.Core
{
    public unsafe partial class ACPI
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

        private const int EXTOP_PREFIX = 0x5B;

        private const int LAI_POPULATE_STACKITEM = 1;
        private const int LAI_NODE_STACKITEM = 7;
        // Evaluate constant data (and keep result).
        //     Primitive objects are parsed.
        //     Names are left unresolved.
        //     Operations (e.g. Add()) are not allowed.
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
        private const int ZERO_OP = 0x00;
        private const int ONE_OP = 0x01;
        private const int ALIAS_OP = 0x06;
        private const int NAME_OP = 0x08;


        private static lai_nsnode RootNode;

        List<lai_nsnode> ns_array = new List<lai_nsnode>();
        private static void lai_create_root()
        {
            RootNode = new lai_nsnode();
            RootNode.type = LAI_NAMESPACE_ROOT;
            RootNode.name = "\\___";
            RootNode.parent = null;

            // Create the predefined objects.
            var sb_node = new lai_nsnode();
            sb_node.type = LAI_NAMESPACE_DEVICE;
            sb_node.name = "_SB_";
            sb_node.parent = RootNode;
            lai_install_nsnode(sb_node);

            var si_node = new lai_nsnode();
            si_node.type = LAI_NAMESPACE_DEVICE;
            si_node.name = "_SI_";
            si_node.parent = RootNode;
            lai_install_nsnode(si_node);

            var gpe_node = new lai_nsnode();
            gpe_node.type = LAI_NAMESPACE_DEVICE;
            gpe_node.name = "_GPE";
            gpe_node.parent = RootNode;
            lai_install_nsnode(gpe_node);

            var pr_node = new lai_nsnode();
            pr_node.type = LAI_NAMESPACE_DEVICE;
            pr_node.name = "_PR_";
            pr_node.parent = RootNode;
            lai_install_nsnode(pr_node);

            var tz_node = new lai_nsnode();
            tz_node.type = LAI_NAMESPACE_DEVICE;
            tz_node.name = "_TZ_";
            tz_node.parent = RootNode;
            lai_install_nsnode(tz_node);

            // Create the OS-defined objects.
            var osi_node = new lai_nsnode();
            osi_node.type = LAI_NAMESPACE_METHOD;
            osi_node.name = "_OSI";
            osi_node.method_flags = 0x1;
            osi_node.method_override = DoOSIMethod;
            lai_install_nsnode(osi_node);

            var rev_node = new lai_nsnode();
            rev_node.type = LAI_NAMESPACE_METHOD;
            rev_node.name = "_REV";
            rev_node.method_flags = 0x0;
            rev_node.method_override = DoRevMethod;
            lai_install_nsnode(rev_node);
        }

        private static void DoRevMethod()
        {
            Console.WriteLine("No implemenation: DoRevMethod");
        }

        private static void DoOSIMethod()
        {
            Console.WriteLine("No implemenation: DoOSIMethod");
        }

        /// <summary>
        /// Creates the ACPI namespace. Requires the ability to scan for ACPI tables
        /// </summary>
        private static void lai_create_namespace()
        {
            lai_create_root();

            // Create the namespace with all the objects.
            var state = new lai_state();
            var dsdt = (uint*)FADT->Dsdt;
            if (dsdt == null)
            {
                lai_panic("unable to find ACPI DSDT");
            }

            var tb = lai_load_table(dsdt, 0);
            lai_populate(RootNode, tb, state);
            lai_finalize_state(state);

            Console.WriteLine("lai_create_namespace finished");
        }

        private static void lai_finalize_state(lai_state state)
        {
            while (state.ctxstack_ptr >= 0)
                lai_exec_pop_ctxstack_back(state);
            while (state.blkstack_ptr >= 0)
                lai_exec_pop_blkstack_back(state);
            while (state.stack_ptr >= 0)
                lai_exec_pop_stack_back(state);
            lai_exec_pop_opstack(state, state.opstack_ptr);
        }



        private static void lai_populate(lai_nsnode parent, lai_aml_segment amls, lai_state state)
        {
            var size = amls.table->header.Length - sizeof(AcpiHeader);
            lai_ctxitem populate_ctxitem = lai_exec_push_ctxstack(state);
            populate_ctxitem.amls = amls;

            //<cosmos>
            uint dsdtAddress = FADT->Dsdt;
            uint dsdtLength = (uint)(*((int*)FADT->Dsdt + 1) - sizeof(AcpiHeader));

            var dsdtHeader = new MemoryBlock08(dsdtAddress, 36);
            var _reader = new BinaryReader(new MemoryStream(dsdtHeader.ToArray()));

            ReadHeader(_reader);

            var dsdtBlock = new MemoryBlock08(dsdtAddress + (uint)sizeof(AcpiHeader), SdtLength - (uint)sizeof(AcpiHeader));

            Stream stream = new MemoryStream(dsdtBlock.ToArray());

            //</cosmos>
            populate_ctxitem.code = dsdtBlock.ToArray();
            populate_ctxitem.handle = parent;

            var blkitem = lai_exec_push_blkstack(state);
            blkitem.pc = 0;
            blkitem.limit = (int)size;

            lai_stackitem item = lai_exec_push_stack(state, LAI_POPULATE_STACKITEM);
            //item.kind = LAI_POPULATE_STACKITEM;

            int status = lai_exec_run(state);
            if (status != 0)
            {
                Console.WriteLine("lai_exec_run() failed in lai_populate()");

            }
        }

        private static int lai_exec_run(lai_state state)
        {
            while (lai_exec_peek_stack_back(state).vaild == 1)
            {
                //debug
                int i = 0;
                while (true)
                {
                    lai_stackitem trace_item = lai_exec_peek_stack(state, i);
                    if (trace_item.vaild == 0)
                        break;

                    Console.WriteLine($"stack item {i} is of type {trace_item.kind}, opcode is {trace_item.op_opcode}");
                    i++;
                }

                //todo
                int e = lai_exec_process(state);
                if (e != 0)
                    return e;
            }

            return 0;
        }
        private static void lai_exec_pop_blkstack_back(lai_state state)
        {
            state.blkstack_ptr--;
            state.blkstack_base.RemoveAt(state.blkstack_base.Count - 1);
        }
        private static void lai_exec_pop_ctxstack_back(lai_state state)
        {
            state.ctxstack_ptr--;
            state.ctxstack_base.RemoveAt(state.ctxstack_base.Count - 1);
        }
        private static void lai_exec_pop_stack_back(lai_state state)
        {
            state.stack_ptr--;
            state.stack_base.RemoveAt(state.stack_base.Count - 1);
        }
        private static void lai_exec_pop_opstack(lai_state state, int n)
        {
            //todo
            //state.opstack_ptr -= n;
        }
        private static void lai_exec_reserve_opstack(lai_state state)
        {

        }
        private static int lai_exec_process(lai_state state)
        {
            lai_stackitem item = lai_exec_peek_stack_back(state);
            lai_ctxitem ctxitem = lai_exec_peek_ctxstack_back(state);
            lai_blkitem block = lai_exec_peek_blkstack_back(state);
            lai_aml_segment amls = ctxitem.amls;
            byte[] method = ctxitem.code;

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
                lai_panic($"execution escaped out of code range. pc: " + block.pc + ", limit: " + block.limit);
            }
            Console.WriteLine("lai_exec_process: pc: " + block.pc + ",limit=" + block.limit);


            if (item.kind == LAI_POPULATE_STACKITEM)
            {
                if (block.pc == block.limit)
                {
                    lai_exec_pop_blkstack_back(state);
                    lai_exec_pop_ctxstack_back(state);
                    lai_exec_pop_stack_back(state);
                    return 0;
                }
                else
                {
                    return lai_exec_parse(LAI_EXEC_MODE, state);
                }
            }
            else if (item.kind == LAI_NODE_STACKITEM)
            {
                int k = state.opstack_ptr - item.opstack_frame;
                Console.WriteLine("k is " + k);
                if (Decompress(item.node_arg_modes)[k] == 0)
                {
                    //lai_operand operands = lai_exec_get_opstack(state, item.opstack_frame);
                    lai_panic("condition not implementaion");
                    return 3;
                }
                else
                {
                    return lai_exec_parse(Decompress(item.node_arg_modes)[k], state);
                }
            }
            else
            {
                Console.WriteLine("lai_exec_process: " + item.kind + " not implemented");
                return 1;
            }
        }
        private const int DUAL_PREFIX = 0x2E;
        private const int MULTI_PREFIX = 0x2F;
        private const int PARENT_CHAR = 0x5E;
        private const int ROOT_CHAR = 0x5C;
        private static bool lai_is_name(byte character)
        {
            if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z')
      || character == '_' || character == ROOT_CHAR || character == PARENT_CHAR
      || character == MULTI_PREFIX || character == DUAL_PREFIX)
                return true;

            else
                return false;
        }
        private static int lai_exec_parse(int parse_mode, lai_state state)
        {
            lai_ctxitem ctxitem = lai_exec_peek_ctxstack_back(state);
            lai_blkitem block = lai_exec_peek_blkstack_back(state);
            lai_aml_segment amls = ctxitem.amls;
            byte[] method = ctxitem.code;

            lai_nsnode ctx_handle = ctxitem.handle;
            lai_invocation invocation = ctxitem.invocation;

            int pc = block.pc;
            int limit = block.limit;

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
                lai_panic($"execution escaped out of code range. pc: " + block.pc + ", limit: " + block.limit);
            }

            // Whether we use the result of an expression or not.
            // If yes, it will be pushed onto the opstack after the expression is computed.
            int want_result = LAI_MF_RESOLVE | LAI_MF_INVOKE;// lai_mode_flags[parse_mode] & LAI_MF_RESULT;

            //todo big if statement

            // Process names. (todo)
            if (lai_is_name(method[pc]))
            {
                lai_amlname amln = new lai_amlname();
                if (!lai_parse_name(ref amln, method, ref pc, limit))
                {
                    Console.WriteLine("lai_parse_name failed");
                    return 1;
                }

                lai_exec_commit_pc(state, pc);
                string path = lai_stringify_amlname(amln);
                Console.WriteLine("parsing name " + path + " at " + pc);
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
            Console.WriteLine("parsing opcode " + opcode);

            switch (opcode)
            {
                case NAME_OP:
                    lai_exec_commit_pc(state, pc);
                    lai_stackitem node_item = lai_exec_push_stack(state, LAI_NODE_STACKITEM);
                    node_item.node_opcode = opcode;
                    node_item.opstack_frame = state.opstack_ptr;
                    byte[] x = new byte[8];
                    x[0] = LAI_UNRESOLVED_MODE;
                    x[1] = LAI_OBJECT_MODE;
                    x[2] = 0;
                    node_item.node_arg_modes = Compress(x);

                    state.stack_base[state.stack_base.Count - 1] = node_item;


                    break;
                default:
                    lai_panic("Unknown opcode: " + opcode);
                    break;
            }
            return 0;
        }

        private static string lai_stringify_amlname(lai_amlname in_amln)
        {
            // Make a copy to avoid rendering the original object unusable.
            var amln = in_amln;

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
            Console.WriteLine("ret len is " + ret.Length+"it: "+(int)amln.it+", end: "+(int)amln.end);
            if (ret.Length <= max_length)
            {

            }
            else
            {
                lai_panic("read too much of the string. the string is " + ret);
            }

            return ret;
        }

        private static bool lai_exec_reserve_stack(lai_state state)
        {
            return true;
        }

        private static bool lai_parse_name(ref lai_amlname amln, byte[] method, ref int pc, int limit)
        {
            pc += lai_amlname_parse(ref amln, method, pc);
            return true;
        }

        private static int lai_amlname_parse(ref lai_amlname amln, byte[] data, int startIndex)
        {
            amln.is_absolute = false;
            amln.height = 0;

            fixed (byte* ptr = data)
            {
                byte* begin = ptr + startIndex;
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

                amln.search_scopes = (!amln.is_absolute && amln.height == 0 && num_segs == 1) ? 1 : 0;
                amln.it = it;
                amln.end = it + 4 * num_segs;
                return (int)(amln.end - begin);
            }
        }

        private static ulong Compress(byte[] b)
        {
            if (b.Length != 8) { throw new Exception("Compress: invaild len: " + b.Length); }
            return BitConverter.ToUInt64(b, 0);
        }
        private static byte[] Decompress(ulong b)
        {
            return BitConverter.GetBytes(b);
        }
        private static void lai_exec_commit_pc(lai_state state, int pc)
        {
            //  var block = lai_exec_peek_blkstack_back(state);
            // block.pc = pc;
            //just in case
            state.blkstack_base[state.blkstack_base.Count - 1].pc = pc;
        }

        private static lai_blkitem lai_exec_peek_blkstack_back(lai_state state)
        {
            if (state.ctxstack_ptr < 0)
                return null;

            return state.blkstack_base[state.blkstack_base.Count - 1];
        }

        private static lai_ctxitem lai_exec_peek_ctxstack_back(lai_state state)
        {
            if (state.ctxstack_ptr < 0)
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
            if (state.stack_ptr - n < 0)
            {
                return new lai_stackitem() { vaild = 0 };
            }
            return state.stack_base[state.stack_base.Count - 1 - n];
        }

        private static lai_stackitem lai_exec_push_stack(lai_state state, int kind = 0)
        {
            state.stack_ptr++;
            state.stack_base.Add(new lai_stackitem() { vaild = 1, kind = kind });

            return state.stack_base[state.stack_base.Count - 1];
        }

        private static lai_blkitem lai_exec_push_blkstack(lai_state state)
        {
            state.blkstack_ptr++;

            state.blkstack_base.Add(new lai_blkitem());

            return state.blkstack_base[state.blkstack_base.Count - 1];
        }

        private static lai_ctxitem lai_exec_push_ctxstack(lai_state state)
        {
            state.ctxstack_ptr++;
            // LAI_ENSURE(state->ctxstack_ptr < state->ctxstack_capacity);
            state.ctxstack_base.Add(new lai_ctxitem());

            return state.ctxstack_base[state.ctxstack_base.Count - 1];
        }

        private static lai_aml_segment lai_load_table(void* ptr, int index)
        {
            var x = new lai_aml_segment();
            x.table = (acpi_aml_t*)ptr;
            x.index = index;

            return x;
        }
        private static void lai_init_state(lai_state t)
        {

        }

        private static void lai_panic(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("LAI PANIC: " + error);
            while (true) { }
        }

        private static void lai_install_nsnode(lai_nsnode node)
        {
            if (node.parent != null)
            {
                foreach (var child in node.parent.children)
                {
                    if (child.name == node.name)
                    {
                        Console.WriteLine("LAI: Attempt was made to insert duplicate namespace node: " + node.name + ", ignoring");
                        return;
                    }
                }

                node.parent.children.Add(node);
            }

        }
    }

    public class lai_nsnode
    {
        public string name;
        public int type;
        public lai_nsnode parent;
        public List<lai_nsnode> children = new List<lai_nsnode>();
        public int method_flags;
        public Action method_override;
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
        public int index = 0;
    }
    public class lai_variable_t
    {

    }
    public class lai_invocation
    {
        public byte[] arg;
        public byte[] local;

        //todo: per_method_list
    }
    public class lai_ctxitem
    {
        public lai_aml_segment amls;
        public byte[] code;
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
        //...
        public int ctxstack_ptr = -1; // Stack to track the current context.
        public int blkstack_ptr = -1; // Stack to track the current block.
        public int stack_ptr = -1;  // Stack to track the current execution state.
        public int opstack_ptr;
    }
    [System.Runtime.InteropServices.StructLayout(LayoutKind.Explicit)]
    public struct lai_stackitem
    {
        [FieldOffset(0)]
        public int kind;
        // For stackitem accepting arguments.
        [FieldOffset(4)]
        public int opstack_frame;

        //<union>
        [FieldOffset(8)]
        public byte mth_want_result;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int cond_state;
        [FieldOffset(12)]
        public int cond_has_else;
        [FieldOffset(16)]
        public int cond_else_pc;
        [FieldOffset(20)]
        public int cond_else_limit;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int loop_state;
        /// <summary>
        /// Loop predicate PC.
        /// </summary>
        [FieldOffset(12)]
        public int loop_pred;
        //</union>

        //<union>
        [FieldOffset(8)]
        public byte buf_want_result;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int pkg_index;
        /// <summary>
        /// 0: Parse size, 1: Create Object, 2: Enumerate items
        /// </summary>
        [FieldOffset(12)]
        public int pkg_phase;
        [FieldOffset(16)]
        public byte pkg_want_result;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int op_opcode;
        [FieldOffset(12)]
        public ulong op_arg_modes;
        [FieldOffset(20)]
        public byte op_want_result;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int node_opcode;
        [FieldOffset(12)]
        public ulong node_arg_modes;
        //</union>

        //<union>
        [FieldOffset(8)]
        public int ivk_argc;
        [FieldOffset(12)]
        public byte ivk_want_result;
        //</union>

        /// <summary>
        /// 1 if vaild, 0 if not
        /// </summary>
        [FieldOffset(32)]
        public int vaild;
    }
    public unsafe struct lai_amlname
    {
        public bool is_absolute;
        public int height;
        public int search_scopes;

        public byte* it;
        public byte* end;
    }
}
