using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static CosmosACPIAML.ACPI.LAI;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public enum lai_object_type
        {
            LAI_TYPE_NONE,
            LAI_TYPE_INTEGER,
            LAI_TYPE_STRING,
            LAI_TYPE_BUFFER,
            LAI_TYPE_PACKAGE,
            LAI_TYPE_DEVICE,
        };

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

        public static lai_api_error lai_obj_get_pkg(lai_variable objectt, long i, ref lai_variable outVar)
        {
            if (objectt.type != LAI.LAI_PACKAGE)
                return lai_api_error.LAI_ERROR_TYPE_MISMATCH;

            if (i >= objectt.pkg_items.Length)
                return lai_api_error.LAI_ERROR_OUT_OF_BOUNDS;

            lai_exec_pkg_load(ref outVar, objectt, i);

            return lai_api_error.LAI_ERROR_NONE;
        }

        public static void lai_exec_pkg_load(ref lai_variable outVar, lai_variable pkg, long i)
        {
            LAI.LAI_ENSURE(pkg.type == LAI.LAI_PACKAGE, "pkg.type == LAI.LAI_PACKAGE");
            lai_exec_pkg_var_load(ref outVar, pkg.pkg_items, i);
        }

        public static void lai_exec_pkg_var_load(ref lai_variable outVar, lai_variable[] head, long i)
        {
            outVar = head[i];
        }

        public static void lai_obj_clone(ref lai_variable dest, lai_variable source)
        {
            // Clone into a temporary object.
            lai_variable temp = source;
            dest = temp;
        }

        public static void lai_clone_string(ref lai_variable dest, lai_variable source)
        {
            dest.stringval = new string(source.stringval.ToCharArray());
        }

        public static void lai_clone_buffer(ref lai_variable dest, lai_variable source)
        {
            long size = source.buffer.Length;
            Array.Copy(source.buffer, dest.buffer, size);
        }

        public static void lai_clone_package(ref lai_variable dest, lai_variable src)
        {
            long n = src.pkg_items.Length;
            for (long i = 0; i < n; i++)
                lai_obj_clone(ref dest.pkg_items[(int)i], src.pkg_items[(int)i]);
        }

        public static lai_object_type lai_object_type_of_objref(lai_variable objectt)
        {
            switch (objectt.type)
            {
                case LAI_INTEGER:
                    return lai_object_type.LAI_TYPE_INTEGER;
                case LAI_STRING:
                    return lai_object_type.LAI_TYPE_STRING;
                case LAI_BUFFER:
                    return lai_object_type.LAI_TYPE_BUFFER;
                case LAI_PACKAGE:
                    return lai_object_type.LAI_TYPE_PACKAGE;

                default:
                    lai_panic("unexpected object type " + objectt.type +  " in lai_object_type_of_objref()");
                    return lai_object_type.LAI_TYPE_NONE;
            }
        }

        public static lai_object_type lai_object_type_of_node(lai_nsnode handle)
        {
            switch (handle.type)
            {
                case LAI_NAMESPACE_DEVICE:
                    return lai_object_type.LAI_TYPE_DEVICE;
                default:
                    lai_panic("unexpected node type " + handle.type + " in lai_object_type_of_node()");
                    return lai_object_type.LAI_TYPE_NONE;
            }
        }

        public static lai_object_type lai_obj_get_type(lai_variable obj)
        {
            switch (obj.type)
            {
                case LAI_INTEGER:
                case LAI_STRING:
                case LAI_BUFFER:
                case LAI_PACKAGE:
                    return lai_object_type_of_objref(obj);

                case LAI_HANDLE:
                    return lai_object_type_of_node(obj.handle);
                case LAI_LAZY_HANDLE:
                    var amln = new lai_amlname(); // Assuming this is a class or struct
                    lai_amlname_parse(ref amln, obj.unres_aml_method, obj.unres_aml_pc);

                    var handle = lai_do_resolve(obj.unres_ctx_handle, ref amln);
                    if (handle == null)
                        throw new InvalidOperationException($"undefined reference {lai_stringify_amlname(amln)}");
                    return lai_object_type_of_node(handle);

                case 0:
                    return lai_object_type.LAI_TYPE_NONE;
                default:
                    throw new InvalidOperationException($"unexpected object type {obj.type} for lai_obj_get_type()");
            }
        }

        public static lai_api_error lai_obj_get_handle(lai_variable objectt, ref lai_nsnode outNode)
        {
            switch (objectt.type)
            {
                case LAI_HANDLE:
                    outNode = objectt.handle;
                    return lai_api_error.LAI_ERROR_NONE;
                case LAI_LAZY_HANDLE:
                    lai_amlname amln = new();
                    lai_amlname_parse(ref amln, objectt.unres_aml_method, objectt.unres_aml_pc);

                    lai_nsnode handle = lai_do_resolve(objectt.unres_ctx_handle, ref amln);
                    if (handle == null)
                        throw new InvalidOperationException($"undefined reference {lai_stringify_amlname(amln)}");
                    outNode = handle;
                    return lai_api_error.LAI_ERROR_NONE;

                default:
                    lai_warn("lai_obj_get_handle() expects a handle type, not a value of type " + objectt.type);
                    return lai_api_error.LAI_ERROR_TYPE_MISMATCH;
            }
        }
    }
}
