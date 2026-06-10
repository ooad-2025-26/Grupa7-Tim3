namespace ParkirajBa.Models
{
    public class PricingCreateDto
    {
        public int pricingType { get; set; }        // 0, 1, 2... enum value
        public decimal price { get; set; }
        public string validFrom { get; set; }       // optional
    }
}