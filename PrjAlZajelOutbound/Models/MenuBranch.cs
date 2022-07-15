using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class MenuBranch
    {
        public class BranchList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<Branch> ItemList { get; set; }
        }
        public class Branch
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public string FldAddress { get; set; }
            public string FldTelephone { get; set; }
            public string FldTaxRegisterationNumber { get; set; }
            public string FldJurisdiction { get; set; }
            public long CreatedDate { get; set; }
            public long ModifiedDate { get; set; }
        }
        public class BranchResult
        {
            public BranchResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<BranchFailedList> FailedList { get; set; }
        }
        public class BranchResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class BranchFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public string FldAddress { get; set; }
            public string FldTelephone { get; set; }
            public string FldTaxRegisterationNumber { get; set; }
            public string FldJurisdiction { get; set; }
        }

        public class DeleteBranchList
        {
            public string AccessToken { get; set; }
            public string ObjectType { get; set; }
            public List<DeleteBranch> ItemList { get; set; }
        }

        public class DeleteBranch
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public string FldAddress { get; set; }
            public string FldTelephone { get; set; }
            public string FldTaxRegisterationNumber { get; set; }
            public string FldJurisdiction { get; set; }
            public int FldIsDeleted { get; set; }
        }

        public class DeleteBranchResult
        {
            public DeleteBranchResponseStatus ResponseStatus { get; set; }
            public List<string> ErrorMessages { get; set; }
            public List<DeleteBranchFailedList> FailedList { get; set; }
        }
        public class DeleteBranchResponseStatus
        {
            public bool IsSuccess { get; set; }
            public string StatusMsg { get; set; }
            public string ErrorCode { get; set; }
        }
        public class DeleteBranchFailedList
        {
            public int FldId { get; set; }
            public string FldCode { get; set; }
            public string FldName { get; set; }
            public string FldAddress { get; set; }
            public string FldTelephone { get; set; }
            public string FldTaxRegisterationNumber { get; set; }
            public string FldJurisdiction { get; set; }
            public int FldIsDeleted { get; set; }
        }
    }
}