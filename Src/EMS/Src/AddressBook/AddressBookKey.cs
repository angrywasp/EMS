namespace EMS
{
    public class AddressBookKey
    {
        private byte[] key;
        private string address;

        public byte[] Key => key;
        public string Address => address;

        public static AddressBookKey Create(byte[] k)
        {
            var b58 = Base58.Encode(k);

            return new AddressBookKey
            {
                key = k,
                address = b58
            };
        }

        public override string ToString() => address;
    }
}