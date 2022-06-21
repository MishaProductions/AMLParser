namespace ACPILibs.AML
{
	public class Definitions
	{
		public const int NameSize = 4;

		public const byte ExtendedOpCodePrefix = 0x5B;

		public const byte DualNamePrefix = (byte)OpCodeEnum.DualNamePrefix;
		public const byte MultiNamePrefix = (byte)OpCodeEnum.MultiNamePrefix;

		public static bool IsNameRootPrefixOrParentPrefix(byte b)
		{
			return (b == 0x5C || b == 0x5E);
		}

		public static bool IsLeadingChar(byte b)
		{
			return (b == '_' || (b >= 'A' && b <= 'Z'));
		}

		public static bool IsName(byte character)
        {
			if ((character >= '0' && character <= '9') || (character >= 'A' && character <= 'Z')
	|| character == '_' || character == 0x5C || character == 0x5E
	|| character == 0x2F || character == 0x2E)
            {
				return true;
			}

			else
				return false;
		}
	}
}
