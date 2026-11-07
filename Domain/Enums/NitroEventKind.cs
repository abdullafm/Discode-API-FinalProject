namespace Domain.Enums
{
    public enum NitroEventKind
    {
        Subscribed = 1,
        Renewed = 2,
        Canceled = 3,
        Resumed = 4,
        Expired = 5,
        PaymentFailed = 6,
        GrantedByAdmin = 7,
        RevokedByAdmin = 8
    }
}
