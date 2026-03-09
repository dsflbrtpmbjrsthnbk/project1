namespace UserManagementApp.Models
{
    public enum IdElementType 
    { 
        FixedText, 
        Random20Bit, 
        Random32Bit, 
        Random6Digit, 
        Random9Digit, 
        GUID, 
        DateTime, 
        Sequence 
    }

    public class IdElement 
    {
        public IdElementType Type { get; set; }
        public string? Value { get; set; }
        public string? Format { get; set; }
    }
}
