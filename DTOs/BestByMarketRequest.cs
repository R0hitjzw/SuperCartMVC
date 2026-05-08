namespace SuperCartMVC.DTOs;

public class BestByMarketRequest
{
    public string Term { get; set; } = string.Empty;
    public List<Product> Products { get; set; } = new();
}