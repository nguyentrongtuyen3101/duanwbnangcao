using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace doanwebnangcao.Models
{
    public class BuyNowItem
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string ProductName { get; set; }
        public string VariantImageUrl { get; set; }
        public string SizeName { get; set; }
        public string ColorName { get; set; }
    }
}