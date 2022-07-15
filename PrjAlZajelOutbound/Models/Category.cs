using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{

    public class CategoryList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<Category> ItemList { get; set; }
    }

    public class Category
    {
        public int FldId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
    }

    public class Result
    {
        public ResponseStatus ResponseStatus { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<FailedList> FailedList { get; set; }
    }
    public class ResponseStatus
    {
        public bool IsSuccess { get; set; }
        public string StatusMsg { get; set; }
        public string ErrorCode { get; set; }
    }
    public class FailedList
    {
        public int FldId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
    }

    public class DeleteCategoryList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<DeleteCategory> ItemList { get; set; }
    }

    public class DeleteCategory
    {
        public int FldId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public int FldIsDeleted { get; set; }
    }

    public class DeleteResult
    {
        public DeleteResponseStatus ResponseStatus { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<DeleteFailedList> FailedList { get; set; }
    }
    public class DeleteResponseStatus
    {
        public bool IsSuccess { get; set; }
        public string StatusMsg { get; set; }
        public string ErrorCode { get; set; }
    }
    public class DeleteFailedList
    {
        public int FldId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public int FldIsDeleted { get; set; }
    }

}