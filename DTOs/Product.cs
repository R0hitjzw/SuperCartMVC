namespace SuperCartMVC.DTOs;

public class Product : IComparable<Product>
{
    public Market Market { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float Price { get; set; }
    public float? UnitPrice { get; set; }
    public string UnitType { get; set; } = "";
    public string PriceUnitOrKg { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string ProductPrice => $"{Price:F2} €";

    public int CompareTo(Product? other) =>
        other == null ? 0 : Price.CompareTo(other.Price);
}