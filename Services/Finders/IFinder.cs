// Services/Finders/IFinder.cs
namespace SuperCartMVC.Services.Finders;

public interface IFinder
{
    Task<List<DTOs.Product>> FindProductsByTermAsync(string term);
    DTOs.Market GetMarket();
}