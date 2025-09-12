namespace DiscountCodes.Models
{
    public class ShoppingCart
    {
        private List<Product> items = new List<Product>();
        public IReadOnlyList<Product> Items => items.AsReadOnly();
        
        public decimal discount { get; set; }

        public void AddItem(Product product)
        {
            items.Add(product);
        }

        /// <summary>
        /// Returns the total price of all items in the cart, after applying any discounts.
        /// </summary>
        /// <returns></returns>
        public decimal GetTotal()
        {
            return items.Sum(item => item.Price) - discount;
        }

        /// <summary>
        /// Applies a discount code to the shopping cart.
        /// If the code is invalid or empty, no discount is applied.
        /// Only one discount code can be applied at a time, applying a new code replaces the previous one.
        /// </summary>
        /// <param name="code"></param>
        public void ApplyDiscount(string code)
        {
            discount = 0;
            switch (code)
            {
                case "BOGOFREE":
                    var groups = items.GroupBy(p => new { p.Name, p.Brand, p.Price });

                    foreach (var group in groups)
                    {
                        int cantidad = group.Count();
                        int gratis = cantidad / 2;
                        decimal precioProducto = group.First().Price;

                        discount = discount + (gratis * precioProducto);
                    }
                    break;
                case "BRAND2DISCOUNT":
                    foreach (var product in items)
                    {
                        if (product.Brand == "Brand2")
                        {
                            var price = product.Price;
                            price = price*0.10m;
                            discount = discount + price;
                        }
                    }
                    break;
                case "10PERCENTOFF":
                    decimal total = GetTotal();
                    discount = total * 0.10m;
                    break;
                
                case "BRAND1MANIA":
                    foreach (var product in items)
                    {
                        if (product.Brand == "Brand1")
                        {
                            var price = product.Price;
                            price = price/2;
                            discount = discount + price;
                        }
                    }  
                    break;
                
                case "5USDOFF":
                    decimal totalCompra = GetTotal();
                    if (totalCompra > 4)
                    {
                        discount = 5;
                    }
                    else discount = 0;
                    break;
            }
        }
    }
}
