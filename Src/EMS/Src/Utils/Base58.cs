using System;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;

namespace EMS
{
    public static class Base58
	{
		public const int CheckSumSizeInBytes = 4;

		public static byte[] AddCheckSum(byte[] data)
		{
			byte[] checkSum = GetCheckSum(data);
			byte[] dataWithCheckSum = ConcatArrays(data, checkSum);
			return dataWithCheckSum;
		}

		//Returns null if the checksum is invalid
		public static byte[] VerifyAndRemoveCheckSum(byte[] data)
		{
			byte[] result = SubArray(data, 0, data.Length - CheckSumSizeInBytes);
			byte[] givenCheckSum = SubArray(data, data.Length - CheckSumSizeInBytes);
			byte[] correctCheckSum = GetCheckSum(result);
			if (givenCheckSum.SequenceEqual(correctCheckSum))
				return result;
			else
				return null;
		}

		private const string Digits = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

		public static string Encode(byte[] data)
		{
			// Decode byte[] to BigInteger
			BigInteger intData = 0;
			for (int i = 0; i < data.Length; i++)
			{
				intData = intData * 256 + data[i];
			}

			// Encode BigInteger to Base58 string
			string result = "";
			while (intData > 0)
			{
				int remainder = (int)(intData % 58);
				intData /= 58;
				result = Digits[remainder] + result;
			}

			// Append `1` for each leading 0 byte
			for (int i = 0; i < data.Length && data[i] == 0; i++)
			{
				result = '1' + result;
			}
			return result;
		}

		public static string EncodeWithCheckSum(byte[] data) => Encode(AddCheckSum(data));

		public static bool Decode(string s, out byte[] result)
		{
			// Decode Base58 string to BigInteger 
			BigInteger intData = 0;
			for (int i = 0; i < s.Length; i++)
			{
				int digit = Digits.IndexOf(s[i]); //Slow
				if (digit < 0)
				{
					result = null;
					return false;
				}
				intData = intData * 58 + digit;
			}

			// Encode BigInteger to byte[]
			// Leading zero bytes get encoded as leading `1` characters
			int leadingZeroCount = s.TakeWhile(c => c == '1').Count();
			var leadingZeros = Enumerable.Repeat((byte)0, leadingZeroCount);
			var bytesWithoutLeadingZeros =
				intData.ToByteArray()
				.Reverse()// to big endian
				.SkipWhile(b => b == 0);//strip sign byte
			result = leadingZeros.Concat(bytesWithoutLeadingZeros).ToArray();
			return true;
		}

		public static bool DecodeWithCheckSum(string s, out byte[] result)
		{
			if (!Decode(s, out result))
				return false;

			result = VerifyAndRemoveCheckSum(result);
			if (result == null)
				return false;

			return true;
		}

		private static byte[] GetCheckSum(byte[] data)
		{
			SHA256 sha256 = SHA256.Create();
			byte[] hash1 = sha256.ComputeHash(data);
			byte[] hash2 = sha256.ComputeHash(hash1);

			var result = new byte[CheckSumSizeInBytes];
			Buffer.BlockCopy(hash2, 0, result, 0, result.Length);

			return result;
		}

        private static byte[] ConcatArrays(byte[] a, byte[] b)
		{
			var result = new byte[a.Length + b.Length];
			Buffer.BlockCopy(a, 0, result, 0, a.Length);
			Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
			return result;
		}

		private static byte[] SubArray(byte[] arr, int start, int length)
		{
			var result = new byte[length];
			Buffer.BlockCopy(arr, start, result, 0, length);
			return result;
		}

		private static byte[] SubArray(byte[] arr, int start) => SubArray(arr, start, arr.Length - start);
	}
}