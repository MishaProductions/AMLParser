using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public const int ACPI_OPREGION_MEMORY = 0;
        public static void lai_read_opregion(ref lai_variable destination, lai_nsnode field)
        {
            if (field.type == LAI_NAMESPACE_FIELD || field.type == LAI_NAMESPACE_INDEXFIELD)
            {
                lai_read_field(ref destination, field);
            }
            else if (field.type == LAI_NAMESPACE_BANKFIELD)
            {
                lai_read_bankfield(destination, field);
            }
            else
            {
                lai_panic("unknown field read");
            }
        }

        private static void lai_read_bankfield(lai_variable destination, lai_nsnode field)
        {
            lai_log("lai_read_bankfield: TODO");
        }


        private static void lai_read_field_internal(byte[] buffer, lai_nsnode field)
        {
            ulong access_size = (ulong)lai_calculate_access_width(field);
            ulong offset = (field.fld_offset & (ulong)~(access_size - 1)) / 8;

            ulong progress = 0;
            while (progress < field.fld_size)
            {
                ulong bit_offset = (field.fld_offset + progress) & (ulong)(access_size - 1);
                ulong access_bits = ((field.fld_size - progress) > (access_size - bit_offset) ? (access_size - bit_offset) : (field.fld_size - progress));
                ulong mask = ((ulong)1 << (int)access_bits) - 1;

                ulong value = 0;
                
                if (field.type == LAI_NAMESPACE_FIELD || field.type == LAI_NAMESPACE_BANKFIELD)
                {
                    value = lai_perform_read(field.fld_region_node, access_size, offset);
                }
                else if (field.type == LAI_NAMESPACE_INDEXFIELD)
                {
                    value = lai_perform_indexfield_read(field, access_size, offset);
                }
                else
                {
                    lai_panic("Unknown field type in lai_write_field_internal");
                }

                value = (value >> (int)bit_offset) & mask;

                lai_buffer_put_at(buffer, value, progress, access_bits);

                progress += access_bits;
                offset += access_size / 8;
            }
        }

        private static ulong lai_perform_indexfield_read(lai_nsnode field, ulong access_size, ulong offset)
        {
            Console.WriteLine("WARN: lai_perform_indexfield_read not implemented");
            return 0;
        }

        private static ulong lai_perform_read(lai_nsnode fld_region_node, ulong access_size, ulong offset)
        {
            Console.WriteLine("WARN: lai_perform_read not implemented");
            return 0;
        }

        private static void lai_buffer_put_at(byte[] buffer, ulong value, ulong bit_offset, ulong num_bits)
        {
            ulong progress = 0;
            while (progress < num_bits)
            {
                ulong in_byte_offset = (bit_offset + progress) & 7;
                ulong access_size = ((num_bits - progress) > (8 - in_byte_offset) ? (8 - in_byte_offset) : (num_bits - progress));
                ulong mask = ((ulong)1 << (int)access_size) - 1;

                buffer[(bit_offset + progress) / 8] |= (byte)(((value >> (int)progress) & mask) << (int)in_byte_offset);

                progress += access_size;
            }
        }

        private static int lai_calculate_access_width(lai_nsnode field)
        {
            lai_nsnode opregion = field.fld_region_node;
            int access_size = 0;
            switch (field.fld_flags)
            {
                case FIELD_BYTE_ACCESS:
                    access_size = 8;
                    break;
                case FIELD_WORD_ACCESS:
                    access_size = 16;
                    break;
                case FIELD_DWORD_ACCESS:
                    access_size = 32;
                    break;
                case FIELD_QWORD_ACCESS:
                    access_size = 64;
                    break;
                case FIELD_ANY_ACCESS:
                    {
                        // This rounds up to the next power of 2.
                        access_size = 1;
                        if (field.fld_size > 1)
                            access_size = 1 << (32 - BitOperations.LeadingZeroCount(field.fld_size - 1));

                        int max_access_width = 32;
                        if (opregion.op_address_space == ACPI_OPREGION_MEMORY)
                            max_access_width = 64;

                        if (access_size > max_access_width)
                            access_size = max_access_width;

                        if (access_size < 8)
                            access_size = 8;

                        break;
                    }
                default:
                    lai_panic("invalid access size");
                    break;
            }
            return access_size;
        }

        private static void lai_read_field(ref lai_variable destination, lai_nsnode field)
        {
            ulong bytes = (field.fld_size + 7) / 8;
            lai_variable var = new();
            if (bytes > 8)
            {
                lai_create_buffer(destination, (int)bytes);
                lai_read_field_internal(destination.buffer, field);
            }
            else
            {
                byte[] buf = new byte[bytes];
                lai_read_field_internal(buf, field);

                ulong value = 0;
                for (ulong i = 0; i < bytes; i++)
                {
                    value |= (ulong)buf[i] << (int)(i * 8);
                }

                var.type = LAI_INTEGER;
                var.integer = value;
            }

            destination = var;
        }
    }
}
