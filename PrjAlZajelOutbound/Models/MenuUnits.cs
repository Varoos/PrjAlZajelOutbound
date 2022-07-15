using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuUnits
    {
        public class UnitList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Units> ItemList { get; set; }
        }

        public class Units
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public long CreatedDate { get; set; }
            public long ModifiedDate { get; set; }
        }

        public class UnitResult
        {
            public UnitResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<UnitFailedList> FailedList { get; set; }
        }
        public class UnitResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class UnitFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
        }


        public class DeleteUnitList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<DeleteUnit> ItemList { get; set; }
        }

        public class DeleteUnit
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldIsDeleted { get; set; }
        }

        public class DeleteUnitResult
        {
            public DeleteUnitResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<DeleteUnitFailedList> FailedList { get; set; }
        }
        public class DeleteUnitResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class DeleteUnitFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public int FldIsDeleted { get; set; }
        }
    }
}