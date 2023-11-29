using Cosmos.HAL;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosLAI.LAI
{
    internal unsafe static partial class lai
    {
        private const int DUAL_PREFIX = 0x2E;
        private const int MULTI_PREFIX = 0x2F;
        #region Parse Flags
        private const int LAI_REFERENCE_MODE = 5;
        private const int LAI_OPTIONAL_REFERENCE_MODE = 6;
        private const int LAI_IMMEDIATE_BYTE_MODE = 7;
        private const int LAI_IMMEDIATE_WORD_MODE = 8;
        private const int LAI_IMMEDIATE_DWORD_MODE = 9;
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
        private const int EXTOP_PREFIX = 0x5B;

        //OpCodes
        private const int ZERO_OP = 0x00;
        private const int ONE_OP = 0x01;
        private const int ALIAS_OP = 0x06;
        private const int NAME_OP = 0x08;
        private const int FIELD = 0x81;
        private const int MUTEX = 0x01;
        private const int DEVICE = 0x82;
        private const int PROCESSOR = 0x83;
        private const int BUFFER_OP = 0x11;
        private const int PACKAGE_OP = 0x12;
        private const int METHOD_OP = 0x14;
        private const int NOP_OP = 0xA3;
        private const int OPREGION = 0x80;
        private const int SCOPE_OP = 0x10;

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
        public static void lai_exec_push_ctxstack(lai_state state, uint* aml, byte* code, lai_nsnode handle)
        {
            lai_ctxitem item = new() { aml = aml, code = code, handle = handle };
            state.ctxstack.Add(item);
        }
        public static lai_ctxitem lai_exec_peek_ctxstack_back(lai_state state)
        {
            if (state.ctxstack.Count == 0) { return null; }
            return state.ctxstack[state.ctxstack.Count - 1];
        }
        public static void lai_exec_pop_ctxstack_back(lai_state state)
        {
            if (state.ctxstack.Count == 0)
            {
                lai_panic("attempt to pop stack while no ctxstack items");
            }

            //todo destroy invocation
            state.ctxstack.RemoveAt(state.stack.Count - 1);
        }
        public static void lai_exec_push_blkstack(lai_state state, int pc, int limit)
        {
            lai_blkitem item = new() { pc = pc, limit = limit };
            state.blkstack.Add(item);
        }
        public static lai_blkitem lai_exec_peek_blkstack_back(lai_state state)
        {
            if (state.blkstack.Count == 0) { return null; }
            return state.blkstack[state.blkstack.Count - 1];
        }
        public static void lai_exec_pop_blkstack_back(lai_state state)
        {
            if (state.blkstack.Count == 0)
            {
                lai_panic("attempt to pop blkstack while no blkstack items");
            }

            state.blkstack.RemoveAt(state.blkstack.Count - 1);
        }
        public static lai_stackitem lai_exec_push_stack(lai_state state, lai_stackitem item)
        {
            state.stack.Add(item);
            return item;
        }
        public static lai_stackitem lai_exec_peek_stack(lai_state state, int n)
        {
            if (state.stack.Count - 1 - n < 0)
            {
                return null;
            }
            return state.stack[state.stack.Count - 1 - n];
        }
        public static lai_stackitem lai_exec_peek_stack_back(lai_state state)
        {
            return lai_exec_peek_stack(state, 0);
        }
        public static void lai_exec_pop_stack_back(lai_state state)
        {
            if (state.stack.Count == 0)
            {
                lai_panic("attempt to pop stack while no blkstack items");
            }

            state.stack.RemoveAt(state.stack.Count - 1);
        }


        // lai_exec_run(): This is the main AML interpreter function.
        public static int lai_exec_run(lai_state state)
        {
            while (lai_exec_peek_stack_back(state) != null)
            {
                int err = lai_exec_process(state);
                if (err != 0)
                {
                    return err;
                }
            }

            return 0;
        }

        private static bool lai_parse_varint(ref int outvar, byte* code, ref int pc, int limit)
        {
            if (pc + 1 > limit)
                return true;
            var sz = (code[pc] >> 6) & 3;
            Console.WriteLine("reading varint at pc : " + pc + ",sizer: " + sz);
            if (sz == 0)
            {
                outvar = (int)(code[pc] & 0x3F);
                pc++;
                Console.WriteLine("resullt: " + outvar);
                return false;
            }
            else if (sz == 1)
            {
                if (pc + 2 > limit)
                    return true;
                outvar = (int)(code[pc] & 0x0F) | (int)(code[pc + 1] << 4);
                pc += 2;
                Console.WriteLine("resullt: " + outvar);
                return false;
            }
            else if (sz == 2)
            {
                if (pc + 3 > limit)
                    return true;
                outvar = ((code[pc] & 0x0F) | (code[pc + 1] << 4)) | (code[pc + 2] << 12);
                pc += 3;
                Console.WriteLine("resullt: " + outvar);
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
                Console.WriteLine("resullt: " + outvar);
                return false;
            }
        }
        private static bool lai_parse_name(ref lai_amlname amln, byte* method, ref int pc, int limit)
        {
            pc += lai_amlname_parse(ref amln, method, pc);
            return false;
        }
        private static int lai_amlname_parse(ref lai_amlname amln, byte* ptr, int startIndex)
        {
            amln.is_absolute = false;
            amln.height = 0;
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
            amln.search_scopes = !amln.is_absolute && amln.height == 0 && num_segs == 1;
            amln.it = it;
            amln.end = it + 4 * num_segs;
            return (int)(amln.end - begin);

        }

        private static int lai_exec_process(lai_state state)
        {
            var item = lai_exec_peek_stack_back(state);
            var ctxitem = lai_exec_peek_ctxstack_back(state);
            var block = lai_exec_peek_blkstack_back(state);

            if (block.pc > block.limit)
            {
                lai_panic($"execution escaped out of code range [{block.pc + 36}, limit {block.limit + 36}");
            }

            if (item.kind == lai_stackitem_kind.Populate)
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
            else
            {
                lai_panic("unexpected lai_stackitem_t");
                return -1;
            }
        }
        // Advances the PC of the current block.
        // lai_exec_parse() calls this function after successfully parsing a full opcode.
        // Even if parsing fails, this mechanism makes sure that the PC never points to
        // the middle of an opcode.
        static void lai_exec_commit_pc(lai_state state, int pc)
        {
            lai_exec_peek_blkstack_back(state).pc = pc;
        }
        public static int lai_exec_parse(int parse_mode, lai_state state)
        {
            lai_ctxitem ctxitem = lai_exec_peek_ctxstack_back(state);
            lai_blkitem block = lai_exec_peek_blkstack_back(state);
            byte* method = ((byte*)ctxitem.aml + 36);
            int pc = block.pc;
            int limit = block.limit;

            if (!(block.pc < block.limit))
            {
                lai_panic($"execution escaped out of code range [{block.pc + 36}, limit {block.limit + 36}");
            }

            // Whether we use the result of an expression or not.
            // If yes, it will be pushed onto the opstack after the expression is computed.
            int want_result = ReadFlags(parse_mode) & LAI_MF_RESULT;

            if (parse_mode == LAI_IMMEDIATE_BYTE_MODE)
            {
                // TODO
            }
            else if (parse_mode == LAI_IMMEDIATE_WORD_MODE)
            {
                // TODO
            }
            else if (parse_mode == LAI_IMMEDIATE_DWORD_MODE)
            {
                // TODO
            }

            if (lai_is_name(method[pc]))
            {
                lai_amlname amln = new();
                if (lai_parse_name(ref amln, method, ref pc, limit))
                {
                    Console.WriteLine("failed to parse name");
                    return -2;
                }

                lai_panic("todo string aml case");
            }

            // General opcodes
            int opcode;
            if (method[pc] == EXTOP_PREFIX)
            {
                if (pc + 1 == limit)
                {
                    lai_panic("2 byte opcode on method boundary");
                }
                opcode = (EXTOP_PREFIX << 8) | method[pc + 1];
                pc += 2;
            }
            else
            {
                opcode = method[pc];
                pc++;
            }
            Console.WriteLine("new OpCode " + opcode.ToString("X") + ", PC=" + pc);

            // This switch handles the majority of all opcodes.
            switch (opcode)
            {
                case NOP_OP:
                    lai_exec_commit_pc(state, pc);
                    break;

                case ZERO_OP:
                    break;

                case NAME_OP:
                    lai_exec_commit_pc(state, pc);

                    lai_stackitem node_item = lai_exec_push_stack(state, new lai_stackitem());
                    node_item.kind = lai_stackitem_kind.Node;
                    node_item.node_opcode = opcode;
                    node_item.opstack_frame = state.stack.Count - 1;
                    node_item.node_arg_modes1 = LAI_UNRESOLVED_MODE;
                    node_item.node_arg_modes2 = LAI_OBJECT_MODE;
                    node_item.node_arg_modes3 = 0;

                    break;
                default:
                    lai_panic("UNKNOWN OpCode 0x" + opcode.ToString("X"));
                    return -1;
            }

            return 0;
        }
        public static int lai_populate(lai_nsnode parent, uint* aml, lai_state state)
        {
            uint size = (*(aml + 1));
            Console.WriteLine("DSDT size: " + size);
            lai_exec_push_ctxstack(state, aml, (((byte*)aml) + 36), parent);
            lai_exec_push_blkstack(state, 0, (int)size);
            lai_exec_push_stack(state, new lai_stackitem() { kind = lai_stackitem_kind.Populate });

            int status = lai_exec_run(state);
            if (status != 0)
            {
                Console.WriteLine("lai_exec_run() failed in lai_populate");
                return status;
            }

            return 0;
        }
    }

    public unsafe class lai_ctxitem
    {
        public uint* aml;
        public byte* code;
        public lai_nsnode handle;
    }

    // The block stack stores a program counter (PC) and PC limit.
    public class lai_blkitem
    {
        public int pc;
        public int limit;
    }

    public enum lai_stackitem_kind
    {
        Populate = 1,
        Method,
        Loop,
        Condition,
        Buffer,
        Package,
        Node,
        OP,
        Invoke,
        Return,
        Bankfield,
        Varpackage
    }
    public class lai_stackitem
    {
        public lai_stackitem_kind kind;
        public int opstack_frame;

        public int node_opcode;
        public byte node_arg_modes1;
        public byte node_arg_modes2;
        public byte node_arg_modes3;
    }
    public unsafe struct lai_amlname
    {
        public bool is_absolute;
        public int height;
        public bool search_scopes;
        public byte* it;
        public byte* end;
    }
}
