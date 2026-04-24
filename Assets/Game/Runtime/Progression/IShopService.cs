namespace FrontierDepths.Progression
{
    public interface IShopService
    {
        bool TryExecuteOffer(ShopDefinition shop, int index, out string message);
    }
}
