namespace EMS
{
    public class DecryptionData
    {
        private byte[] data = null;
        private int keyIndex = -1;
        private string address = string.Empty;

        public byte[] Data => data;

        public int KeyIndex => keyIndex;

        public string Address => address;

        public DecryptionData(byte[] data, int keyIndex)
        {
            this.data = data;
            this.keyIndex = keyIndex;
            this.address = keyIndex == -1 ? string.Empty: KeyRing.Keys[keyIndex].Base58Address;
        }
    }
}