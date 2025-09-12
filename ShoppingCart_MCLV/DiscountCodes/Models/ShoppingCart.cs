namespace DiscountCodes.Models
{
    public class ShoppingCart
    {
        private List<Product> items = new List<Product>();
        public IReadOnlyList<Product> Items => items.AsReadOnly();

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
            return items.Sum(item => item.Price);
        }

        /// <summary>
        /// Applies a discount code to the shopping cart.
        /// If the code is invalid or empty, no discount is applied.
        /// Only one discount code can be applied at a time, applying a new code replaces the previous one.
        /// </summary>
        /// <param name="code"></param>
        public void ApplyDiscount(string code)
        {
            
            switch (code)
            {
                case "BOGOFREE":
                    foreach (var product in items)
                    {
                        if (items.Count < 1)
                            GetTotal();
                    }
                    break;
                case "BRAND2DISCOUNT":
                    foreach (var product in items)
                    {
                        if (product.Brand == "Brand2")
                        {
                            var price = product.Price;
                            decimal precioTotal;
                            price = price * (1/100);
                            GetTotal();
                        }
                    }
                    break;
                case "10PERCENTOFF":
                    decimal total = GetTotal();
                    total = total * (1/100);
                    break;
                case "BRAND1MANIA":
                    foreach (var product in items)
                    {
                        if (product.Brand == "Brand1")
                        {
                            var price = product.Price;
                            price = product.Price/2;
                        }
                    }  
                    break;
                case "5USDOFF":
                    ApplyDiscount("5USDOFF");
                    break;
            }
        }
    }
}
