using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuCurrency
    {
        public class CurrencyList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Currency> ItemList { get; set; }
        }

        public class Currency
        {
            public int FldId { get; set; }
            public string FldSymbol { get; set; }
            public string FldName { get; set; }
            public decimal FldExchangeRate { get; set; }
            public int FldPlaces { get; set; }
        }

        public class CurrencyResult
        {
            public CurrencyResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<CurrencyFailedList> FailedList { get; set; }
        }
        public class CurrencyResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class CurrencyFailedList
        {
            public int FldId { get; set; }
            public string FldSymbol { get; set; }
            public string FldName { get; set; }
            public decimal FldExchangeRate { get; set; }
            public int FldPlaces { get; set; }
        }
    }
}