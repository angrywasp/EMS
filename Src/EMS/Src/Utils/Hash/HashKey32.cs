using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using Newtonsoft.Json;

namespace EMS
{
    public class HashKey32JsonConverer : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(HashKey32);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HashKey32 hk = ((string)reader.Value).FromByteHex();
            return hk;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            HashKey32 hk = (HashKey32)value;
            writer.WriteValue(hk.ToString());
        }
    }

    [JsonConverter(typeof(HashKey32JsonConverer))]
    public struct HashKey32 : IReadOnlyList<byte>, IEquatable<HashKey32>, IEquatable<byte[]>
    {
        private readonly byte[] value;

        public byte this[int index]
        {
            get
            {
                if (this.value != null)
                    return this.value[index];

                return default(byte);
            }
        }

        public int Count => 32;

        public static readonly HashKey32 Empty = new byte[32]
        {
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        public bool IsNullOrEmpty()
        {
            if (value == null)
                return true;

            if (value.SequenceEqual(Empty))
                return true;

            return false;
        }

        public HashKey32(byte[] bytes)
        {
            value = bytes;
        }

        public static HashKey32 Make(byte[] input) => Keccak.Hash256(input);
        public static HashKey32 Make(IEnumerable<byte> input) => Keccak.Hash256(input.ToArray());

        public bool Equals(HashKey32 other) => this.SequenceEqual(other);

        public bool Equals(byte[] other) => this.SequenceEqual(other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is HashKey32 && this.Equals((HashKey32)obj);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            if (this.value != null)
                return ((IList<byte>)this.value).GetEnumerator();

            return Enumerable.Repeat(default(byte), 32).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public override int GetHashCode()
        {
            if (this.value == null)
                return 0;

            int offset = 0;
            return 
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset) ^
                BitShifter.ToInt(this.value, ref offset);
        }

        public static bool operator ==(HashKey32 left, HashKey32 right) => left.Equals(right);

        public static bool operator !=(HashKey32 left, HashKey32 right) => !left.Equals(right);

        public static bool operator ==(byte[] left, HashKey32 right) => right.Equals(left);

        public static bool operator !=(byte[] left, HashKey32 right) => !right.Equals(left);

        public static bool operator ==(HashKey32 left, byte[] right) => left.Equals(right);

        public static bool operator !=(HashKey32 left, byte[] right) => !left.Equals(right);

        public static implicit operator HashKey32(byte[] value) => new HashKey32(value);

        public static implicit operator byte[](HashKey32 value) => value.ToByte();

        public static implicit operator List<byte>(HashKey32 value) => value.ToList();

        public static implicit operator HashKey32(List<byte> value) => new HashKey32(value.ToArray());

        public static implicit operator HashKey32(string hex) => new HashKey32(hex.FromByteHex());
        
        public override string ToString() => value.ToHex();

        public byte[] ToByte() => value;
    }
}