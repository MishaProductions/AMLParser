using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_init_state(ref lai_state state)
        {
            state = new lai_state();
            state.ctxstack_capacity = 8;
            state.blkstack_capacity = 8;
            state.stack_capacity = 16;
            state.opstack_capacity = 16;
            state.blkstack_ptr = -1;
            state.stack_ptr = -1;
        }

        private static int lai_exec_reduce_op(int opcode, lai_state state, lai_operand[] operands, ref lai_variable reduction_res)
        {
            lai_variable result = new lai_variable();

            switch (opcode)
            {
                default:
                    lai_panic("lai_exec_reduce_op: unknown opcode 0x" + opcode.ToString("X"));
                    break;
            }
            reduction_res = result;
            return 0;
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
                        lai_obj_clone(ref result, handle.objectt);
                        Cosmos.HAL.Global.debugger.Send("result.type=" + result.type);
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

                            Cosmos.HAL.Global.debugger.Send("LAI_NAMESPACE_METHOD TODO 1");
                            // TODO
                        }
                    }

                    return e;
                default:
                    return 2;
            }

        }
        public static int lai_eval(ref lai_variable result, lai_nsnode handle, lai_state state)
        {
            return lai_eval_args(ref result, handle, state, 0, null);
        }
    }
}
