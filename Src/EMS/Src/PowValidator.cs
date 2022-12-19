// Re-implementation of the difficulty target calculation used by Monero
// https://github/com/monero-project/monero

namespace EMS
{
    public static class PowValidator
    {
        public static unsafe bool Validate(byte[] hash, ulong difficulty)
        {
            ulong low = 0, high = 0, top = 0, cur = 0;

            fixed(byte* h = hash)
            {
                ulong* ul = (ulong*)h;
                mul(ul[3], difficulty, ref top, ref high);
                if (high != 0)
                    return false;

                mul(ul[0], difficulty, ref low, ref cur);
                mul(ul[1], difficulty, ref low, ref high);
                bool carry = cadd(cur, low);
                cur = high;
                mul(ul[2], difficulty, ref low, ref high);
                carry = cadc(cur, low, carry);
                carry = cadc(high, top, carry);
                return !carry;
            }
        }

        private static ulong hi_dword(ulong val) => val >> 32;
        private static ulong lo_dword(ulong val) => val & 0xFFFFFFFF;
        private static bool cadd(ulong a, ulong b) => a + b < a;
        private static bool cadc(ulong a, ulong b, bool c) => a + b < a || (c && a + b == ulong.MaxValue);

        private static ulong mul128(ulong multiplier, ulong multiplicand, ref ulong product_hi)
        {
            ulong a = hi_dword(multiplier);
            ulong b = lo_dword(multiplier);
            ulong c = hi_dword(multiplicand);
            ulong d = lo_dword(multiplicand);

            ulong ac = a * c;
            ulong ad = a * d;
            ulong bc = b * c;
            ulong bd = b * d;

            ulong adbc = ad + bc;
            ulong adbc_carry = adbc < ad ? 1ul : 0;

            ulong product_lo = bd + (adbc << 32);
            ulong product_lo_carry = product_lo < bd ? 1ul : 0;
            product_hi = ac + (adbc >> 32) + (adbc_carry << 32) + product_lo_carry;

            return product_lo;
        }

        private static void mul(ulong a, ulong b, ref ulong low, ref ulong high)
        {
            low = mul128(a, b, ref high);
        }
    }
}