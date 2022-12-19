namespace EMS
{
    public enum Message_Type : byte
    {
        Text = 0,
        Binary = 1,
        AddressList = 2,
        Invalid = 3,
    }

    public enum Message_Direction : byte
    {
        None,
        In,
        Out
    }
}