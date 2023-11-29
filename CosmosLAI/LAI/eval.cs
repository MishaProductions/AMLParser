using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosLAI.LAI
{
    internal unsafe static partial class lai
    {
        private const int PARENT_CHAR = 0x5E;
        private const int ROOT_CHAR = 0x5C;
        static bool lai_is_name(byte character)
        {
            if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z')
                || character == '_' || character == ROOT_CHAR || character == PARENT_CHAR
                || character == MULTI_PREFIX || character == DUAL_PREFIX)
                return true;

            else
                return false;
        }
    }
}
