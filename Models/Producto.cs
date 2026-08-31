namespace AppSoftConsola.Models
{
    public class Producto
    {
        public int Code { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public int OrderShow { get; set; }

        public Producto(int code, string desc, decimal amount, int orderShow = 0)
        {
            Code = code;
            Description = desc;
            Amount = amount;
            OrderShow = orderShow;
        }
    }
}