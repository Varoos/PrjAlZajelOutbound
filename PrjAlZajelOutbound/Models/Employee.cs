using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class EmployeeList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<Employee> ItemList { get; set; }
    }
    public class Employee
    {
        public int FldRouteId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public string FldNationality { get; set; }
        public string FldAddress { get; set; }
        public string FldMobilePhone { get; set; }
        public string FldEmploymentDate { get; set; }
        public int FldBranchId { get; set; }
    }
    public class EmployeeResult
    {
        public EmployeeResponseStatus ResponseStatus { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<EmployeeFailedList> FailedList { get; set; }
    }
    public class EmployeeResponseStatus
    {
        public bool IsSuccess { get; set; }
        public string StatusMsg { get; set; }
        public string ErrorCode { get; set; }
    }
    public class EmployeeFailedList
    {
        public int FldRouteId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public string FldNationality { get; set; }
        public string FldAddress { get; set; }
        public string FldMobilePhone { get; set; }
        public string FldEmploymentDate { get; set; }
        public int FldBranchId { get; set; }
    }


    public class DeleteEmployeeList
    {
        public string AccessToken { get; set; }
        public string ObjectType { get; set; }
        public List<DeleteEmployee> ItemList { get; set; }
    }

    public class DeleteEmployee
    {
        public int FldRouteId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public string FldNationality { get; set; }
        public string FldAddress { get; set; }
        public string FldMobilePhone { get; set; }
        public string FldEmploymentDate { get; set; }
        public int FldBranchId { get; set; }
        public int FldIsDeleted { get; set; }
    }

    public class DeleteEmployeeResult
    {
        public DeleteEmployeeResponseStatus ResponseStatus { get; set; }
        public List<string> ErrorMessages { get; set; }
        public List<DeleteEmployeeFailedList> FailedList { get; set; }
    }
    public class DeleteEmployeeResponseStatus
    {
        public bool IsSuccess { get; set; }
        public string StatusMsg { get; set; }
        public string ErrorCode { get; set; }
    }
    public class DeleteEmployeeFailedList
    {
        public int FldRouteId { get; set; }
        public string FldCode { get; set; }
        public string FldName { get; set; }
        public string FldNationality { get; set; }
        public string FldAddress { get; set; }
        public string FldMobilePhone { get; set; }
        public string FldEmploymentDate { get; set; }
        public int FldBranchId { get; set; }
        public int FldIsDeleted { get; set; }
    }
}