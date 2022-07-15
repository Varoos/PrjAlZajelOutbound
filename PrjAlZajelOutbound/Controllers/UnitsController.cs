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
    public class UnitsController : Controller
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

        // GET: Units
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
                    objreg.SetLog("AlZajelUnits.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelUnits.log", sMessage);
                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                UnitList clist = new UnitList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "unit";

                string sql = $@"select iMasterId,sName,sCode,iCreatedDate,iModifiedDate,bGroup from mCore_Units where iMasterId<>0 and iStatus<>5 and sName='{Name}' and sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Units> lc = new List<Units>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Units c = new Units();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
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
                        objreg.SetLog("AlZajelUnits.log", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<UnitResult>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            int res = 0;
                            if (CreatedDate == ModifiedDate)
                            {
                                string UpSql = $@"update muCore_Units set Posted=1,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            else
                            {
                                string UpSql = $@"update muCore_Units set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            if (res == 1)
                            {
                                objreg.SetLog("AlZajelUnits.log", "Data posted / updated to mobile app device");
                                //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                            }
                            Message = "Data Posted Successful";
                        }
                        else
                        {
                            var ErrorMessagesList = lng.ErrorMessages.ToList();
                            foreach (var item in ErrorMessagesList)
                            {
                                objreg.SetLog("AlZajelUnits.log", "Error Message for Unit Master:" + item);
                                //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Error Message for Unit Master:" + item);

                                string UpSql = $@"update muCore_Units set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldId}";
                                int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                if (res == 1)
                                {
                                    objreg.SetLog("AlZajelUnits.log", "Data posted / updated to mobile app device");
                                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                                }
                                Message = item;
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetLog("AlZajelUnits.log", "FailedList Message for Unit Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "FailedList Message for Unit Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                    var FieldMasterId = item.FldId;
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
                objreg.SetLog("AlZajelUnits.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " Error :" + errors1);
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
                    objreg.SetLog("AlZajelUnits.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelUnits.log", sMessage);
                    //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                DeleteUnitList clist = new DeleteUnitList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "unit";

                string sql = $@"select iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_Units where iMasterId<>0 and sName='{Name}' and sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, companyId, ref errors1);
                List<DeleteUnit> lc = new List<DeleteUnit>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DeleteUnit c = new DeleteUnit();
                    c.FldId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
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
                    objreg.SetLog("AlZajelUnits.log", "Delete PostingURL :" + PostingURL);
                    var arrResponse = client.UploadString(PostingURL, sContent);
                    var lng = JsonConvert.DeserializeObject<DeleteUnitResult>(arrResponse);

                    if (lng.ResponseStatus.IsSuccess == true)
                    {
                        Message = lng.ResponseStatus.StatusMsg;
                        ResponseStatus = lng.ResponseStatus.IsSuccess;
                        objreg.SetLog("AlZajelUnits.log", "Delete Response" + Message);
                        //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Delete Response" + Message);
                    }
                    else
                    {
                        objreg.SetLog("AlZajelUnits.log", "Delete Failed Response" + lng.ResponseStatus.StatusMsg);
                        //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Delete Failed Response" + lng.ResponseStatus.StatusMsg);

                        var ErrorMessagesList = lng.ErrorMessages.ToList();
                        foreach (var item in ErrorMessagesList)
                        {
                            Message = item;
                            objreg.SetLog("AlZajelUnits.log", "Error Message for Unit Master:" + item);
                            //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "Error Message for Unit Master:" + item);
                        }

                        int FailedListCount = lng.FailedList.Count();
                        if (FailedListCount > 0)
                        {
                            var FailedList = lng.FailedList.ToList();
                            foreach (var item in FailedList)
                            {
                                objreg.SetLog("AlZajelUnits.log", "FailedList Message for Unit Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + "FailedList Message for Unit Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                var FieldMasterId = item.FldId;
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
                objreg.SetLog("AlZajelUnits.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelUnits.log", DateTime.Now.ToString() + " Error :" + errors1);
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
                objreg.SetLog("AlZajelUnits.log", " AccessTokenURL :" + AccessTokenURL);
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