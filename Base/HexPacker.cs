using System;

namespace Heleus.Base
{
    public static class HexPacker
    {
        public static string ToHex(Action<Packer> action)
        {
            try
            {
                using (var packer = new Packer())
                {
                    action.Invoke(packer);
                    return Hex.ToString(packer.ToByteArray());
                }
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

            return null;
        }

        public static bool FromHex(string hex, Action<Unpacker> action)
        {
            try
            {
                var data = Hex.FromString(hex);
                using (var unpacker = new Unpacker(data))
                {
                    action.Invoke(unpacker);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.IgnoreException(ex);
            }

            return false;
        }
    }
}
