using Focus.Common.DataStructs;
using Newtonsoft.Json;
using PrjAlZajelOutbound.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace PrjAlZajelOutbound.Controllers
{
    public class MenuItemController : Controller
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
        // GET: MenuItem
        public ActionResult Index(int CompanyId)
        {
            ViewBag.CompId = CompanyId;
            return View();
        }

        public ActionResult CategoryPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuCategory.CategoryList clist = new MenuCategory.CategoryList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "category";

//                string sql = $@"select ic.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_ItemCategory ic join muCore_ItemCategory muic on ic.iMasterId=muic.iMasterId where ic.iMasterId<>0 and iStatus<>5 and bGroup=0 and muic.Posted=0";
                 string sql = $@"select ic.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_ItemCategory ic join muCore_ItemCategory muic on ic.iMasterId=muic.iMasterId where ic.iMasterId<>0 and iStatus<>5 and bGroup=0";

                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuCategory.Category> lc = new List<MenuCategory.Category>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuCategory.Category c = new MenuCategory.Category();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;

                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("AlZajelMenuCategory.log", "New Posted sContent: " + sContent);
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuCategory.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuCategory.Result>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_ItemCategory set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_ItemCategory set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        objreg.SetSuccessLog("AlZajelMenuCategory.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "ErrorList Message for Category Master with Posted status as 0 :- " + item);
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + "ErrorList Message for Category Master with Posted status as 0 :- " + item + " \n ");
                                        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Category Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + "FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muCore_ItemCategory set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId);
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + "FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }

                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", " Error 1 :" + errors1);
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuCategory.DeleteCategoryList clist = new MenuCategory.DeleteCategoryList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "category";

            //    string sql = $@"select ic.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_ItemCategory ic join muCore_ItemCategory muic on ic.iMasterId=muic.iMasterId where ic.iMasterId<>0 and iStatus=5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuCategory.DeleteCategory> dlc = new List<MenuCategory.DeleteCategory>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuCategory.DeleteCategory dc = new MenuCategory.DeleteCategory();
            //            dc.FldId = Convert.ToInt32(row["iMasterId"]);
            //            dc.FldName = Convert.ToString(row["sName"]);
            //            dc.FldCode = Convert.ToString(row["sCode"]);
            //            dc.FldIsDeleted = 1;
            //            dlc.Add(dc);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                objreg.SetSuccessLog("AlZajelMenuCategory.log", "Deletion sContent: " + sContent);
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + " Deletion sContent: " + sContent + " \n ");
            //                //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuCategory.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuCategory.DeleteResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    objreg.SetSuccessLog("AlZajelMenuCategory.log", "Data posted / updated to mobile app device for Deleted Master");
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_ItemCategory set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        objreg.SetSuccessLog("AlZajelMenuCategory.log", "Data posted / updated to mobile app device for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + "Data posted / updated to mobile app device for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                        //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "ErrorList Message for Category Deleted Master:- " + item);
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + "ErrorList Message for Category Deleted Master:- " + item + " \n ");
            //                            //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Category Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "FailedList Message for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + "FailedList Message for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Category Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "Error 2 :" + errors1);
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + "Error 2 :" + errors1 + " \n ");
            //    //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuCategory.CategoryList clist = new MenuCategory.CategoryList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "category";

            //    string sql = $@"select ic.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_ItemCategory ic join muCore_ItemCategory muic on ic.iMasterId=muic.iMasterId where ic.iMasterId<>0 and iStatus<>5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuCategory.Category> lc = new List<MenuCategory.Category>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuCategory.Category c = new MenuCategory.Category();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                objreg.SetSuccessLog("AlZajelMenuCategory.log", "Other than New and Deleted sContent: " + sContent);
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                // FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                   objreg.SetLog("AlZajelMenuCategory.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuCategory.Result>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_ItemCategory set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_ItemCategory set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            objreg.SetSuccessLog("AlZajelMenuCategory.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            //FConvert.LogFile("AlZajelMenuCategory.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "ErrorList Message for Category Master Other than New and Deleted :- " + item);
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Category Master Other than New and Deleted :- " + item + " \n ");
            //                            // FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Category Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Category Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muCore_ItemCategory set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", "FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Category Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }

            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    objreg.SetErrorLog("AlZajelMenuCategoryFailed.log", " Error 3 :" + errors1);
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    //FConvert.LogFile("AlZajelMenuCategoryFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
           
            
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UnitPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuUnits.UnitList clist = new MenuUnits.UnitList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "unit";

//                string sql = $@"select u.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_Units u join muCore_Units mu on u.iMasterId=mu.iMasterId where u.iMasterId<>0 and iStatus<>5 and bGroup=0 and mu.Posted=0";
                string sql = $@"select u.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_Units u join muCore_Units mu on u.iMasterId=mu.iMasterId where u.iMasterId<>0 and iStatus<>5 and bGroup=0";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuUnits.Units> lc = new List<MenuUnits.Units>();

                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuUnits.Units c = new MenuUnits.Units();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("AlZajelMenuUnits.log", "New Posted sContent: " + sContent);
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuUnits.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuUnits.UnitResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_Units set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_Units set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        objreg.SetSuccessLog("AlZajelMenuUnits.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Units Master with Posted status as 0 :- " + item + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "ErrorList Message for Units Master with Posted status as 0 :- " + item);
                                        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Units Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muCore_Units set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId);
                                            //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", " Error 1 :" + errors1);
                //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuUnits.DeleteUnitList clist = new MenuUnits.DeleteUnitList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "unit";

            //    string sql = $@"select u.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_Units u join muCore_Units mu on u.iMasterId=mu.iMasterId where u.iMasterId<>0 and iStatus=5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuUnits.DeleteUnit> dlc = new List<MenuUnits.DeleteUnit>();

            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuUnits.DeleteUnit dc = new MenuUnits.DeleteUnit();
            //            dc.FldId = Convert.ToInt32(row["iMasterId"]);
            //            dc.FldCode = Convert.ToString(row["sCode"]);
            //            dc.FldName = Convert.ToString(row["sName"]);
            //            dc.FldIsDeleted = 1;
            //            dlc.Add(dc);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuUnits.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuUnits.DeleteUnitResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    objreg.SetSuccessLog("AlZajelMenuUnits.log", "Data posted / updated to mobile app device for Deleted Master");
            //                    //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_Units set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                        objreg.SetSuccessLog("AlZajelMenuUnits.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Units Deleted Master:- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "ErrorList Message for Units Deleted Master:- " + item);
            //                            //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Units Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Units Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "FailedList Message for Units Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Units Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", " Error 2 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuUnits.UnitList clist = new MenuUnits.UnitList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "unit";

            //    string sql = $@"select u.iMasterId,sName,sCode,iCreatedDate,iModifiedDate from mCore_Units u join muCore_Units mu on u.iMasterId=mu.iMasterId where u.iMasterId<>0 and iStatus<>5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuUnits.Units> lc = new List<MenuUnits.Units>();

            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuUnits.Units c = new MenuUnits.Units();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuUnits.log", "Other than New and Deleted sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuUnits.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuUnits.UnitResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_Units set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_Units set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            objreg.SetSuccessLog("AlZajelMenuUnits.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuUnits.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Units Master Other than New and Deleted :- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "ErrorList Message for Units Master Other than New and Deleted :- " + item);
            //                            //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Units Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Units Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muCore_Units set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", "FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                                //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Units Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuUnitsFailed.log", " Error 3 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuUnitsFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult ItemPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuItemFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuItemFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuProduct.ProductList clist = new MenuProduct.ProductList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "product";

                //string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
                //                case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
                //                iCreatedDate,iModifiedDate from mCore_Product p
                //                join muCore_Product mup on p.iMasterId=mup.iMasterId
                //                join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
                //                join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
                //                where p.iMasterId<>0 and iStatus<>5  and p.bGroup=0 and mup.Posted=0";
                string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
                                case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
                                iCreatedDate,iModifiedDate,pu.ProductDisplayUnit from mCore_Product p
                                join muCore_Product mup on p.iMasterId=mup.iMasterId
                                join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
                                join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
                                where p.iMasterId<>0 and iStatus<>5  and p.bGroup=0";

                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuProduct.Product> lc = new List<MenuProduct.Product>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuProduct.Product c = new MenuProduct.Product();
                        c.FldProdId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldCategoryId = Convert.ToInt32(row["ItemCategory"]);
                        c.FldBaseUnitId = Convert.ToInt32(row["iDefaultBaseUnit"]);
                        c.FldTaxCategory = Convert.ToString(row["TaxCategory"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        c.FldDisplayUnitId = Convert.ToInt32(row["ProductDisplayUnit"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("AlZajelMenuItem.log", "New Posted sContent: " + sContent);
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuItem.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuProduct.ProductResult>(arrResponse);
                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_Product set Posted=1,PostedDate={strdate} where iMasterId={item.FldProdId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_Product set Posted=2,PostedDate={strdate} where iMasterId={item.FldProdId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode + " \n ");
                                        objreg.SetSuccessLog("AlZajelMenuItem.log", "Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Product Master with Posted status as 0 :- " + item + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuItemFailed.log", "ErrorList Message for Product Master with Posted status as 0 :- " + item);
                                        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Product Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Product Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuItemFailed.log", "FailedList Message for Product Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Product Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldProdId;

                                        string UpSql = $@"update muCore_Product set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Product Master Updated Posted as 0 with Master Id:" + item.FldProdId + " \n ");
                                            objreg.SetErrorLog("AlZajelMenuItemFailed.log", "FailedList Message for Product Master Updated Posted as 0 with Master Id:" + item.FldProdId);
                                            //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Product Master Updated Posted as 0 with Master Id:" + item.FldProdId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n  source : "+e.Source.ToString()+" \n Stack Trace : "+e.StackTrace.ToString());
                objreg.SetErrorLog("AlZajelMenuItemFailed.log", " Error 1 :" + errors1);
                //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuItemFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuItemFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuProduct.DeleteProductList clist = new MenuProduct.DeleteProductList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "product";

            //    string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
            //                    case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
            //                    iCreatedDate,iModifiedDate,p.bGroup from mCore_Product p
            //                    join muCore_Product mup on p.iMasterId=mup.iMasterId
            //                    join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
            //                    join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
            //                    where p.iMasterId<>0 and iStatus=5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuProduct.DeleteProduct> dlc = new List<MenuProduct.DeleteProduct>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuProduct.DeleteProduct c = new MenuProduct.DeleteProduct();
            //            c.FldProdId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCategoryId = Convert.ToInt32(row["ItemCategory"]);
            //            c.FldBaseUnitId = Convert.ToInt32(row["iDefaultBaseUnit"]);
            //            c.FldTaxCategory = Convert.ToString(row["TaxCategory"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuItem.log", "Deletion sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuItem.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuProduct.DeleteProductResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    objreg.SetSuccessLog("AlZajelMenuItem.log", "Data posted / updated to mobile app device for Deleted Master");
            //                    //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_Product set PostedDate={strdate} where iMasterId={item.FldProdId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode + " \n ");
            //                        objreg.SetSuccessLog("AlZajelMenuItem.log", "Data posted / updated to mobile app device for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode);
            //                        //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Item Deleted Master:- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuItemFailed.log", "ErrorList Message for Item Deleted Master:- " + item);
            //                            //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Item Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuItemFailed.log", "FailedList Message for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Item Deleted Master with Master Id: " + item.FldProdId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuItemFailed.log", " Error 2 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuItemFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuItemFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //  return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuProduct.ProductList clist = new MenuProduct.ProductList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "product";

            //    string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
            //                    case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
            //                    iCreatedDate,iModifiedDate,p.bGroup from mCore_Product p
            //                    join muCore_Product mup on p.iMasterId=mup.iMasterId
            //                    join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
            //                    join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
            //                    where p.iMasterId<>0 and iStatus<>5 and bGroup=0 and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuProduct.Product> lc = new List<MenuProduct.Product>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuProduct.Product c = new MenuProduct.Product();
            //            c.FldProdId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldCategoryId = Convert.ToInt32(row["ItemCategory"]);
            //            c.FldBaseUnitId = Convert.ToInt32(row["iDefaultBaseUnit"]);
            //            c.FldTaxCategory = Convert.ToString(row["TaxCategory"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuItem.log", "Other than New and Deleted sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuItem.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuProduct.ProductResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_Product set Posted=1,PostedDate={strdate} where iMasterId={item.FldProdId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_Product set Posted=2,PostedDate={strdate} where iMasterId={item.FldProdId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode + " \n ");
            //                            objreg.SetSuccessLog("AlZajelMenuItem.log", "Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldProdId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Item Master Other than New and Deleted :- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuItemFailed.log", "ErrorList Message for Item Master Other than New and Deleted :- " + item);
            //                            //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Item Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Item Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuItemFailed.log", "FailedList Message for Item Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Item Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldProdId;

            //                            string UpSql = $@"update muCore_Product set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Item Master Updated Posted as 0 with Master Id:" + item.FldProdId + " \n ");
            //                                objreg.SetErrorLog("AlZajelMenuItemFailed.log", "FailedList Message for Item Master Updated Posted as 0 with Master Id:" + item.FldProdId);
            //                                //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Item Master Updated Posted as 0 with Master Id:" + item.FldProdId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuItemFailed.log", " Error 3 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult UnitConversionPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region UnitConversion
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuUnitConversionFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuUnitConversionFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuUnitConversion.UnitConversionList clist = new MenuUnitConversion.UnitConversionList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "package";

                string sql = $@"select a.iUnitConversionId,a.iProductId,a.iUnitId,a.fXFactor from mCore_UnitConversion a
                               join mCore_Product p on a.iProductId=p.iMasterId 
                               where a.bIsDeleted=0 and p.iStatus<>5 and p.iMasterId<>0";
               // string sql = $@"select iUnitConversionId,iProductId,iUnitId,fXFactor from mCore_UnitConversion where dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))=dbo.DateToInt(GETDATE()) and bIsDeleted=0";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuUnitConversion.UnitConversion> lc = new List<MenuUnitConversion.UnitConversion>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for Unit Conversion";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuUnitConversion.UnitConversion c = new MenuUnitConversion.UnitConversion();
                        c.FldId = Convert.ToInt32(row["iUnitConversionId"]);
                        c.FldProductId = Convert.ToInt32(row["iProductId"]);
                        c.FldProductUnitId = Convert.ToInt32(row["iUnitId"]);
                        c.FldConversionQty = Convert.ToDecimal(row["fXFactor"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            FConvert.LogFile("AlZajelMenuUnitConversion.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuUnitConversion.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuUnitConversion.UnitConversionResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {

                                    msg.AppendLine();
                                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " \n ");
                                    FConvert.LogFile("AlZajelMenuUnitConversion.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId);
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for UnitConversion Master with Posted status as 0 :- " + item + " \n ");
                                        FConvert.LogFile("AlZajelMenuUnitConversionFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for UnitConversion Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for UnitConversion Master with Master Id:" + item.FldId + " \n ");
                                        FConvert.LogFile("AlZajelMenuUnitConversionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for UnitConversion Master with Master Id:" + item.FldId);
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for Unit Conversion";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                FConvert.LogFile("AlZajelMenuUnitConversionFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            // return Json(new { CompanyId = CompanyId }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult BranchPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuBranchFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuBranch.BranchList clist = new MenuBranch.BranchList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "branch";

                //string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
                //                from mCore_CompanyMaster cm
                //                join muCore_CompanyMaster mucm on cm.iMasterId=mucm.iMasterId
                //                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                //                where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0 and mucm.Posted=0";
                string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
                                from mCore_CompanyMaster cm
                                join muCore_CompanyMaster mucm on cm.iMasterId=mucm.iMasterId
                                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                                where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0";

                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuBranch.Branch> lc = new List<MenuBranch.Branch>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuBranch.Branch c = new MenuBranch.Branch();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldAddress = Convert.ToString(row["Address"]);
                        c.FldTelephone = Convert.ToString(row["Telephone"]);
                        c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
                        c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            objreg.SetSuccessLog("AlZajelMenuBranch.log", "New Posted sContent: " + sContent);
                            //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuBranch.log", " PostingURL :" + PostingURL);
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuBranch.BranchResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_CompanyMaster set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_CompanyMaster set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        objreg.SetSuccessLog("AlZajelMenuBranch.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Branch Master with Posted status as 0 :- " + item + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "ErrorList Message for Branch Master with Posted status as 0 :- " + item);
                                        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Branch Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muCore_CompanyMaster set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId);
                                            //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                objreg.SetErrorLog("AlZajelMenuBranchFailed.log", " Error 1 :" + errors1);
                //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuBranch.DeleteBranchList clist = new MenuBranch.DeleteBranchList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "branch";

            //    string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
            //                    from mCore_CompanyMaster cm
            //                    join muCore_CompanyMaster mucm on cm.iMasterId=mucm.iMasterId
            //                    join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
            //                    where cm.iMasterId<>0 and cm.iStatus=5 and cm.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(cm.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuBranch.DeleteBranch> dlc = new List<MenuBranch.DeleteBranch>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuBranch.DeleteBranch c = new MenuBranch.DeleteBranch();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldAddress = Convert.ToString(row["Address"]);
            //            c.FldTelephone = Convert.ToString(row["Telephone"]);
            //            c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
            //            c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuBranch.log", "Deletion sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuBranch.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuBranch.DeleteBranchResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    objreg.SetSuccessLog("AlZajelMenuBranch.log", "Data posted / updated to mobile app device for Deleted Master");
            //                    //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_CompanyMaster set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                        objreg.SetSuccessLog("AlZajelMenuBranch.log", "Data posted / updated to mobile app device for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Branch Deleted Master:- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "ErrorList Message for Branch Deleted Master:- " + item);
            //                            //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Branch Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "FailedList Message for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Branch Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuBranchFailed.log", " Error 2 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuBranchFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuBranch.BranchList clist = new MenuBranch.BranchList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "branch";

            //    string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
            //                    from mCore_CompanyMaster cm
            //                    join muCore_CompanyMaster mucm on cm.iMasterId=mucm.iMasterId
            //                    join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
            //                    where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(cm.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuBranch.Branch> lc = new List<MenuBranch.Branch>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuBranch.Branch c = new MenuBranch.Branch();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldAddress = Convert.ToString(row["Address"]);
            //            c.FldTelephone = Convert.ToString(row["Telephone"]);
            //            c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
            //            c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuBranch.log", "Other than New and Deleted sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuBranch.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuBranch.BranchResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_CompanyMaster set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_CompanyMaster set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            objreg.SetSuccessLog("AlZajelMenuBranch.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuBranch.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Branch Master Other than New and Deleted :- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "ErrorList Message for Branch Master Other than New and Deleted :- " + item);
            //                            //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Branch Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Branch Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muCore_CompanyMaster set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                objreg.SetErrorLog("AlZajelMenuBranchFailed.log", "FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                                //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Branch Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuBranchFailed.log", " Error 3 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuBranchFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult EmployeePosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuEmployee.EmployeeList clist = new MenuEmployee.EmployeeList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "salesman";

                //string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate ,e.bGroup
                //                from mPay_Employee e
                //                join muPay_Employee meg on e.iMasterId=meg.iMasterId
                //                join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
                //                join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
                //                join mpay_Nationality n on n.iMasterId=me.iNationality
                //                where e.iMasterId>0 and e.iStatus<>5 and e.bGroup=0 and meg.Posted=0";
                string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate ,e.bGroup
                                from mPay_Employee e
                                join muPay_Employee meg on e.iMasterId=meg.iMasterId
                                join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
                                join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
                                join mpay_Nationality n on n.iMasterId=me.iNationality
                                where e.iMasterId>0 and e.iStatus<>5 and e.bGroup=0";

                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuEmployee.Employee> lc = new List<MenuEmployee.Employee>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuEmployee.Employee c = new MenuEmployee.Employee();
                        c.FldRouteId = Convert.ToInt32(row["iMasterId"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldNationality = Convert.ToString(row["Nationality"]);
                        c.FldAddress = Convert.ToString(row["sAddress"]);
                        c.FldMobilePhone = Convert.ToString(row["sMobileNumber"]);
                        c.FldEmploymentDate = Convert.ToString(row["dDateofJoining"]);
                        c.FldBranchId = Convert.ToInt32(row["BranchId"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuEmployee.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuEmployee.EmployeeResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muPay_Employee set Posted=1,PostedDate={strdate} where iMasterId={item.FldRouteId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muPay_Employee set Posted=2,PostedDate={strdate} where iMasterId={item.FldRouteId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldRouteId + " With Code:- " + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldRouteId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Employee Master with Posted status as 0 :- " + item + " \n ");
                                        FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Employee Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldRouteId;

                                        string UpSql = $@"update muCore_CompanyMaster set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Employee Master Updated Posted as 0 with Master Id:" + item.FldRouteId + " \n ");
                                            FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Employee Master Updated Posted as 0 with Master Id:" + item.FldRouteId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuEmployee.DeleteEmployeeList clist = new MenuEmployee.DeleteEmployeeList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "salesman";

            //    string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate ,e.bGroup
            //                    from mPay_Employee e
            //                    join muPay_Employee meg on e.iMasterId=meg.iMasterId
            //                    join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
            //                    join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
            //                    join mpay_Nationality n on n.iMasterId=me.iNationality
            //                    where e.iMasterId>0 and e.iStatus=5 and e.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(e.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuEmployee.DeleteEmployee> dlc = new List<MenuEmployee.DeleteEmployee>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuEmployee.DeleteEmployee c = new MenuEmployee.DeleteEmployee();
            //            c.FldRouteId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldNationality = Convert.ToString(row["Nationality"]);
            //            c.FldAddress = Convert.ToString(row["sAddress"]);
            //            c.FldMobilePhone = Convert.ToString(row["sMobileNumber"]);
            //            c.FldEmploymentDate = Convert.ToString(row["dDateofJoining"]);
            //            c.FldBranchId = Convert.ToInt32(row["BranchId"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuEmployee.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuEmployee.DeleteEmployeeResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muPay_Employee set PostedDate={strdate} where iMasterId={item.FldRouteId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Employee Deleted Master with Master Id: " + item.FldRouteId + " and Code: " + item.FldCode + " \n ");
            //                        FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Employee Deleted Master with Master Id: " + item.FldRouteId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Employee Deleted Master:- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Employee Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Employee Deleted Master with Master Id: " + item.FldRouteId + " and Code: " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Employee Deleted Master with Master Id: " + item.FldRouteId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", " : Invalid Token");
            //        //FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuEmployeeFailed.log", " : " + sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuEmployee.EmployeeList clist = new MenuEmployee.EmployeeList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "salesman";

            //    string sql = $@"select e.iMasterId,e.sName,e.sCode,n.sName [Nationality],mec.sAddress,mec.sMobileNumber,convert(varchar,dbo.IntToDate(meg.dDateofJoining), 110) dDateofJoining,iTag1 [BranchId],e.iCreatedDate,e.iModifiedDate ,e.bGroup
            //                    from mPay_Employee e
            //                    join muPay_Employee meg on e.iMasterId=meg.iMasterId
            //                    join muPay_Employee_PersonalInformation me on e.iMasterId=me.iMasterId
            //                    join muPay_Employee_ContactDetails mec on mec.iMasterId=e.iMasterId
            //                    join mpay_Nationality n on n.iMasterId=me.iNationality
            //                    where e.iMasterId>0 and e.iStatus<>5 and e.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(e.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuEmployee.Employee> lc = new List<MenuEmployee.Employee>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuEmployee.Employee c = new MenuEmployee.Employee();
            //            c.FldRouteId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldNationality = Convert.ToString(row["Nationality"]);
            //            c.FldAddress = Convert.ToString(row["sAddress"]);
            //            c.FldMobilePhone = Convert.ToString(row["sMobileNumber"]);
            //            c.FldEmploymentDate = Convert.ToString(row["dDateofJoining"]);
            //            c.FldBranchId = Convert.ToInt32(row["BranchId"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuEmployee.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuEmployee.EmployeeResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muPay_Employee set Posted=1,PostedDate={strdate} where iMasterId={item.FldRouteId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muPay_Employee set Posted=2,PostedDate={strdate} where iMasterId={item.FldRouteId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldRouteId + " With Code:- " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuEmployee.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldRouteId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Employee Master Other than New and Deleted :- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Employee Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Employee Master with Master Id:" + item.FldRouteId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldRouteId;

            //                            string UpSql = $@"update muPay_Employee set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Employee Master Updated Posted as 0 with Master Id:" + item.FldRouteId + " \n ");
            //                                FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Employee Master Updated Posted as 0 with Master Id:" + item.FldRouteId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuEmployeeFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult OutletPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuOutletFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuOutletFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");

                    //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuOutlet.OutletList clist = new MenuOutlet.OutletList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "warehouse";

                //string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                //                Employee,iCreatedDate,iModifiedDate,bGroup from mpos_Outlet o
                //                join muPos_Outlet mo on mo.iMasterId=o.iMasterId
                //                where o.iMasterId>0 and iStatus<>5 and o.bGroup=0 and mo.Posted=0";
                string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                                Employee,iCreatedDate,iModifiedDate,bGroup from mpos_Outlet o
                                join muPos_Outlet mo on mo.iMasterId=o.iMasterId
                                where o.iMasterId>0 and iStatus<>5 and o.bGroup=0";

                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuOutlet.Outlet> lc = new List<MenuOutlet.Outlet>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuOutlet.Outlet c = new MenuOutlet.Outlet();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldBranchId = Convert.ToInt32(row["OutletId"]);
                        c.FldType = Convert.ToInt32(row["OutletTypeID"]);
                        c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
                        c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
                        c.FldRouteId = Convert.ToInt32(row["Employee"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuOutlet.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuOutlet.OutletResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muPos_Outlet set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muPos_Outlet set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Outlet Master with Posted status as 0 :- " + item + " \n ");
                                        FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Outlet Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muPos_Outlet set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Outlet Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Outlet Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuOutletFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuOutletFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuOutlet.DeleteOutletList clist = new MenuOutlet.DeleteOutletList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "warehouse";

            //    string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
            //                    Employee,iCreatedDate,iModifiedDate,bGroup from mpos_Outlet o
            //                    join muPos_Outlet mo on mo.iMasterId=o.iMasterId
            //                    where o.iMasterId>0 and o.iStatus=5 and o.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuOutlet.DeleteOutlet> dlc = new List<MenuOutlet.DeleteOutlet>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuOutlet.DeleteOutlet c = new MenuOutlet.DeleteOutlet();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldBranchId = Convert.ToInt32(row["OutletId"]);
            //            c.FldType = Convert.ToInt32(row["OutletTypeID"]);
            //            c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
            //            c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
            //            c.FldRouteId = Convert.ToInt32(row["Employee"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuOutlet.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuOutlet.DeleteOutletResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muPos_Outlet set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Outlet Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                        FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Outlet Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Outlet Deleted Master:- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Outlet Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Outlet Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Outlet Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuOutletFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuOutletFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuOutlet.OutletList clist = new MenuOutlet.OutletList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "warehouse";

            //    string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
            //                    Employee,iCreatedDate,iModifiedDate,bGroup from mpos_Outlet o
            //                    join muPos_Outlet mo on mo.iMasterId=o.iMasterId
            //                    where o.iMasterId>0 and o.iStatus<>5 and o.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuOutlet.Outlet> lc = new List<MenuOutlet.Outlet>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuOutlet.Outlet c = new MenuOutlet.Outlet();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldBranchId = Convert.ToInt32(row["OutletId"]);
            //            c.FldType = Convert.ToInt32(row["OutletTypeID"]);
            //            c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
            //            c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
            //            c.FldRouteId = Convert.ToInt32(row["Employee"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuOutlet.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuOutlet.OutletResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muPos_Outlet set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muPos_Outlet set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuOutlet.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Outlet Master Other than New and Deleted :- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Outlet Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Outlet Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muPos_Outlet set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Outlet Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Outlet Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CurrencyPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region Currency
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuCurrencyFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuCurrencyFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuCurrency.CurrencyList clist = new MenuCurrency.CurrencyList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "currency";

                //string sql = $@"select c.iCurrencyId,c.sCode,c.sName,c.iNoOfDecimals,fRate from mCore_Currency c 
                //                join mCore_ExchangeRateDefinition erd on erd.iCurrencyNameId=c.iCurrencyId
                //                join mCore_ExchangeRate er on erd.iDefinedAs=iBaseCurrencyId
                //                where c.iCurrencyId>0 and bIsDeleted=0";
                string sql = $@"select iCurrencyId [iCurrencyId], sCode,sName, iNoOfDecimals,1 fRate from mCore_Currency where sCode='AED'
                                union
                                select c.iCurrencyId,c.sCode,c.sName,c.iNoOfDecimals,fRate from mCore_Currency c 
                                join mCore_ExchangeRateDefinition erd on erd.iCurrencyNameId=c.iCurrencyId
                                join mCore_ExchangeRate er on erd.iDefinedAs=iBaseCurrencyId
                                where c.iCurrencyId>0 and bIsDeleted=0 ";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuCurrency.Currency> lc = new List<MenuCurrency.Currency>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for Currency";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuCurrency.Currency c = new MenuCurrency.Currency();
                        c.FldId = Convert.ToInt32(row["iCurrencyId"]);
                        c.FldSymbol = Convert.ToString(row["sCode"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldExchangeRate = Convert.ToDecimal(row["fRate"]);
                        c.FldPlaces = Convert.ToInt32(row["iNoOfDecimals"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            FConvert.LogFile("AlZajelMenuCurrency.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuCurrency.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuCurrency.CurrencyResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {

                                    msg.AppendLine();
                                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " \n ");
                                    FConvert.LogFile("AlZajelMenuCurrency.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId);
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Currency Master with Posted status as 0 :- " + item + " \n ");
                                        FConvert.LogFile("AlZajelMenuCurrencyFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Currency Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Currency Master with Master Id:" + item.FldId + " \n ");
                                        FConvert.LogFile("AlZajelMenuCurrencyFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Currency Master with Master Id:" + item.FldId);
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for Currency";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                FConvert.LogFile("AlZajelMenuCurrencyFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            // return Json(new { CompanyId = CompanyId }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult WarehousePosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");

                    //FConvert.LogFile("AlZajelMenuOutletFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuOutlet.OutletList clist = new MenuOutlet.OutletList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "warehouse";

                //string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                //                Employee,iCreatedDate,iModifiedDate,bGroup from mCore_Warehouse o
                //                join muCore_Warehouse mo on mo.iMasterId=o.iMasterId
                //                where o.iMasterId>0 and iStatus<>5 and o.bGroup=0 and mo.Posted=0";
                string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
                                Employee,iCreatedDate,iModifiedDate,bGroup from mCore_Warehouse o
                                join muCore_Warehouse mo on mo.iMasterId=o.iMasterId
                                where o.iMasterId>0 and iStatus<>5 and o.bGroup=0";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuOutlet.Outlet> lc = new List<MenuOutlet.Outlet>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuOutlet.Outlet c = new MenuOutlet.Outlet();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldBranchId = Convert.ToInt32(row["OutletId"]);
                        c.FldType = Convert.ToInt32(row["OutletTypeID"]);
                        c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
                        c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
                        c.FldRouteId = Convert.ToInt32(row["Employee"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuWarehouse.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuOutlet.OutletResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_Warehouse set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_Warehouse set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Warehouse Master with Posted status as 0 :- " + item + " \n ");
                                        FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Warehouse Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Warehouse Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Warehouse Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muCore_Warehouse set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Warehouse Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Warehouse Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuOutlet.DeleteOutletList clist = new MenuOutlet.DeleteOutletList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "warehouse";

            //    string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
            //                    Employee,iCreatedDate,iModifiedDate,bGroup from mCore_Warehouse o
            //                    join muCore_Warehouse mo on mo.iMasterId=o.iMasterId
            //                    where o.iMasterId>0 and o.iStatus=5 and o.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuOutlet.DeleteOutlet> dlc = new List<MenuOutlet.DeleteOutlet>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuOutlet.DeleteOutlet c = new MenuOutlet.DeleteOutlet();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldBranchId = Convert.ToInt32(row["OutletId"]);
            //            c.FldType = Convert.ToInt32(row["OutletTypeID"]);
            //            c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
            //            c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
            //            c.FldRouteId = Convert.ToInt32(row["Employee"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuWarehouse.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuOutlet.DeleteOutletResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_Warehouse set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Warehouse Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                        FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Warehouse Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Warehouse Deleted Master:- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Warehouse Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Warehouse Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Warehouse Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuWarehouseFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuOutlet.OutletList clist = new MenuOutlet.OutletList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "warehouse";

            //    string sql = $@"select o.iMasterId,sName,sCode,mo.CompanyMaster [OutletId],case when OutletTypeID=0 then 1 else 2 end OutletTypeID,PlateNumber,convert(varchar,dbo.IntToDate(RegistrationDate), 23) RegistrationDate,
            //                    Employee,iCreatedDate,iModifiedDate,bGroup from mCore_Warehouse o
            //                    join muCore_Warehouse mo on mo.iMasterId=o.iMasterId
            //                    where o.iMasterId>0 and o.iStatus<>5 and o.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuOutlet.Outlet> lc = new List<MenuOutlet.Outlet>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuOutlet.Outlet c = new MenuOutlet.Outlet();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldBranchId = Convert.ToInt32(row["OutletId"]);
            //            c.FldType = Convert.ToInt32(row["OutletTypeID"]);
            //            c.FldPlateNumber = Convert.ToString(row["PlateNumber"]);
            //            c.FldRegistrationDate = Convert.ToString(row["RegistrationDate"]);
            //            c.FldRouteId = Convert.ToInt32(row["Employee"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuWarehouse.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuOutlet.OutletResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_Warehouse set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_Warehouse set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuWarehouse.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Warehouse Master Other than New and Deleted :- " + item + " \n ");
            //                            FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Warehouse Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Warehouse Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Warehouse Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muCore_Warehouse set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Warehouse Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Warehouse Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    FConvert.LogFile("AlZajelMenuWarehouseFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DivisionPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuBranch.BranchList clist = new MenuBranch.BranchList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "branch";

                //string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
                //                from mCore_Division cm
                //                join muCore_Division mucm on cm.iMasterId=mucm.iMasterId
                //                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                //                where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0 and mucm.Posted=0";
                string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
                                from mCore_Division cm
                                join muCore_Division mucm on cm.iMasterId=mucm.iMasterId
                                join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
                                where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuBranch.Branch> lc = new List<MenuBranch.Branch>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuBranch.Branch c = new MenuBranch.Branch();
                        c.FldId = Convert.ToInt32(row["iMasterId"]);
                        c.FldCode = Convert.ToString(row["sCode"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldAddress = Convert.ToString(row["Address"]);
                        c.FldTelephone = Convert.ToString(row["Telephone"]);
                        c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
                        c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
                        c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
                        c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            objreg.SetSuccessLog("AlZajelMenuDivision.log", "New Posted sContent: " + sContent);
                            //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuDivision.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuBranch.BranchResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                                foreach (var item in clist.ItemList)
                                {
                                    int res = 0;
                                    if (item.CreatedDate == item.ModifiedDate)
                                    {
                                        string UpSql = $@"update muCore_Division set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    else
                                    {
                                        string UpSql = $@"update muCore_Division set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
                                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                    }
                                    if (res == 1)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
                                        objreg.SetSuccessLog("AlZajelMenuDivision.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
                                    }
                                }
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Division Master with Posted status as 0 :- " + item + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "ErrorList Message for Division Master with Posted status as 0 :- " + item);
                                        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Division Master with Posted status as 0 :- " + item);
                                    }
                                }

                                int FailedListCount = lng.FailedList.Count();
                                if (FailedListCount > 0)
                                {
                                    var FailedList = lng.FailedList.ToList();
                                    foreach (var item in FailedList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
                                        var FieldMasterId = item.FldId;

                                        string UpSql = $@"update muCore_Division set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
                                        int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                        if (res == 1)
                                        {
                                            msg.AppendLine();
                                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
                                            objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId);
                                            //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId);
                                        }
                                    }
                                }
                                isStatus = false;
                            }
                        }

                        #endregion
                    }
                }
                //else
                //{
                //    Message = "No Data Found for PostedStatusZero";
                //    msg.AppendLine();
                //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                //}
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", " Error 1 :" + errors1);
                //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion

            #region PostedStatuswithiStatus5
            //try
            //{
            //    AccessToken = GetAccessToken();
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuBranch.DeleteBranchList clist = new MenuBranch.DeleteBranchList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "branch";

            //    string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
            //                    from mCore_Division cm
            //                    join muCore_Division mucm on cm.iMasterId=mucm.iMasterId
            //                    join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
            //                    where cm.iMasterId<>0 and cm.iStatus=5 and cm.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(cm.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuBranch.DeleteBranch> dlc = new List<MenuBranch.DeleteBranch>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatus5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuBranch.DeleteBranch c = new MenuBranch.DeleteBranch();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldAddress = Convert.ToString(row["Address"]);
            //            c.FldTelephone = Convert.ToString(row["Telephone"]);
            //            c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
            //            c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
            //            c.FldIsDeleted = 1;
            //            dlc.Add(c);
            //        }
            //        clist.ItemList = dlc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Deletion sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuDivision.log", "Deletion sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Deletion sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuDivision.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuBranch.DeleteBranchResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    msg.AppendLine();
            //                    msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Deleted Master" + " \n ");
            //                    objreg.SetSuccessLog("AlZajelMenuDivision.log", "Data posted / updated to mobile app device for Deleted Master");
            //                    //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Deleted Master");
            //                    isStatus = true;

            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        string UpSql = $@"update muCore_Division set PostedDate={strdate} where iMasterId={item.FldId}";
            //                        res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        msg.AppendLine();
            //                        msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                        objreg.SetSuccessLog("AlZajelMenuDivision.log", "Data posted / updated to mobile app device for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                    }
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Division Deleted Master:- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "ErrorList Message for Division Deleted Master:- " + item);
            //                            //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Division Deleted Master:- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "FailedList Message for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Division Deleted Master with Master Id: " + item.FldId + " and Code: " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatus5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    isStatus = false;
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 2 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", " Error 2 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + " Error 2 :" + errors1);
            //}
            #endregion

            #region PostedStatuswithiStatusNot5
            //try
            //{
            //    if (AccessToken == "")
            //    {
            //        Message = "Invalid Token";
            //        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "Invalid Token");
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
            //        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : Invalid Token");
            //        var sMessage = "Token Should not be Empty";
            //        objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", sMessage);
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
            //        //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + sMessage);
            //        //return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
            //        return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            //    }

            //    MenuBranch.BranchList clist = new MenuBranch.BranchList();
            //    clist.AccessToken = AccessToken;
            //    clist.ObjectType = "branch";

            //    string sql = $@"select cm.iMasterId,cm.sName,cm.sCode,Address,Telephone,TRN,j.sCode [Jurisdiction] ,cm.iCreatedDate,cm.iModifiedDate
            //                    from mCore_Division cm
            //                    join muCore_Division mucm on cm.iMasterId=mucm.iMasterId
            //                    join mCore_Jurisdiction j on j.iMasterId=mucm.Jurisdiction
            //                    where cm.iMasterId<>0 and cm.iStatus<>5 and cm.bGroup=0  and dbo.DateToInt(dbo.IntToGregDateTime(cm.iModifiedDate))>PostedDate";
            //    DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
            //    List<MenuBranch.Branch> lc = new List<MenuBranch.Branch>();
            //    if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
            //    {
            //        Message = "No Data Found for PostedStatuswithiStatusNot5";
            //        msg.AppendLine();
            //        msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    }
            //    else
            //    {
            //        foreach (DataRow row in ds.Tables[0].Rows)
            //        {
            //            MenuBranch.Branch c = new MenuBranch.Branch();
            //            c.FldId = Convert.ToInt32(row["iMasterId"]);
            //            c.FldCode = Convert.ToString(row["sCode"]);
            //            c.FldName = Convert.ToString(row["sName"]);
            //            c.FldAddress = Convert.ToString(row["Address"]);
            //            c.FldTelephone = Convert.ToString(row["Telephone"]);
            //            c.FldTaxRegisterationNumber = Convert.ToString(row["TRN"]);
            //            c.FldJurisdiction = Convert.ToString(row["Jurisdiction"]);
            //            c.CreatedDate = Convert.ToInt64(row["iCreatedDate"]);
            //            c.ModifiedDate = Convert.ToInt64(row["iModifiedDate"]);
            //            lc.Add(c);
            //        }
            //        clist.ItemList = lc;
            //        if (clist.ItemList.Count() > 0)
            //        {
            //            #region PostingSection
            //            var sContent = new JavaScriptSerializer().Serialize(clist);
            //            using (WebClient client = new WebClient())
            //            {
            //                client.Headers.Add("Content-Type", "application/json");
            //                msg.AppendLine();
            //                msg.Append(DateTime.Now.ToString() + ": " + " Other than New and Deleted sContent: " + sContent + " \n ");
            //                objreg.SetSuccessLog("AlZajelMenuDivision.log", "Other than New and Deleted sContent: " + sContent);
            //                //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Other than New and Deleted sContent: " + sContent);
            //                objreg.SetLog("AlZajelMenuDivision.log", " PostingURL :" + PostingURL);
            //                var arrResponse = client.UploadString(PostingURL, sContent);
            //                var lng = JsonConvert.DeserializeObject<MenuBranch.BranchResult>(arrResponse);

            //                if (lng.ResponseStatus.IsSuccess == true)
            //                {
            //                    foreach (var item in clist.ItemList)
            //                    {
            //                        int res = 0;
            //                        if (item.CreatedDate == item.ModifiedDate)
            //                        {
            //                            string UpSql = $@"update muCore_CompanyMaster set Posted=1,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        else
            //                        {
            //                            string UpSql = $@"update muCore_CompanyMaster set Posted=2,PostedDate={strdate} where iMasterId={item.FldId}";
            //                            res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                        }
            //                        if (res == 1)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode + " \n ");
            //                            objreg.SetSuccessLog("AlZajelMenuDivision.log", "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuDivision.log", DateTime.Now.ToString() + " : " + "Data posted / updated to mobile app device for Master:- " + item.FldId + " With Code:- " + item.FldCode);
            //                        }
            //                    }
            //                    isStatus = true;
            //                }
            //                else
            //                {
            //                    int ErrorListCount = lng.ErrorMessages.Count();
            //                    if (ErrorListCount > 0)
            //                    {
            //                        var ErrorList = lng.ErrorMessages.ToList();
            //                        foreach (var item in ErrorList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Division Master Other than New and Deleted :- " + item + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "ErrorList Message for Division Master Other than New and Deleted :- " + item);
            //                            //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Division Master Other than New and Deleted :- " + item);
            //                        }
            //                    }

            //                    int FailedListCount = lng.FailedList.Count();
            //                    if (FailedListCount > 0)
            //                    {
            //                        var FailedList = lng.FailedList.ToList();
            //                        foreach (var item in FailedList)
            //                        {
            //                            msg.AppendLine();
            //                            msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode + " \n ");
            //                            objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Division Master with Master Id:" + item.FldId + " and Code:" + item.FldCode);
            //                            var FieldMasterId = item.FldId;

            //                            string UpSql = $@"update muCore_Division set Posted=0,PostedDate={strdate} where iMasterId={FieldMasterId}";
            //                            int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
            //                            if (res == 1)
            //                            {
            //                                msg.AppendLine();
            //                                msg.Append(DateTime.Now.ToString() + ": " + " FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId + " \n ");
            //                                objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", "FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                                //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + "FailedList Message for Division Master Updated Posted as 0 with Master Id:" + item.FldId);
            //                            }
            //                        }
            //                    }
            //                    isStatus = false;
            //                }
            //            }
            //            #endregion
            //        }
            //    }
            //    //else
            //    //{
            //    //    Message = "No Data Found for PostedStatuswithiStatusNot5";
            //    //    msg.AppendLine();
            //    //    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
            //    //}
            //}
            //catch (Exception e)
            //{
            //    errors1 = e.Message;
            //    Message = e.Message;
            //    msg.AppendLine();
            //    msg.Append(DateTime.Now.ToString() + ": " + " Error 3 :" + errors1 + " \n ");
            //    objreg.SetErrorLog("AlZajelMenuDivisionFailed.log", " Error 3 :" + errors1);
            //    //FConvert.LogFile("AlZajelMenuDivisionFailed.log", DateTime.Now.ToString() + " : " + " Error 3 :" + errors1);
            //    isStatus = false;
            //}
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
            // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
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
                client.Encoding = Encoding.UTF8;
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

        [HttpPost]
        public ActionResult CategoryFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuCategoryLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuCategoryLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuCategoryLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult UnitFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuUnitLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuUnitLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuUnitLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult ItemFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuItemLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuItemLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuItemLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult BranchFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuBranchLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuBranchLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuBranchLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult EmployeeFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuEmployeeLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuEmployeeLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuEmployeeLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult OutletFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuOutletLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuOutletLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuOutletLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult UnitConversionFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuUnitConversionLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuUnitConversionLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuUnitConversionLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult CurrencyFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuCurrencyLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuCurrencyLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuCurrencyLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult AccountFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuAccountLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuAccountLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuAccountLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult WarehouseFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuWarehouseLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuWarehouseLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuWarehouseLog.txt");
                }
            }
            return null;
        }

        [HttpPost]
        public ActionResult DivisionFileDownload()
        {
            string ValidationMessage = Session["TextData"].ToString();
            if (!string.IsNullOrEmpty(ValidationMessage))
            {
                string path = @"C:\Windows\Temp\MenuDivisionLog.txt";
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuDivisionLog.txt");
                }
                else
                {
                    FileInfo fi = new FileInfo(path);
                    StreamWriter sw = fi.CreateText();
                    sw.Close();

                    System.IO.File.WriteAllText(path, ValidationMessage);
                    byte[] bytes = System.IO.File.ReadAllBytes(path);
                    return File(bytes, "application/text", "MenuDivisionLog.txt");
                }
            }
            return null;
        }


        public ActionResult AccountPosting(int CompanyId)
        {
            StringBuilder msg = new StringBuilder();
            int strdate = GetDateToInt(DateTime.Now);
            bool isStatus = false;

            AccessToken = GetAccessToken();
            #region PostedStatusZero
            try
            {
                if (AccessToken == "")
                {
                    Message = "Invalid Token";
                    objreg.SetErrorLog("AlZajelMenuItemFailed.log", "Invalid Token");
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + "Invalid Token" + " \n ");
                    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetErrorLog("AlZajelMenuItemFailed.log", sMessage);
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + sMessage + " \n ");
                    //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + sMessage);
                    // return Json(new { isStatus = isStatus }, JsonRequestBehavior.AllowGet);
                    return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
                }

                MenuAccount.AccountList clist = new MenuAccount.AccountList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "customer";

                string sql = $@"select a.iMasterId,a.sName,a.sCode, d.ContactPerson,d.sAddress,d.MobileNo,d.sTelNo,d.sEMail,mu.ShipToName,mu.ShipToAddress,mu.ShipToEmail,mu.ShipToTel,v.TRN,a.iCreditDays,a.fCreditLimit,mu.CreditCategory,p.sCode PlaceOfSupply
                                from mCore_Account a
                                join muCore_Account mu on a.iMasterId=mu.iMasterId
                                join muCore_Account_Details  d on d.iMasterId = a.iMasterId 
                                join muCore_Account_VAT_Settings v on v.iMasterId = a.iMasterId
                                join mCore_PlaceOfSupply p on p.iMasterId = v.PlaceOfSupply
                                where a.iMasterId<>0 and iAccountType = 5 and a.bGroup <>1 and a.istatus<>5";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<MenuAccount.Account> lc = new List<MenuAccount.Account>();
                if (ds == null || ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
                {
                    Message = "No Data Found for PostedStatusZero";
                    msg.AppendLine();
                    msg.Append(DateTime.Now.ToString() + ": " + Message + " \n ");
                }
                else
                {
                    foreach (DataRow row in ds.Tables[0].Rows)
                    {
                        MenuAccount.Account c = new MenuAccount.Account();
                        c.FldIntegrationPartyId = Convert.ToInt32(row["iMasterId"]);
                        c.FldName = Convert.ToString(row["sName"]);
                        c.FldCustomerCode = Convert.ToString(row["sCode"]);
                        c.FldContactPerson = Convert.ToString(row["ContactPerson"]);
                        c.FldAddress = Convert.ToString(row["sAddress"]);
                        c.FldMobilePhone = Convert.ToString(row["MobileNo"]);
                        c.FldTelephone = Convert.ToString(row["sTelNo"]);
                        c.FldEmail = Convert.ToString(row["sEMail"]);
                        c.FldShipToName = Convert.ToString(row["ShipToName"]);
                        c.FldShipToAddress = Convert.ToString(row["ShipToAddress"]);
                        c.FldShipToEmail = Convert.ToString(row["ShipToEmail"]);
                        c.FldShipToTel = Convert.ToString(row["ShipToTel"]);
                        c.FldTrn = Convert.ToString(row["TRN"]);
                        c.FldCreditDays = Convert.ToInt32(row["iCreditDays"]);
                        c.FldCreditLimit = Convert.ToDecimal(row["fCreditLimit"]);
                        c.FldCreditCategory = Convert.ToInt32(row["CreditCategory"]);
                        c.FldPlaceOfSupply = Convert.ToString(row["PlaceOfSupply"]);
                        c.FldBranchId = 0;
                        c.FldCategoryId =1;
                        c.FldActivityId = 0; 
                        c.FldAdminAreaId = 0;
                        c.FldWorkingAreaId = 0;
                        c.FldPriceBookId = 0;
                        c.FldCurrencyId = 7;
                        c.FldRouteId = null;
                        lc.Add(c);
                    }
                    clist.ItemList = lc;
                    if (clist.ItemList.Count() > 0)
                    {
                        #region PostingSection

                        var sContent = new JavaScriptSerializer().Serialize(clist);
                        using (WebClient client = new WebClient())
                        {
                            client.Headers.Add("Content-Type", "application/json");
                            objreg.SetSuccessLog("AlZajelMenuItem.log", "New Posted sContent: " + sContent);
                            msg.AppendLine();
                            msg.Append(DateTime.Now.ToString() + ": " + " New Posted sContent: " + sContent + " \n ");
                            //FConvert.LogFile("AlZajelMenuItem.log", DateTime.Now.ToString() + " : " + "New Posted sContent: " + sContent);
                            objreg.SetLog("AlZajelMenuItem.log", " PostingURL :" + PostingURL);
                            client.Encoding = Encoding.UTF8;
                            var arrResponse = client.UploadString(PostingURL, sContent);
                            var lng = JsonConvert.DeserializeObject<MenuAccount.AccountResult>(arrResponse);

                            if (lng.ResponseStatus.IsSuccess == true)
                            {
                               
                                isStatus = true;
                            }
                            else
                            {
                                int ErrorListCount = lng.ErrorMessages.Count();
                                if (ErrorListCount > 0)
                                {
                                    var ErrorList = lng.ErrorMessages.ToList();
                                    foreach (var item in ErrorList)
                                    {
                                        msg.AppendLine();
                                        msg.Append(DateTime.Now.ToString() + ": " + " ErrorList Message for Account Master with Posted status as 0 :- " + item + " \n ");
                                        objreg.SetErrorLog("AlZajelMenuItemFailed.log", "ErrorList Message for Account Master with Posted status as 0 :- " + item);
                                        //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + "ErrorList Message for Account Master with Posted status as 0 :- " + item);
                                    }
                                }

                                
                            }
                        }

                        #endregion
                    }
                }
               
            }
            catch (Exception e)
            {
                isStatus = false;
                errors1 = e.Message;
                Message = e.Message;
                msg.AppendLine();
                msg.Append(DateTime.Now.ToString() + ": " + " Error 1 :" + errors1 + " \n ");
                objreg.SetErrorLog("AlZajelMenuItemFailed.log", " Error 1 :" + errors1);
                //FConvert.LogFile("AlZajelMenuItemFailed.log", DateTime.Now.ToString() + " : " + " Error 1 :" + errors1);
            }
            #endregion
            Session["TextData"] = msg.ToString();
            return Json(new { success = "1" }, JsonRequestBehavior.AllowGet);
        }
    }
}