namespace MapFlux
{
    internal static class TypeCompatibility
    {
        internal static bool IsNumericOrEnum(Type type)
        {
            if (type.IsEnum)
            {
                return true;
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.SByte or TypeCode.Byte or TypeCode.Int16 or TypeCode.UInt16 or
                TypeCode.Int32 or TypeCode.UInt32 or TypeCode.Int64 or TypeCode.UInt64 or
                TypeCode.Single or TypeCode.Double or TypeCode.Decimal => true,
                _ => false
            };
        }
    }
}
