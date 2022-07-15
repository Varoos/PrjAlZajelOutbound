using Focus.Common.DataStructs;
using Newtonsoft.Json;
using PrjAlZajelOutbound.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace PrjAlZajelOutbound.Controllers
{
    public class EmployeeController : Controller
    {
        string errors1 = "";
        string Message = "";
        string AccessToken = "";
        string AccessTokenURL = ConfigurationManager.AppSettings["AccessTokenURL"];
        string PostingURL = ConfigurationManager.AppSettings["PostingURL"];
        string serverip = ConfigurationManager.AppSettings["ServerIP"];
        string serveripp = ConfigurationManager.AppSettings["ServerIPP"];
        string CompanyCode = ConfigurationManager.AppSettings["CompanyCode"];
        string Identifier = ConfigurationManager.AppSettings["Identifier"];
        string Secret = ConfigurationManager.AppSettings["Secret"];
        string Lng = ConfigurationManager.AppSettings["Lng"];
        BL_Registry objreg = new BL_Registry();
        // GET: Employee
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult MasterPosting(int CompanyId, string SessionId, string Name, string Code)
        {
            string Message = "";
            int isGroup = 0;
            long CreatedDate = 0;
            long ModifiedDate = 0;
            int strdate = GetDateToInt(DateTime.Now);

            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("AlZajelEmployee.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelEmployee.log", sMessage);
                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                EmployeeList clist = new EmployeeList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "salesman";

                string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate ,e.bGroup
                                from mPay_Employee e
                                join muPay_Employee meg on e.iMasterId=meg.iMasterId
                                join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
                                join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
                                join mpay_Nationality n on n.iMasterId=me.iNationality
                                where e.iMasterId>0 and e.iStatus<>5 and  e.sName='{Name}' and e.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Employee> lc = new List<Employee>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Employee c = new Employee();
                    c.FldRouteId = Convert.ToInt32(row["iMasterId"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldNationality = Convert.ToString(row["Nationality"]);
                    c.FldAddress = Convert.ToString(row["sAddress"]);
                    c.FldMobilePhone = Convert.ToString(row["sMobileNumber"]);
                    c.FldEmploymentDate = Convert.ToString(row["dDateofJoining"]);
                    c.FldBranchId = Convert.ToInt32(row["BranchId"]);
                    isGroup = Convert.ToInt32(row["bGroup"]);
                    CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                    ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                    lc.Add(c);
                }
                clist.ItemList = lc;
                if (isGroup == 0)
                {
                    var sContent = new JavaScriptSerializer().Serialize(clist);
                    using (WebClient client = new WebClient())
                    {
                        client.Headers.Add("Content-Type", "application/json");
                        objreg.SetLog("AlZajelEmployee.log", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<EmployeeResult>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            int res = 0;
                            if (CreatedDate == ModifiedDate)
                            {
                                string UpSql = $@"update muPay_Employee,PostedDate={strdate} set Posted=1 where iMasterId={clist.ItemList.First().FldRouteId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            else
                            {
                                string UpSql = $@"update muPay_Employee set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldRouteId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            if (res == 1)
                            {
                                objreg.SetLog("AlZajelEmployee.log", "Data posted / updated to mobile app device");
                                //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                            }
                            Message = "Data Posted Successful";
                        }
                        else
                        {
                            var ErrorMessagesList = lng.ErrorMessages.ToList();
                            foreach (var item in ErrorMessagesList)
                            {
                                objreg.SetLog("AlZajelEmployee.log", "Error Message for Employee Master:" + item);
                                //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Error Message for Employee Master:" + item);

                                string UpSql = $@"update muPay_Employee set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldRouteId}";
                                int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                if (res == 1)
                                {
                                    objreg.SetLog("AlZajelEmployee.log", "Data posted / updated to mobile app device");
                                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                                }

                                Message = item;
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetLog("AlZajelEmployee.log", "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
                                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
                                    var FieldMasterId = item.FldRouteId;
                                }
                            }
                        }
                    }
                }
                else
                {
                    Message = "Data is Group";
                }
                return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetLog("AlZajelEmployee.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " Error :" + errors1);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult MasterDelete(int companyId, string Name, string Code)
        {
            string Message = "";
            bool ResponseStatus = false;
            long CreatedDate = 0;
            long ModifiedDate = 0;

            try
            {
                AccessToken = GetAccessToken();
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetLog("AlZajelEmployee.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelEmployee.log", sMessage);
                    //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                DeleteEmployeeList clist = new DeleteEmployeeList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "salesman";

                string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate 
                                from mPay_Employee e
                                join muPay_Employee meg on e.iMasterId=meg.iMasterId
                                join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
                                join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
                                join mpay_Nationality n on n.iMasterId=me.iNationality
                                where e.iMasterId>0 and  e.sName='{Name}' and e.sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, companyId, ref errors1);
                List<DeleteEmployee> lc = new List<DeleteEmployee>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DeleteEmployee c = new DeleteEmployee();
                    c.FldRouteId = Convert.ToInt32(row["iMasterId"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldNationality = Convert.ToString(row["Nationality"]);
                    c.FldAddress = Convert.ToString(row["sAddress"]);
                    c.FldMobilePhone = Convert.ToString(row["sMobileNumber"]);
                    c.FldEmploymentDate = Convert.ToString(row["dDateofJoining"]);
                    c.FldBranchId = Convert.ToInt32(row["BranchId"]);
                    c.FldIsDeleted = 1;
                    CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                    ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                    lc.Add(c);
                }
                clist.ItemList = lc;

                var sContent = new JavaScriptSerializer().Serialize(clist);
                using (WebClient client = new WebClient())
                {
                    client.Headers.Add("Content-Type", "application/json");
                    objreg.SetLog("AlZajelEmployee.log", "Delete PostingURL :" + PostingURL);
                    var arrResponse = client.UploadString(PostingURL, sContent);
                    var lng = JsonConvert.DeserializeObject<DeleteEmployeeResult>(arrResponse);

                    if (lng.ResponseStatus.IsSuccess == true)
                    {
                        Message = lng.ResponseStatus.StatusMsg;
                        ResponseStatus = lng.ResponseStatus.IsSuccess;
                        objreg.SetLog("AlZajelEmployee.log", "Delete Response" + Message);
                        //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Delete Response" + Message);
                    }
                    else
                    {
                        objreg.SetLog("AlZajelEmployee.log", "Delete Failed Response" + lng.ResponseStatus.StatusMsg);
                        //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Delete Failed Response" + lng.ResponseStatus.StatusMsg);

                        var ErrorMessagesList = lng.ErrorMessages.ToList();
                        foreach (var item in ErrorMessagesList)
                        {
                            objreg.SetLog("AlZajelEmployee.log", "Error Message for Employee Master:" + item);
                            //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "Error Message for Employee Master:" + item);
                            Message = item;
                        }

                        int FailedListCount = lng.FailedList.Count();
                        if (FailedListCount > 0)
                        {
                            var FailedList = lng.FailedList.ToList();
                            foreach (var item in FailedList)
                            {
                                objreg.SetLog("AlZajelEmployee.log", "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
                                //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
                                var FieldMasterId = item.FldRouteId;
                            }
                        }
                    }
                }

                return Json(new { Message = Message, ResponseStatus = ResponseStatus }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception e)
            {
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetLog("AlZajelEmployee.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelEmployee.log", DateTime.Now.ToString() + " Error :" + errors1);
            }
            return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
        }

        public string GetAccessToken()
        {
            string AccessToken = "";
            Datum datanum = new Datum();
            datanum.CompanyCode = CompanyCode;
            datanum.Identifier = Identifier;
            datanum.Secret = Secret;
            datanum.Lng = Lng;
            string sContent = JsonConvert.SerializeObject(datanum);
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("Content-Type", "application/json");
                objreg.SetLog("AlZajelEmployee.log", " AccessTokenURL :" + AccessTokenURL);
                var arrResponse = client.UploadString(AccessTokenURL, sContent);
                Resultlogin lng = JsonConvert.DeserializeObject<Resultlogin>(arrResponse);

                AccessToken = lng.AccessToken;
                if (lng.AccessToken == null || lng.AccessToken == "" || lng.AccessToken == "-1")
                {
                    return AccessToken;
                }
                else
                {
                }
            }

            return AccessToken;
        }

        public static int GetDateToInt(DateTime dt)
        {
            int val;
            val = Convert.ToInt16(dt.Year) * 65536 + Convert.ToInt16(dt.Month) * 256 + Convert.ToInt16(dt.Day);
            return val;
        }

        public Int64 GetDateTimetoInt(DateTime dt)
        {
            Int64 val;
            val = Convert.ToInt64(dt.Year) * 8589934592 + Convert.ToInt64(dt.Month) * 33554432 + Convert.ToInt64(dt.Day) * 131072 + Convert.ToInt64(dt.Hour) * 4096 + Convert.ToInt64(dt.Minute) * 64 + Convert.ToInt64(dt.Second);
            return val;
        }
    }
}