using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_create_buffer(lai_variable objectt, int size)
        {
            objectt.type = LAI_BUFFER;
            objectt.buffer = new byte[size];
        }

        public static int lai_obj_get_integer(lai_variable objectt, ref ulong outt)
        {
            switch(objectt.type)
            {
                case LAI_INTEGER:
                    outt = objectt.integer;
                    return 0;
                default:
                    lai_warn("lai_obj_get_integer() expects an integer, not a value of type " + objectt.type);
                    return 2;
            }
        }


        // ...
        public static int lai_obj_to_integer(ref lai_variable result, lai_variable obj)
        {
            switch (obj.type)
            {
                case LAI_INTEGER:
                    lai_obj_clone(ref result, ref obj);
                    break;
                default:
                    lai_panic("lai_obj_to_integer doesnt support yet "+obj.type);
                    break;
            }
            return 0;
        }
        // ...
        public static int lai_obj_exec_match_op(int op, ref lai_variable var, ref lai_variable obj, ref int output)
        {
            lai_variable compare_obj = new();
            bool result;
            if (var.type == LAI_INTEGER)
            {
                int err = lai_obj_to_integer(ref compare_obj, obj);
                if (err != 0)
                {
                    return err;
                }

                switch (op)
                {
                    case MATCH_MTR: // MTR: Always True
                        result = true;
                        break;
                    case MATCH_MEQ: // MEQ: Equals
                        result = (var.integer == compare_obj.integer);
                        break;
                    case MATCH_MLE: // MLE: Less than or equal
                        result = (var.integer <= compare_obj.integer);
                        break;
                    case MATCH_MLT: // MLT: Less than
                        result = (var.integer < compare_obj.integer);
                        break;
                    case MATCH_MGE: // MGE: Greater than or equal
                        result = (var.integer >= compare_obj.integer);
                        break;
                    case MATCH_MGT: // MGT: Greater than
                        result = (var.integer > compare_obj.integer);
                        break;

                    default:
                        lai_warn("lai_obj_exec_match_op: Illegal op passed "+op);
                        return 7;
                }
            }
            else
            {
                //TODO
                lai_panic("lai_obj_exec_match_op only supports ints right now...");
                return -1;
            }

            Console.WriteLine("ints: "+var.integer+",cmp:"+compare_obj.integer);

            output = result ? 1 : 0;
            return 0;
        }
    }
}
