using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AngryWasp.Cryptography;
using AngryWasp.Helpers;
using Newtonsoft.Json;

namespace EMS
{
    public class HashKey16JsonConverer : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(HashKey16);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            HashKey16 hk = ((string)reader.Value).FromByteHex();
            return hk;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            HashKey16 hk = (HashKey16)value;
            writer.WriteValue(hk.ToString());
        }
    }

    [JsonConverter(typeof(HashKey16JsonConverer))]
    public struct HashKey16 : IReadOnlyList<byte>, IEquatable<HashKey16>, IEquatable<byte[]>
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

        public int Count => 16;

        public static readonly HashKey16 Empty = new byte[16] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };

        public bool IsNullOrEmpty()
        {
            if (value == null)
                return true;

            if (value.SequenceEqual(Empty))
                return true;

            return false;
        }

        public HashKey16(byte[] bytes)
        {
            value = bytes;
        }

        public static HashKey16 Make(byte[] input) => Keccak.Hash128(input);
        public static HashKey16 Make(IEnumerable<byte> input) => Keccak.Hash128(input.ToArray());

        public bool Equals(HashKey16 other) => this.SequenceEqual(other);

        public bool Equals(byte[] other) => this.SequenceEqual(other);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            return obj is HashKey16 && this.Equals((HashKey16)obj);
        }

        public IEnumerator<byte> GetEnumerator()
        {
            if (this.value != null)
                return ((IList<byte>)this.value).GetEnumerator();

            return Enumerable.Repeat(default(byte), 16).GetEnumerator();
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
                BitShifter.ToInt(this.value, ref offset);
        }

        public static bool operator ==(HashKey16 left, HashKey16 right) => left.Equals(right);

        public static bool operator !=(HashKey16 left, HashKey16 right) => !left.Equals(right);

        public static bool operator ==(byte[] left, HashKey16 right) => right.Equals(left);

        public static bool operator !=(byte[] left, HashKey16 right) => !right.Equals(left);

        public static bool operator ==(HashKey16 left, byte[] right) => left.Equals(right);

        public static bool operator !=(HashKey16 left, byte[] right) => !left.Equals(right);

        public static implicit operator HashKey16(byte[] value) => new HashKey16(value);

        public static implicit operator byte[](HashKey16 value) => value.ToByte();

        public static implicit operator List<byte>(HashKey16 value) => value.ToList();

        public static implicit operator HashKey16(List<byte> value) => new HashKey16(value.ToArray());

        public static implicit operator HashKey16(string hex) => new HashKey16(hex.FromByteHex());

        public override string ToString() => value.ToHex();

        public byte[] ToByte() => value;
    }
}