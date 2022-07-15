using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuAccount
    {
        public class AccountList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Account> ItemList { get; set; }
        }

        public class Account
        {
            public int FldIntegrationPartyId { get; set; }
            public string FldCustomerCode { get; set; }
            public string FldName { get; set; }
            public string FldContactPerson { get; set; }
            public string FldAddress { get; set; }
            public string FldMobilePhone { get; set; }
            public string FldTelephone{ get; set; }
            public string FldEmail { get; set; }
            public string FldShipToName { get; set; }
            public string FldShipToAddress { get; set; }
            public string FldShipToEmail { get; set; }
            public string FldShipToTel { get; set; }
            public string FldTrn { get; set; }
            public int FldCreditDays { get; set; }
            public decimal FldCreditLimit { get; set; }    
            public int FldCreditCategory { get; set; }
            public string FldPlaceOfSupply { get; set; }
            public int FldBranchId { get; set; }
            public int FldCategoryId  { get; set; }
            public int FldActivityId { get; set; }
            public int FldAdminAreaId { get; set; }
            public int FldWorkingAreaId { get; set; }
            public int FldPriceBookId { get; set; }
            public int FldCurrencyId { get; set; }
            public string FldRouteId { get; set; }
        }

        public class AccountResult
        {
            public ProductResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<ProductFailedList> FailedList { get; set; }
        }
    }
}