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
    public class ItemController : Controller
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
        // GET: Item
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
                    objreg.SetLog("AlZajelItem.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelItem.log", sMessage);
                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                ProductList clist = new ProductList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "product";

                string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
                                case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
                                iCreatedDate,iModifiedDate,p.bGroup,pu.ProductDisplayUnit from mCore_Product p
                                join muCore_Product mup on p.iMasterId=mup.iMasterId
                                join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
                                join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
                                where p.iMasterId<>0 and iStatus<>5  and sName='{Name}' and sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, CompanyId, ref errors1);
                List<Product> lc = new List<Product>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    Product c = new Product();
                    c.FldProdId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldCategoryId = Convert.ToInt32(row["ItemCategory"]);
                    c.FldBaseUnitId = Convert.ToInt32(row["iDefaultBaseUnit"]);
                    c.FldTaxCategory = Convert.ToString(row["TaxCategory"]);
                    c.FldDisplayUnitId = Convert.ToInt32(row["ProductDisplayUnit"]);
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
                        objreg.SetLog("AlZajelItem.log", " PostingURL :" + PostingURL);
                        var arrResponse = client.UploadString(PostingURL, sContent);
                        var lng = JsonConvert.DeserializeObject<ProductResult>(arrResponse);

                        if (lng.ResponseStatus.IsSuccess == true)
                        {
                            int res = 0;
                            if (CreatedDate == ModifiedDate)
                            {
                                string UpSql = $@"update muCore_Product set Posted=1,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldProdId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            else
                            {
                                string UpSql = $@"update muCore_Product set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldProdId}";
                                res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                            }
                            if (res == 1)
                            {
                                objreg.SetLog("AlZajelItem.log", "Data posted / updated to mobile app device");
                                //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                            }
                            Message = "Data Posted Successful";
                        }
                        else
                        {
                            var ErrorMessagesList = lng.ErrorMessages.ToList();
                            foreach (var item in ErrorMessagesList)
                            {
                                objreg.SetLog("AlZajelItem.log", "Error Message for Outlet Master:" + item);
                                //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Error Message for Product Master:" + item);

                                string UpSql = $@"update muCore_Product set Posted=2,PostedDate={strdate} where iMasterId={clist.ItemList.First().FldProdId}";
                                int res = objreg.GetExecute(UpSql, CompanyId, ref errors1);
                                if (res == 1)
                                {
                                    objreg.SetLog("AlZajelItem.log", "Data posted / updated to mobile app device");
                                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Data posted / updated to mobile app device");
                                }
                                Message = item;
                            }

                            int FailedListCount = lng.FailedList.Count();
                            if (FailedListCount > 0)
                            {
                                var FailedList = lng.FailedList.ToList();
                                foreach (var item in FailedList)
                                {
                                    objreg.SetLog("AlZajelItem.log", "FailedList Message for Product Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "FailedList Message for Product Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                    var FieldMasterId = item.FldProdId;
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
                objreg.SetLog("AlZajelItem.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " Error :" + errors1);
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
                    objreg.SetLog("AlZajelItem.log", "Invalid Token");
                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " : Invalid Token");
                    var sMessage = "Token Should not be Empty";
                    objreg.SetLog("AlZajelItem.log", sMessage);
                    //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " : " + sMessage);
                    return Json(new { Message = Message }, JsonRequestBehavior.AllowGet);
                }

                DeleteProductList clist = new DeleteProductList();
                clist.AccessToken = AccessToken;
                clist.ObjectType = "product";

                string sql = $@"select p.iMasterId,sName,sCode,mup.ItemCategory,pu.iDefaultBaseUnit,
                                case when ps.TaxCategory=0 then 'Taxable' when ps.TaxCategory=1 then 'Zero' when ps.TaxCategory=2 then 'Exempted' end [TaxCategory],
                                iCreatedDate,iModifiedDate from mCore_Product p
                                join muCore_Product mup on p.iMasterId=mup.iMasterId
                                join muCore_Product_Units pu  on p.iMasterId=pu.iMasterId
                                join muCore_Product_Settings ps  on p.iMasterId=ps.iMasterId
                                where p.iMasterId<>0 and sName='{Name}' and sCode='{Code}'";
                DataSet ds = objreg.GetData(sql, companyId, ref errors1);
                List<DeleteProduct> lc = new List<DeleteProduct>();
                foreach (DataRow row in ds.Tables[0].Rows)
                {
                    DeleteProduct c = new DeleteProduct();
                    c.FldProdId = Convert.ToInt32(row["iMasterId"]);
                    c.FldName = Convert.ToString(row["sName"]);
                    c.FldCode = Convert.ToString(row["sCode"]);
                    c.FldCategoryId = Convert.ToInt32(row["ItemCategory"]);
                    c.FldBaseUnitId = Convert.ToInt32(row["iDefaultBaseUnit"]);
                    c.FldTaxCategory = Convert.ToString(row["TaxCategory"]);
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
                    objreg.SetLog("AlZajelItem.log", " PostingURL :" + PostingURL);
                    var arrResponse = client.UploadString(PostingURL, sContent);
                    var lng = JsonConvert.DeserializeObject<DeleteProductResult>(arrResponse);

                    if (lng.ResponseStatus.IsSuccess == true)
                    {
                        Message = lng.ResponseStatus.StatusMsg;
                        ResponseStatus = lng.ResponseStatus.IsSuccess;
                        objreg.SetLog("AlZajelItem.log", "Delete Response" + Message);
                        //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Delete Response" + Message);
                    }
                    else
                    {
                        objreg.SetLog("AlZajelItem.log", "Delete Failed Response" + lng.ResponseStatus.StatusMsg);
                        //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Delete Failed Response" + lng.ResponseStatus.StatusMsg);

                        var ErrorMessagesList = lng.ErrorMessages.ToList();
                        foreach (var item in ErrorMessagesList)
                        {
                            Message = item;
                            objreg.SetLog("AlZajelItem.log", "Error Message for Item Master:" + item);
                            //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "Error Message for Item Master:" + item);
                        }

                        int FailedListCount = lng.FailedList.Count();
                        if (FailedListCount > 0)
                        {
                            var FailedList = lng.FailedList.ToList();
                            foreach (var item in FailedList)
                            {
                                objreg.SetLog("AlZajelItem.log", "FailedList Message for Item Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + "FailedList Message for Item Master with Master Id:" + item.FldProdId + " and Code:" + item.FldCode);
                                var FieldMasterId = item.FldProdId;
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
                objreg.SetLog("AlZajelItem.log", " Error :" + errors1);
                //FConvert.LogFile("AlZajelItem.log", DateTime.Now.ToString() + " Error :" + errors1);
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
                objreg.SetLog("AlZajelItem.log", " AccessTokenURL :" + AccessTokenURL);
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