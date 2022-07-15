using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuUnitConversion
    {
        public class UnitConversionList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<UnitConversion> ItemList { get; set; }
        }

        public class UnitConversion
        {
            public int FldId { get; set; }
            public int FldProductId { get; set; }
            public int FldProductUnitId { get; set; }
            public decimal FldConversionQty { get; set; }
        }

        public class UnitConversionResult
        {
            public UnitConversionResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<UnitConversionFailedList> FailedList { get; set; }
        }
        public class UnitConversionResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class UnitConversionFailedList
        {
            public int FldId { get; set; }
            public int FldProductId { get; set; }
            public int FldProductUnitId { get; set; }
            public decimal FldConversionQty { get; set; }
        }
    }
}