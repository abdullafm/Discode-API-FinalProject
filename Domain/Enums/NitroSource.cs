namespace Domain.Enums
{
    public enum NitroSource
    {
        Stripe = 1,
        Admin = 2,
        // a gift link somebody bought (or a super admin made) and somebody else claimed
        Gift = 3
    }
}
