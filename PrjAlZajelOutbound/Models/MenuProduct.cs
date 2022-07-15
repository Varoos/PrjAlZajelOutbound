using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuProduct
    {
        public class ProductList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Product> ItemList { get; set; }
        }

        public class Product
        {
            public int FldProdId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldCategoryId { get; set; }
            public int FldBaseUnitId { get; set; }
            public string FldTaxCategory { get; set; }
            public long CreatedDate { get; set; }
            public long ModifiedDate { get; set; }
            public int FldDisplayUnitId { get; set; }
        }

        public class ProductResult
        {
            public ProductResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<ProductFailedList> FailedList { get; set; }
        }
        public class ProductResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class ProductFailedList
        {
            public int FldProdId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldCategoryId { get; set; }
            public int FldBaseUnitId { get; set; }
            public string FldTaxCategory { get; set; }
        }

        public class DeleteProductList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<DeleteProduct> ItemList { get; set; }
        }

        public class DeleteProduct
        {
            public int FldProdId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldCategoryId { get; set; }
            public int FldBaseUnitId { get; set; }
            public string FldTaxCategory { get; set; }
            public int FldIsDeleted { get; set; }
        }

        public class DeleteProductResult
        {
            public DeleteProductResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<DeleteProductFailedList> FailedList { get; set; }
        }
        public class DeleteProductResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class DeleteProductFailedList
        {
            public int FldProdId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldCategoryId { get; set; }
            public int FldBaseUnitId { get; set; }
            public string FldTaxCategory { get; set; }
            public int FldIsDeleted { get; set; }
        }
    }
}