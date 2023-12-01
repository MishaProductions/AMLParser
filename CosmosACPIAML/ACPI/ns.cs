using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        private static lai_instance global_instance = new lai_instance();
        public static lai_instance lai_current_instance()
        {
            return global_instance;
        }

        // ...

        public static lai_nsnode lai_resolve_path(lai_nsnode ctx_handle, string path)
        {
            lai_nsnode current = ctx_handle;
            if (current == null)
            {
                current = lai_current_instance().RootNode;
            }

            fixed (char* pathtmp = path)
            {
                char* pathptr = pathtmp;
                if (*pathptr == '\\')
                {
                    while (current.parent != null)
                    {
                        current = current.parent;
                    }
                    if (current.type != LAI_NAMESPACE_ROOT)
                    {
                        lai_panic("expected namespace root in lai_resolve path");
                    }
                    pathptr++;
                }
                else
                {
                    int height = 0;
                    while(*pathptr == '^')
                    {
                        height++;
                        pathptr++;
                    }

                    for (int i = 0; i < height; i++)
                    {
                        if (current.parent == null)
                        {
                            if (current.type != LAI_NAMESPACE_ROOT)
                            {
                                lai_panic("expected namespace root in lai_resolve path");
                            }
                        }
                        current = current.parent;
                    }
                }

                if (*pathptr == 0)
                {
                    return current;
                }

                for(; ; )
                {
                    string segment = "";
                    int k;
                    for (k = 0; k < 4; k++)
                    {
                        if (!lai_is_name((byte)*pathptr))
                            break;
                        segment += *(pathptr++);
                    }

                    // ACPI pads names with trailing underscores.
                    while (k < 4)
                    {
                        segment += "_";
                        k++;
                    }

                    current = lai_ns_get_child(current, segment);
                    if (current == null)
                    {
                        return null;
                    }

                    if (current.type == LAI_NAMESPACE_ALIAS)
                    {
                        current = current.al_target;
                    }

                    if (*pathptr == 0)
                    {
                        break;
                    }

                    if (*pathptr != '.')
                    {
                        lai_panic("expected pathptr to have .");
                    }

                    pathptr++;
                }
            }

            return current;
        }

    }

    public class lai_instance
    {
        public lai_nsnode RootNode;
    }
}
