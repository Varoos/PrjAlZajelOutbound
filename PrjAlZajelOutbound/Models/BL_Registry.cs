using Focus.Common.DataStructs;
using Microsoft.Practices.EnterpriseLibrary.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;

namespace PrjAlZajelOutbound.Models
{
    public class BL_Registry
    {
        public DataSet GetData(string strSelQry, int CompId, ref string error)
        {
            error = "";
            try
            {
                Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                return (obj.ExecuteDataSet(CommandType.Text, strSelQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile("AlZajel.log", "err : " + "[" + System.DateTime.Now + "] - GetData :" + error + "---" + strSelQry);
                return null;
            }
        }

        public int GetExecute(string strSelQry, int CompId, ref string error)
        {
            error = "";
            try
            {
                Database obj = Focus.DatabaseFactory.DatabaseWrapper.GetDatabase(CompId);
                return (obj.ExecuteNonQuery(CommandType.Text, strSelQry));
            }
            catch (Exception e)
            {
                error = e.Message;
                FConvert.LogFile("AlZajel.log", DateTime.Now.ToString() + " GetExecute :" + error + "---" + strSelQry);
                return 0;
            }
        }
        public void SetLog(string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = System.IO.Path.GetTempPath()  + "AlSalamLog" + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public void SetLog(string LogName, string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine)+@"\" + LogName + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public void SetSuccessLog(string LogName, string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = Environment.GetEnvironmentVariable("TEMP", EnvironmentVariableTarget.Machine) + @"\" + LogName + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }

        public void SetErrorLog(string LogName, string content)
        {
            StreamWriter objSw = null;
            try
            {
                string sFilePath = System.Web.HttpContext.Current.Server.MapPath("~/Temp") + LogName + DateTime.Now.Date.ToString("ddMMyyyy") + ".txt";
                objSw = new StreamWriter(sFilePath, true);
                objSw.WriteLine(DateTime.Now.ToString() + " " + content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                //SetLog("Error -" + ex.Message);
            }
            finally
            {
                if (objSw != null)
                {
                    objSw.Flush();
                    objSw.Dispose();
                }
            }
        }
    }
}