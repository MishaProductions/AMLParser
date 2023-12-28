using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        private static int lai_exec_reduce_op(int opcode, lai_state state, lai_operand[] operands, ref lai_variable reduction_res)
        {
            lai_variable result = new lai_variable();
            Console.WriteLine("lai_exec_reduce_op:" + opcode);

            switch (opcode)
            {
                case STORE_OP:
                    {
                        lai_variable objectref = new();
                        lai_variable outt = new();
                        lai_exec_get_objectref(state, operands, ref objectref);

                        lai_obj_clone(ref result, ref objectref);

                        // Store a copy to the target operand.
                        // TODO: Verify that we HAVE to make a copy.
                        lai_obj_clone(ref outt, ref result);
                        lai_operand_mutate(ref state, ref operands[1], ref result);
                        break;
                    }
                case AND_OP:
                    {
                        lai_variable lhs = new();
                        lai_variable rhs = new();
                        lai_exec_get_integer(state, operands[0], ref lhs);
                        lai_exec_get_integer(state, operands[1], ref rhs);

                        result.type = LAI_INTEGER;
                        result.integer = lhs.integer & rhs.integer;
                        lai_operand_mutate(ref state, ref operands[2], ref result);
                        break;
                    }
                case LEQUAL_OP:
                    {
                        lai_variable lhs = new();
                        lai_exec_get_objectref(state, operands, ref lhs);
                        lai_variable rhs = new();
                        lai_exec_get_objectref(state, cosmos_shift_array_by_index(operands, 1), ref rhs);

                        int res = 0;
                        var err = lai_obj_exec_match_op(MATCH_MEQ, ref lhs, ref rhs, ref res);
                        if (err != 0)
                        {
                            return 6;
                        }
                        result.type = LAI_INTEGER;
                        result.integer = res != 0 ? ~((ulong)0) : 0;
                        break;
                    }
                default:
                    lai_panic("lai_exec_reduce_op: unknown opcode 0x" + opcode.ToString("X"));
                    break;
            }
            reduction_res = result;
            return 0;
        }
        /// <summary>
        /// Returns an new array that starts with index
        /// </summary>
        /// <param name="operands"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private static lai_operand[] cosmos_shift_array_by_index(lai_operand[] operands, int index)
        {
            lai_operand[] new_array = new lai_operand[operands.Length];
            int j = 0;
            for (int i = index; i < operands.Length; i++)
            {
                new_array[j] = operands[i];
                j++;
            }
            return new_array;
        }

        private static void lai_operand_mutate(ref lai_state state, ref lai_operand dest, ref lai_variable result)
        {
            if (dest.tag == LAI_OPERAND_OBJECT)
            {
                switch (dest.objectt.type)
                {
                    default:
                        lai_panic($"unexpected object type {dest.objectt.type} for lai_store_overwrite()");
                        break;
                }
            }
        }

        private static void lai_obj_clone(ref lai_variable dest, ref lai_variable source)
        {
            dest = source;
        }
        private static int lai_exec_run(lai_state state)
        {
            while (lai_exec_peek_stack_back(state) != null)
            {
                ////debug
                //int i = 0;
                //while (true)
                //{
                //    lai_stackitem trace_item = lai_exec_peek_stack(state, i);
                //    if (trace_item == null)
                //        break;

                //    if (trace_item.kind == LAI_OP_STACKITEM)
                //        lai_log($"stack item {i} is of type {trace_item.kind}, opcode is {trace_item.op_opcode}");
                //    else
                //        lai_log($"stack item {i} is of type {trace_item.kind}");
                //    i++;
                //}

                int e = lai_exec_process(state);
                if (e != 0)
                    return e;
            }

            return 0;
        }
        public static int lai_eval_args(ref lai_variable result, lai_nsnode handle, lai_state state, int n, lai_variable args)
        {
            switch (handle.type)
            {
                case LAI_NAMESPACE_NAME:
                    if (n != 0)
                    {
                        lai_warn("non-empty argument list given when evaluating Name()");
                        return 2;
                    }
                    if (result != null)
                    {
                        lai_obj_clone(ref result, ref handle.objectt);
                    }
                    return 0;
                case LAI_NAMESPACE_METHOD:

                    lai_variable method_result = new();
                    int e = 0;
                    if (handle.method_override != null)
                    {
                        // It's an OS-defined method.
                        // TODO: Verify the number of argument to the overridden method.
                        e = handle.method_override(args, ref method_result);
                    }
                    else
                    {
                        // It's an AML method.
                        if (handle.amls == null)
                        {
                            lai_panic("lai_eval_args: expected amls");
                        }

                        var method_ctxitem = lai_exec_push_ctxstack(ref state, handle.amls, handle.pointer, handle, new lai_invocation());

                        var blkitem = lai_exec_push_blkstack(ref state, 0, handle.size);

                        var item = lai_exec_push_stack(ref state);
                        item.kind = LAI_METHOD_STACKITEM;
                        item.mth_want_result = 1;

                        e = lai_exec_run(state);

                        if (e == 0)
                        {
                            if (state.ctxstack_base.Count > 0)
                            {
                                lai_panic("ctxstack should be empty after running lai_exec_run");
                            }
                            if (state.stack_base.Count > 0)
                            {
                                lai_panic("stack should be empty after running lai_exec_run");
                            }
                            if (state.opstack_base.Count != 1)
                            {
                                lai_panic("opstack should be 1 after running lai_exec_run");
                            }
                            var opstack_top = lai_exec_get_opstack(state, 0);
                            lai_variable objectref = new();
                            lai_exec_get_objectref(state, opstack_top, ref objectref);
                            Console.WriteLine("opstacktop is " + objectref.type+" with val"+objectref.integer);
                            lai_obj_clone(ref method_result, ref objectref);
                            lai_var_finalize(objectref);
                            lai_exec_pop_opstack(ref state, 1);
                        }
                        else
                        {
                            Console.WriteLine("lai_exec_run failed");
                            // If there is an error the lai_state_t is probably corrupted, we should reset
                            // it
                            state = new();
                        }
                    }

                    if (e == 0 && result != null)
                    {
                        Console.WriteLine("method result OK, returned "+method_result.integer+", type "+method_result.type);
                        result = method_result;
                    }

                    return e;
                default:
                    return 2;
            }

        }
        public static int lai_eval(ref lai_variable result, lai_nsnode handle, lai_state state)
        {
            Console.WriteLine("evaling: " + lai_stringify_node_path(handle));
            return lai_eval_args(ref result, handle, state, 0, null);
        }
    }
}
