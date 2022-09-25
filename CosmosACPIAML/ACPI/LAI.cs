﻿using Cosmos.Core;
using Cosmos.System.Graphics;
using Cosmos.System.Network.IPv4.TCP;
using CosmosACPIAMl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
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

            var dsdtBlock = new MemoryBlock08(dsdtAddress + 36, SdtLength);
            Console.WriteLine("The DSDT is located at " + FADT->Dsdt);
            Stream stream = new MemoryStream(dsdtBlock.ToArray());

            //</cosmos>

            //  Kernel.PrintDebug(Convert.ToBase64String(dsdtBlock.ToArray()));

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
                    lai_operand operands = lai_exec_get_opstack(state, item.opstack_frame);
                    lai_exec_reduce_node(item.node_opcode, state, operands, ctx_handle);
                    lai_exec_pop_opstack(state, k);

                    lai_exec_pop_stack_back(state);
                    return 0;
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

        private static void lai_exec_reduce_node(int opcode, lai_state state, lai_operand operands, lai_nsnode ctx_handle)
        {
            Console.WriteLine("lai_exec_reduce_node: opcode " + opcode);
            switch (opcode)
            {
                case NAME_OP:
                    lai_variable objectt = new lai_variable();
                    // ..lai_exec_get_objectref(state, &operands[1], objectt);
                    objectt = operands.objectt;

                    if (operands.tag != LAI_UNRESOLVED_NAME)
                    {
                        lai_panic("assertion failure: tag must be LAI_UNRESOLVED_NAME in lai_exec_reduce_node");
                    }
                    lai_amlname amln = new lai_amlname();
                    lai_amlname_parse(ref amln, operands.cosmos_aml, operands.unres_aml);
                    Console.WriteLine($"it: {(int)amln.it}, end: {(int)amln.end}");
                    lai_nsnode node = new lai_nsnode();
                    node.type = LAI_NAMESPACE_NAME;
                    lai_do_resolve_new_node(node, ctx_handle, ref amln);
                    node.objectt = objectt;

                    lai_install_nsnode(node);
                    var item = lai_exec_peek_ctxstack_back(state);
                    if (item.invocation != null)
                    {
                        //todo
                        // item.invocation.per_method_list.Add(node.per_method_item);
                    }
                    break;
                default:
                    lai_panic("undefined opcode in lai_exec_reduce_node: " + opcode);
                    break;
            }
        }

        private static void lai_do_resolve_new_node(lai_nsnode node, lai_nsnode ctx_handle, ref lai_amlname in_amln)
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
            Console.WriteLine($"it: {(int)amln.it}, end: {(int)amln.end}");
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
                    Console.WriteLine("Created NEW node: " + node.name);
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
                        Console.WriteLine("resolution of new object name traverses Alias() -  this is not supported in ACPICA");
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

        private static lai_operand lai_exec_get_opstack(lai_state state, int n)
        {
            if (n < state.opstack_ptr)
            {

            }
            else
            {
                lai_panic("fatal error: n < state.opstack_ptr must be true in lai_exec_get_opstack");
            }
            return state.opstack_base[n];
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
        private const int LAI_UNRESOLVED_NAME = 3;
        private const int METHOD_OP = 0x14;
        private const int NOP_OP = 0xA3;
        private const int OPREGION = 0x80;
        private const int SCOPE_OP = 0x10;
        private const int BYTEPREFIX = 0x0A;
        private const int WORDPREFIX = 0x0B;
        private const int DWORDPREFIX = 0x0C;
        private const int STRINGPREFIX = 0x0D;
        private const int QWORDPREFIX = 0x0E;
        private const int LAI_OPERAND_OBJECT = 1;
        private const int LAI_INTEGER = 1;
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
            int want_result = ReadFlags(parse_mode) & LAI_MF_RESULT;//LAI_MF_RESOLVE | LAI_MF_INVOKE;// lai_mode_flags[parse_mode] & LAI_MF_RESULT;

            //todo big if statement

            // Process names. (todo)
            int oldpc = pc;
            if (lai_is_name(method[pc]))
            {
                lai_amlname amln = new lai_amlname();
                if (lai_parse_name(ref amln, method, ref pc, limit))
                {
                    Console.WriteLine("lai_parse_name failed");
                    return 1;
                }

                lai_exec_commit_pc(ref state, pc);
                string path = lai_stringify_amlname(amln);
                Console.WriteLine("parsing name " + path + " at " + pc);
                if (parse_mode == LAI_DATA_MODE)
                {
                    if (want_result != 0)
                    {
                        //TODO
                        Console.WriteLine("parse_mode == LAI_DATA_MODE and want_result != 0 todo");
                    }
                }
                else if ((ReadFlags(parse_mode) & LAI_MF_RESOLVE) == 0)
                {
                    if (want_result != 0)
                    {
                        var opstack_res = lai_exec_push_opstack(state);
                        opstack_res.tag = LAI_UNRESOLVED_NAME;
                        opstack_res.unres_ctx_handle = ctx_handle;
                        Console.WriteLine("****PC: " + oldpc + "****");
                        opstack_res.unres_aml = oldpc;
                        opstack_res.cosmos_aml = method;

                        state.opstack_base[state.opstack_base.Count - 1] = opstack_res;
                    }
                }
                else
                {
                    Console.WriteLine("WARMING: RESOLVING NOT IMPLEMETNATION!");
                    lai_nsnode handle = lai_do_resolve(ctx_handle, ref amln);

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
            Console.WriteLine("parsing opcode " + opcode);

            switch (opcode)
            {
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
                                        Console.WriteLine("failed to parse BYTEPREFIX");
                                        return 5;
                                    }
                                    value = temp;
                                    break;
                                }
                            default:
                                Console.WriteLine("Data type not implemnation: " + opcode);
                                break;
                        }
                        lai_exec_commit_pc(ref state, pc);
                        if (parse_mode == LAI_DATA_MODE || parse_mode == LAI_OBJECT_MODE)
                        {
                            lai_operand result = lai_exec_push_opstack(state);
                            result.tag = LAI_OPERAND_OBJECT;
                            result.objectt.type = LAI_INTEGER;
                            result.objectt.integer = value;
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
                        lai_stackitem node_item = lai_exec_push_stack(state, LAI_NODE_STACKITEM);
                        node_item.node_opcode = opcode;
                        node_item.opstack_frame = state.opstack_ptr;
                        byte[] x = new byte[8];
                        x[0] = LAI_UNRESOLVED_MODE;
                        x[1] = LAI_OBJECT_MODE;
                        x[2] = 0;
                        node_item.node_arg_modes = Compress(x);

                        state.stack_base[state.stack_base.Count - 1] = node_item;
                    }


                    break;
                case SCOPE_OP:
                    {
                        int nested_pc;
                        var encoded_size = 0;
                        lai_amlname amln = new lai_amlname();

                        if (lai_parse_varint(ref encoded_size, method, ref pc, limit) || lai_parse_name(ref amln, method, ref pc, limit))
                        {
                            Console.WriteLine("FAILURE");
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

                        var populate_ctxitem = lai_exec_push_ctxstack(state);
                        populate_ctxitem.amls = amls;
                        populate_ctxitem.code = method;
                        populate_ctxitem.handle = scoped_ctx_handle;
                        state.ctxstack_base[state.ctxstack_base.Count - 1] = populate_ctxitem;

                        var blkitem = lai_exec_push_blkstack(state);
                        blkitem.pc = nested_pc;
                        blkitem.limit = opcode_pc + 1 + encoded_size;

                        lai_stackitem item = lai_exec_push_stack(state, LAI_POPULATE_STACKITEM);
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
                            Console.WriteLine("Method_OP: failed to parse");
                            return 5;
                        }
                        int nested_pc = pc;
                        pc = opcode_pc + 1 + encoded_size;

                        lai_exec_commit_pc(ref state, pc);
                        lai_nsnode node = new lai_nsnode();
                        node.type = LAI_NAMESPACE_METHOD;
                        lai_do_resolve_new_node(node, ctx_handle, ref amln);
                        node.method_flags = flags;
                        node.amls = amls;
                        node.offset = nested_pc;
                        node.size = pc - nested_pc;
                        lai_install_nsnode(node);

                        break;
                    }
                     case (EXTOP_PREFIX << 8) | OPREGION:
                    {
                        lai_exec_commit_pc(ref state, pc);
                        lai_stackitem node_item = lai_exec_push_stack(state);

                        break;
                    }
                default:
                    lai_panic("Unknown opcode: " + opcode);
                    break;


            }
            return 0;
        }

        private static bool lai_parse_u8(ref byte output, byte[] code, ref int pc, int limit)
        {
            if (pc + 1 > limit)
                return true;

            output = code[pc];
            pc++;
            return false;
        }

        private static bool lai_parse_varint(ref int outvar, byte[] code, ref int pc, int limit)
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
                outvar = (int)(code[pc] & 0x0F) | (int)(code[pc + 1] << 4)
                       | (int)(code[pc + 2] << 12);
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
                string segment="";
                for (int i = 0; i < 4; i++)
                    segment += (char)amln.it[i];
                amln.it += 4;
                Console.WriteLine("Resolving " + segment + " by searching through scopes");
                while(current != null)
                {
                    lai_nsnode node = lai_ns_get_child(current, segment);
                    if(node == null)
                    {
                        current = current.parent;
                        continue;
                    }

                    if(node.type == LAI_NAMESPACE_ALIAS)
                    {
                        node = node.al_target;
                        LAI_ENSURE(node.type != LAI_NAMESPACE_ALIAS, "node->type != LAI_NAMESPACE_ALIAS");
                    }
                    Console.WriteLine("resolution returns " + lai_stringify_amlname(amln));
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
                    if(current.parent == null)
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

                if(current.type == LAI_NAMESPACE_ALIAS)
                {
                    current = current.al_target;
                    LAI_ENSURE(current.type == LAI_NAMESPACE_ROOT, "current->type == LAI_NAMESPACE_ROOT x4");
                }
                return current;
            }
            return null;
        }

        private static void LAI_ENSURE(bool condition, string message)
        {
            if (!condition)
            {
                lai_panic("Assertion failed: " + message);
            }
        }

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
            Console.WriteLine("ret len is " + ret.Length + "it: " + (int)amln.it + ", end: " + (int)amln.end);
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
            return false;
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

                amln.search_scopes = !amln.is_absolute && amln.height == 0 && num_segs == 1;
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
        private static void lai_exec_commit_pc(ref lai_state state, int pc)
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
        public static lai_operand lai_exec_push_opstack(lai_state state)
        {
            state.opstack_ptr++;
            // LAI_ENSURE(state->ctxstack_ptr < state->ctxstack_capacity);
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
        private static void lai_init_state(lai_state t)
        {

        }

        private static void lai_panic(string error)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("LAI PANIC: " + error);
            Console.WriteLine("This is a critical error. Press the ENTER key to skip. Not Recommended");
            Console.ReadLine();
            Console.ForegroundColor = ConsoleColor.White;
        }
        private static void lai_warn(string warn)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("WARN: " + warn);
            Console.ForegroundColor = ConsoleColor.White;
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

    public unsafe class lai_nsnode
    {
        public string name;
        public int type;
        public lai_nsnode parent;
        public lai_nsnode al_target;
        public lai_variable objectt;
        public List<lai_nsnode> children = new List<lai_nsnode>();
        public int method_flags;
        public Action method_override;
        public lai_aml_segment amls;
        public byte[] objectaml;
        /// <summary>
        /// AML offset
        /// </summary>
        public int offset;
        public int size;
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
    public class lai_variable
    {
        public int type;
        /// <summary>
        /// For Name()
        /// </summary>
        public ulong integer;


        public int iref_index;

        public lai_nsnode handle;
        public int index;
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
        public List<lai_operand> opstack_base = new List<lai_operand>();
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
        public bool search_scopes;

        public byte* it;
        public byte* end;
    }

    public unsafe struct lai_operand
    {
        public int tag;
        public lai_variable objectt;
        public int index;
        public lai_nsnode unres_ctx_handle;
        /// <summary>
        /// offset
        /// </summary>
        public int unres_aml;
        public byte[] cosmos_aml;
        public lai_nsnode handle;
    }
}