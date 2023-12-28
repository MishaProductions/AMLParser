using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosACPIAML.ACPI
{
    public unsafe partial class LAI
    {
        public static void lai_eisaid(ref lai_variable _object, string id)
        {
            int n = id.Length;
            if (id.Length != 7)
            {
                // Handle string creation and memory allocation
                // This part needs further details on what lai_create_string and lai_exec_string_access do
                // For now, we'll simulate a panic if it's not the correct length
                Console.WriteLine("could not allocate memory for string or invalid length");
                return;
            }

            _object.type = LAI_INTEGER;

            uint outVal = 0;
            outVal |= (uint)((id[0] - 0x40) << 26);
            outVal |= (uint)((id[1] - 0x40) << 21);
            outVal |= (uint)((id[2] - 0x40) << 16);
            outVal |= (uint)(char_to_hex(id[3]) << 12);
            outVal |= (uint)(char_to_hex(id[4]) << 8);
            outVal |= (uint)(char_to_hex(id[5]) << 4);
            outVal |= (uint)(char_to_hex(id[6]));

            outVal = bswap32(outVal);
            _object.integer = outVal & 0xFFFFFFFF;
        }

        public static byte char_to_hex(char character)
        {
            if (character >= '0' && character <= '9')
                return (byte)(character - '0');
            else if (character >= 'A' && character <= 'F')
                return (byte)(character - 'A' + 10);
            else if (character >= 'a' && character <= 'f')
                return (byte)(character - 'a' + 10);

            return 0;
        }

        public static uint bswap32(uint dword)
        {
            return (uint)((dword >> 24) & 0xFF) | ((dword << 8) & 0xFF0000) | ((dword >> 8) & 0xFF00)
                   | ((dword << 24) & 0xFF000000);
        }
    }
}
