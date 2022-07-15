using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class Datum
    {
        public string CompanyCode { get; set; }
        public string Identifier { get; set; }
        public string Secret { get; set; }
        public string Lng { get; set; }
    }

    public class Resultlogin
    {
        public string AccessToken { get; set; }
        public bool IsSuccess { get; set; }
        public string StatusMsg { get; set; }
    }

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
}