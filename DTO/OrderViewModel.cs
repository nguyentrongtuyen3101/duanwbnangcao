using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

using doanwebnangcao.Models;

namespace doanwebnangcao.DTO
{
    public class OrderViewModel
    {
        public Address Address { get; set; }
        public Product Product { get; set; }
        public ProductVariant Variant { get; set; }
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
        public string OrderNote { get; set; }
    }
}