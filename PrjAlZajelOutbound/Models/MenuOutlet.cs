using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuOutlet
    {
        public class OutletList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Outlet> ItemList { get; set; }
        }

        public class Outlet
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldBranchId { get; set; }
            public int FldType { get; set; }
            public string FldPlateNumber { get; set; }
            public string FldRegistrationDate { get; set; }
            public int FldRouteId { get; set; }
            public long CreatedDate { get; set; }
            public long ModifiedDate { get; set; }
        }

        public class OutletResult
        {
            public OutletResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<OutletFailedList> FailedList { get; set; }
        }
        public class OutletResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class OutletFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldBranchId { get; set; }
            public int FldType { get; set; }
            public string FldPlateNumber { get; set; }
            public string FldRegistrationDate { get; set; }
            public int FldRouteId { get; set; }
        }

        public class DeleteOutletList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<DeleteOutlet> ItemList { get; set; }
        }

        public class DeleteOutlet
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldBranchId { get; set; }
            public int FldType { get; set; }
            public string FldPlateNumber { get; set; }
            public string FldRegistrationDate { get; set; }
            public int FldRouteId { get; set; }
            public int FldIsDeleted { get; set; }
        }

        public class DeleteOutletResult
        {
            public DeleteOutletResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<DeleteOutletFailedList> FailedList { get; set; }
        }
        public class DeleteOutletResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class DeleteOutletFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldBranchId { get; set; }
            public int FldType { get; set; }
            public string FldPlateNumber { get; set; }
            public string FldRegistrationDate { get; set; }
            public int FldRouteId { get; set; }
            public int FldIsDeleted { get; set; }
        }
    }
}