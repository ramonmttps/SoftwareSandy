using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.EnterpriseLibrary.Data.Sql;
using System.Data.Common;
using System.Data;
using System.IO;
using System.Configuration;
using System.Data.SqlClient;

namespace SoftwareSandy
{
    class QuickCap
    {
        string INExtension = string.Empty;
        string OUTExtension = string.Empty;
        string MOVEPath = string.Empty;
        string MainPath = string.Empty;
        string sINPath = string.Empty;
        string sOUTPath = string.Empty;
        string Connection = string.Empty;
        string NetworksLocation = string.Empty;
        string company = string.Empty;

        public  LogLogger log { set; get; }
        //SqlDatabase db;
        
        public void ClaimScrubRead()
        {
            if (ConfigurationSettings.AppSettings["INPath"] != null)
                sINPath = ConfigurationSettings.AppSettings["INPath"];
            if (ConfigurationSettings.AppSettings["OUTPath"] != null)
                sOUTPath = ConfigurationSettings.AppSettings["OUTPath"];
            if (ConfigurationSettings.AppSettings["INExtension"] != null)
                INExtension = ConfigurationSettings.AppSettings["INExtension"];
            if (ConfigurationSettings.AppSettings["OUTExtension"] != null)
                OUTExtension = ConfigurationSettings.AppSettings["OUTExtension"];
            if (ConfigurationSettings.AppSettings["MOVEPath"] != null)
                MOVEPath = ConfigurationSettings.AppSettings["MOVEPath"];
            if (ConfigurationSettings.AppSettings["NetworksLocation"] != null)
                NetworksLocation = ConfigurationSettings.AppSettings["NetworksLocation"];


            string connectionString = string.Empty;
            SqlDatabase sqlDB = new SqlDatabase(ConfigurationManager.ConnectionStrings["CommonDB"].ToString());
            DbCommand dbc = sqlDB.GetSqlStringCommand("SELECT * FROM DBO.COMPANY_DB_CONFIG");
            string strMSSQLConString = string.Empty;
            DataSet dsConfig = new DataSet();

            try
            {
                dsConfig = sqlDB.ExecuteDataSet(dbc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
            }

            if (dsConfig.Tables.Count > 0 && dsConfig.Tables[0].Rows.Count > 0)
            {
                string strMySqlDSN = string.Empty;
                string strCompId = string.Empty;
                string strMSSQLDB = string.Empty;

                int rowID = 0;

                foreach (DataRow dr in dsConfig.Tables[0].Rows)
                {
                    if (dr["COMPANY_ID"] != null && dr["COMPANY_ID"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(dr["COMPANY_ID"])))
                    {
                        strCompId = Convert.ToString(dr["COMPANY_ID"]);
                        //strCompId = "DNSAVMED";
                        //sINPath = String.Format(@"{0}\{1}\IN\", NetworksLocation, strCompId);
                        //sOUTPath = String.Format(@"{0}\{1}\OUT\", NetworksLocation, strCompId);
                        //MOVEPath = String.Format(@"{0}\{1}\MV_PROCESSED\", NetworksLocation, strCompId);

                    }
                    if (dr["MSSQLDBNAME"] != null && dr["MSSQLDBNAME"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(dr["MSSQLDBNAME"])))
                    {
                        strMSSQLDB = Convert.ToString(dr["MSSQLDBNAME"]);
                        sINPath = String.Format(@"{0}\{1}\IN\", NetworksLocation, strMSSQLDB);
                        sOUTPath = String.Format(@"{0}\{1}\OUT\", NetworksLocation, strMSSQLDB);
                        MOVEPath = String.Format(@"{0}\{1}\MV_PROCESSED\", NetworksLocation, strMSSQLDB);

                        strMSSQLConString = String.Format("server=172.16.0.37\\quickcap;database={0};user Id=sa;password=SA0922!", strMSSQLDB);
                        //strMSSQLConString = "server=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBSERVER"].ToString() + ";database=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBNAME"].ToString() + ";user Id=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBUSERNAME"].ToString() + ";password=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBPASSWORD"].ToString();
                    }
                    rowID++;
                    if (!string.IsNullOrEmpty(strCompId))
                    {
                        DateTime? dttmDateTime = null;
                        //     string CompanyID = System.Configuration.ConfigurationSettings.AppSettings["CompanyID"].ToString();
                        //            //AuthIntegeration.ReferralWS.ReferralWS obj = new AuthIntegeration.ReferralWS.ReferralWS();
                        dttmDateTime = GetServiceLastRunTime("SCRUB", strMSSQLConString);
                        if (!(dttmDateTime != null && dttmDateTime.HasValue))
                        {
                            log.LogInformation("No Data");
                        }
                        else
                        {
                            ReadData(dttmDateTime, strMSSQLConString);
                            //ApplyData(strMSSQLConString);
                        }
                    }
                }
            }
        } // ClaimScrub

        public void ClaimScrubWrite()
        {
            if (ConfigurationSettings.AppSettings["INPath"] != null)
                sINPath = ConfigurationSettings.AppSettings["INPath"];
            if (ConfigurationSettings.AppSettings["OUTPath"] != null)
                sOUTPath = ConfigurationSettings.AppSettings["OUTPath"];
            if (ConfigurationSettings.AppSettings["INExtension"] != null)
                INExtension = ConfigurationSettings.AppSettings["INExtension"];
            if (ConfigurationSettings.AppSettings["OUTExtension"] != null)
                OUTExtension = ConfigurationSettings.AppSettings["OUTExtension"];
            if (ConfigurationSettings.AppSettings["MOVEPath"] != null)
                MOVEPath = ConfigurationSettings.AppSettings["MOVEPath"];
            if (ConfigurationSettings.AppSettings["NetworksLocation"] != null)
                NetworksLocation = ConfigurationSettings.AppSettings["NetworksLocation"];


            string connectionString = string.Empty;
            SqlDatabase sqlDB = new SqlDatabase(ConfigurationManager.ConnectionStrings["CommonDB"].ToString());
            DbCommand dbc = sqlDB.GetSqlStringCommand("SELECT * FROM DBO.COMPANY_DB_CONFIG");
            string strMSSQLConString = string.Empty;
            DataSet dsConfig = new DataSet();

            try
            {
                dsConfig = sqlDB.ExecuteDataSet(dbc);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error " + ex.Message);
            }

            if (dsConfig.Tables.Count > 0 && dsConfig.Tables[0].Rows.Count > 0)
            {
                string strMySqlDSN = string.Empty;
                string strCompId = string.Empty;
                string strMSSQLDB = string.Empty;

                int rowID = 0;

                foreach (DataRow dr in dsConfig.Tables[0].Rows)
                {
                    if (dr["COMPANY_ID"] != null && dr["COMPANY_ID"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(dr["COMPANY_ID"])))
                    {
                        strCompId = Convert.ToString(dr["COMPANY_ID"]);
                        //strCompId = "DNSAVMED";
                        //sINPath = String.Format(@"{0}\{1}\IN\", NetworksLocation, strCompId);
                        //sOUTPath = String.Format(@"{0}\{1}\OUT\", NetworksLocation, strCompId);
                        //MOVEPath = String.Format(@"{0}\{1}\MV_PROCESSED\", NetworksLocation, strCompId);

                    }
                    if (dr["MSSQLDBNAME"] != null && dr["MSSQLDBNAME"] != DBNull.Value && !string.IsNullOrEmpty(Convert.ToString(dr["MSSQLDBNAME"])))
                    {
                        strMSSQLDB = Convert.ToString(dr["MSSQLDBNAME"]);
                        sINPath = String.Format(@"{0}\{1}\IN\", NetworksLocation, strMSSQLDB);
                        sOUTPath = String.Format(@"{0}\{1}\OUT\", NetworksLocation, strMSSQLDB);
                        MOVEPath = String.Format(@"{0}\{1}\MV_PROCESSED\", NetworksLocation, strMSSQLDB);

                        strMSSQLConString = String.Format("server=172.16.0.37\\quickcap;database={0};user Id=sa;password=SA0922!", strMSSQLDB);
                        //strMSSQLConString = "server=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBSERVER"].ToString() + ";database=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBNAME"].ToString() + ";user Id=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBUSERNAME"].ToString() + ";password=" + dsConfig.Tables[0].Rows[rowID]["MSSQLDBPASSWORD"].ToString();
                    }
                    rowID++;
                    if (!string.IsNullOrEmpty(strCompId))
                    {
                        DateTime? dttmDateTime = null;
                        //     string CompanyID = System.Configuration.ConfigurationSettings.AppSettings["CompanyID"].ToString();
                        //            //AuthIntegeration.ReferralWS.ReferralWS obj = new AuthIntegeration.ReferralWS.ReferralWS();
                        dttmDateTime = GetServiceLastRunTime("SCRUB", strMSSQLConString);
                        if (!(dttmDateTime != null && dttmDateTime.HasValue))
                        {
                            log.LogInformation("No Data");
                        }
                        else
                        {
                            //ReadData(dttmDateTime, strMSSQLConString);
                            ApplyData(strMSSQLConString);
                        }
                    }
                }
            }
        } // ClaimScrub


        public DateTime? GetServiceLastRunTime(string strCategory, string strMSSQLConString)
        {
            SqlDatabase sqlDB = new SqlDatabase(strMSSQLConString);
            DateTime? dttmDateTime = null;
//            Program.WriteLog(strMSSQLConString);
            {
                DbConnection connection = sqlDB.CreateConnection();
                connection.Open();

                DbCommand dbc = sqlDB.GetStoredProcCommand("usp_GetServiceLastRunDetails");

                sqlDB.AddInParameter(dbc, "@Service_Code", DbType.String, strCategory);

                try
                {
                    using (IDataReader reader = sqlDB.ExecuteReader(dbc))
                    {
                        while (reader.Read())
                        {
                            dttmDateTime = Convert.ToDateTime(reader["LASTRUNDATETIME"].ToString());
                        }
                    }

                }
                catch (Exception ex)
                {
                    //Program.WriteLog(ex.Message);
                    log.LogInformation(ex.Message);

                }
                finally
                {
                    connection.Close();
                }

                return dttmDateTime;
            }

        }

        public void ApplyData(string strMSSQLConString)
        {
            try
            {
                System.Collections.ArrayList CHDPFiles = new System.Collections.ArrayList();

                if (Directory.Exists(sOUTPath))
                {

                    string parentdir = sOUTPath.Replace(Path.GetDirectoryName(sOUTPath) + "\\", "");

                    foreach (string fileName in Directory.GetFiles(sOUTPath))
                        if (Path.GetExtension(fileName.Substring(0, fileName.Length - 8)).ToLower().Replace(".", "").IndexOf(OUTExtension) >= 0)
                        {
                            CHDPFiles.Add(fileName);
                        }

                    string sRecordPath = string.Empty;
                    string sDatepath = string.Empty;
                    string sDate = string.Empty;
                    DateTime dtDate = System.DateTime.MinValue;
                    //int val = 0;
                    for (int i = 0; i < CHDPFiles.Count; i++)
                    {
                        sDatepath = CHDPFiles[i].ToString();
                        dtDate = System.DateTime.MinValue;
                        ReadText(sDatepath, strMSSQLConString);
                        sRecordPath = isProcessComplete(true, CHDPFiles[i].ToString(), "1");
                    }
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
        }


        private string isProcessComplete(bool isSuccess, string filename, string pbatchName)
        {
            if (isSuccess)
            {
                try
                {
                    if (!Directory.Exists(MOVEPath))
                        Directory.CreateDirectory(MOVEPath);
                    File.Copy(filename, MOVEPath + "\\" + Path.GetFileName(filename), true);
                    log.LogInformation("File Processed successfully, Moved to " + MOVEPath + "\\" + Path.GetFileName(filename));
                    File.Delete(filename);
                }
                catch (Exception ex)
                {
                    log.LogError(ex.Message);

                }
                return MOVEPath + "\\" + Path.GetFileName(filename);

            }
            else
            {

            }
            return "";
        }

        public DataSet GetDataSet(string ConnectionString, string SQL)
        {
            SqlConnection conn = new SqlConnection(ConnectionString);
            SqlDataAdapter da = new SqlDataAdapter();
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = SQL;
            cmd.CommandType = CommandType.StoredProcedure;
            da.SelectCommand = cmd;
            DataSet ds = new DataSet();

            conn.Open();
            da.Fill(ds);
            conn.Close();

            return ds;
        }

        public void ReadData(DateTime? dttmDateTime, string strMSSQLConString1)
        {
            //<claim id="476737">
            //<claim-line>
            //<service-begin-date>2013-12-26</service-begin-date>
            //<service-end-date>2013-12-26</service-end-date>
            //<diagnostic-code>70211</diagnostic-code>
            //<diagnostic-code>216.5  </diagnostic-code>
            //<modifier>25</modifier>
            //<procedure-code>99203</procedure-code>
            //</claim-line>
            //<claim-line>
            //<service-begin-date>2013-12-26</service-begin-date>
            //<service-end-date>2013-12-26</service-end-date>
            //<diagnostic-code>2382</diagnostic-code>
            //<modifier></modifier>
            //<procedure-code>11100</procedure-code>
            //</claim-line>
            //<birth-date>1958-11-06</birth-date>
            //<service-date>2013-12-26</service-date>
            //<gender>M</gender>
            //<payer>MCR</payer>
            //<taxonomy>207ND0900X</taxonomy>
            //</claim>


            SqlDatabase sqlDB1 = new SqlDatabase(strMSSQLConString1);
            /*
            DbConnection connection = sqlDB1.CreateConnection();
            connection.Open();
            DataRow[] drColl = null;
            DbCommand dbc = sqlDB1.GetStoredProcCommand("usp_GetClaimsforScrub");

            sqlDB1.AddInParameter(dbc, "@ExecuteDateTime", DbType.String, dttmDateTime);
            */
            DbConnection connection = sqlDB1.CreateConnection();
            connection.Open();
            DataRow[] drColl = null;
            DbCommand dbc = sqlDB1.GetStoredProcCommand("usp_GetClaimsforScrub");

            sqlDB1.AddInParameter(dbc, "@ExecuteDateTime", DbType.String, dttmDateTime);


            try
            {
                DataSet dsResult = null;
//                if (company.CompareTo("PAV") == 0)
                dsResult = GetDataSet(strMSSQLConString1, "usp_GetClaimsforScrub");
                /*
                else
                {
                    connection = sqlDB1.CreateConnection();
                    connection.Open();
                    
                    dbc = sqlDB1.GetStoredProcCommand("usp_GetClaimsforScrub");

                    sqlDB1.AddInParameter(dbc, "@ExecuteDateTime", DbType.String, dttmDateTime);


                    dsResult = sqlDB1.ExecuteDataSet(dbc);
                }
                */
                string sXmlData = string.Empty;
                string sXMLHeader = string.Empty;
                string sClaim = string.Empty;

                string sSpec = string.Empty;
                string sFinalXML = string.Empty;
                if (dsResult != null && dsResult.Tables.Count > 0 && dsResult.Tables[0].Rows.Count > 0)
                {
                    sXMLHeader = "<?xml version='1.0' encoding='UTF-8'?>" + Environment.NewLine;
                    sXMLHeader += "<claims>" + Environment.NewLine;
                    foreach (DataRow dr in dsResult.Tables[0].Rows)
                    {
                        sClaim = @"<claim id=""" + Convert.ToString(dr["ClaimNo"]) + @""">" + Environment.NewLine;

                        drColl = dsResult.Tables[1].Select("Claimno='" + Convert.ToString(dr["Claimno"]) + "'", "RVU_SEQ_ORDER desc");
                        StringBuilder sDetail = new StringBuilder();
                        foreach (DataRow dr1 in drColl)
                        {
                            sDetail.Append("<claim-line diagnostic-type=\"icd10cm\">" + Environment.NewLine);
                            sDetail.Append("<service-begin-date>" + Convert.ToDateTime(dr1["ServiceDateFrom"]).ToString("yyyy-MM-dd") + "</service-begin-date>" + Environment.NewLine);
                            sDetail.Append("<service-end-date>" + Convert.ToDateTime(dr1["ServiceDateTo"]).ToString("yyyy-MM-dd") + "</service-end-date>" + Environment.NewLine);
                            sDetail.Append("<lineno>" + Convert.ToString(dr1["ClaimServiceID"]) + "</lineno>" + Environment.NewLine);
                            sDetail.Append("<diagnostic-code>" + Convert.ToString(dr1["DiagCode"]) + "</diagnostic-code>" + Environment.NewLine);
                            if (Convert.ToString(dr1["DiagCode2"]) != "")
                                sDetail.Append("<diagnostic-code>" + Convert.ToString(dr1["DiagCode2"]) + "</diagnostic-code>" + Environment.NewLine);
                            if (Convert.ToString(dr1["DiagCode3"]) != "")
                                sDetail.Append("<diagnostic-code>" + Convert.ToString(dr1["DiagCode3"]) + "</diagnostic-code>" + Environment.NewLine);
                            if (Convert.ToString(dr1["DiagCode4"]) != "")
                                sDetail.Append("<diagnostic-code>" + Convert.ToString(dr1["DiagCode4"]) + "</diagnostic-code>" + Environment.NewLine);
                            if (Convert.ToString(dr1["Modifier1"]) != "")
                                sDetail.Append("<modifier>" + Convert.ToString(dr1["Modifier1"]) + "</modifier>" + Environment.NewLine);
                            if (Convert.ToString(dr1["Modifier2"]) != "")
                                sDetail.Append("<modifier>" + Convert.ToString(dr1["Modifier2"]) + "</modifier>" + Environment.NewLine);
                            if (Convert.ToString(dr1["Modifier3"]) != "")
                                sDetail.Append("<modifier>" + Convert.ToString(dr1["Modifier3"]) + "</modifier>" + Environment.NewLine);
                            if (Convert.ToString(dr1["Modifier4"]) != "")
                                sDetail.Append("<modifier>" + Convert.ToString(dr1["Modifier4"]) + "</modifier>" + Environment.NewLine);
                            sDetail.Append("<procedure-code>" + Convert.ToString(dr1["ProcCode"]).Trim() + "</procedure-code>" + Environment.NewLine);
                            sDetail.Append("</claim-line>" + Environment.NewLine);

                        }
                        sClaim = sClaim + "<birth-date>" + Convert.ToDateTime(dr["DateOfBirth"]).ToString("yyyy-MM-dd") + "</birth-date>" + Environment.NewLine;
                        sClaim = sClaim + "<service-date>" + Convert.ToDateTime(dr["ServiceDateFrom"]).ToString("yyyy-MM-dd") + "</service-date>" + Environment.NewLine;
                        sClaim = sClaim + "<gender>" + Convert.ToString(dr["Gender"]) + "</gender>" + Environment.NewLine;
                        sClaim = sClaim + "<payer>" + Convert.ToString(dr["payer"]) + "</payer>" + Environment.NewLine;
                        sClaim = sClaim + "<taxonomy>" + Convert.ToString(dr["taxonomy"]) + "</taxonomy>" + Environment.NewLine;
                        //sClaim = sClaim + "</claim>" + Environment.NewLine;
                        sSpec = Convert.ToString(dr["ProvSpeciltyCode"]);
                        sFinalXML = sFinalXML + sClaim + sDetail.ToString() + "</claim>" + Environment.NewLine;
                    }
                    sFinalXML = sXMLHeader + sFinalXML + "</claims>";
                    WriteFile(sFinalXML, sINPath, Convert.ToString(sSpec.Substring(0, 1)), INExtension);
                }

            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

            }
            finally
            {
                connection.Close();
            }


        }

        private static void WriteFile(string Message, string sPath, string Spec, string Ext)
        {
            sPath = sPath + Spec + GetTimeStamp(DateTime.Now) + "." + Ext;
            FileStream fs = new FileStream(sPath, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter m_streamWriter = new StreamWriter(fs);
            m_streamWriter.BaseStream.Seek(0, SeekOrigin.End);
            m_streamWriter.WriteLine(Message);
            m_streamWriter.Flush();
            m_streamWriter.Close();
            m_streamWriter.Dispose();
        }

        private static string GetTimeStamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmss");
        }


        void ReadText(string sDatepath, string strMSSQLConString)
        {
            string inputString;
            StringBuilder resultLabel = new StringBuilder();
            string[] sReturn = null;
            string sClaimNo = string.Empty;
            string sClaimServiceID = string.Empty;
            string sCaution = string.Empty;
            string sCode = string.Empty;
            string sOther = string.Empty;
            string sDesc = string.Empty;
            string sFinalNotes = string.Empty;
            string sPercentage = string.Empty;
            string _strPrevClaimNo = string.Empty;
            string sClaimPercentage = string.Empty;
            try
            {
                using (StreamReader streamReader = File.OpenText(sDatepath))
                {
                    inputString = streamReader.ReadLine();
                    while (inputString != null)
                    {
                        resultLabel.Append(inputString + "<br />");
                        if (inputString.ToString() != "")
                        {
                            if (inputString.ToString().IndexOf(":") > 0)
                            {
                                //line 475716 : 2 : Caution : mM51 : Medicare Modifier 51 Required : Procedure code 11603 has been billed on the same DOS as another procedure without an appropriate modifier. Typically, procedures or services with the lower relative value should be reported with modifier 51: however, use of the 51 modifier may vary from payer to payer.  Please refer to the requirements of your individual claim payer.
                                //id 20161027837K00017 : Line 2 : Review : mMP :  : Procedure code 29515 is eligible for a multiple procedure reduction.
                                sReturn = inputString.Split(':');
                                if (sReturn.Length >= 5)
                                {
                                    //0
                                    sClaimNo = sReturn[0].Replace("id ", "").Trim(); //line 475716
                                    if (_strPrevClaimNo == "") _strPrevClaimNo = sClaimNo;
                                    if (sClaimNo != string.Empty && _strPrevClaimNo != sClaimNo)
                                    {
                                        if (sClaimServiceID == string.Empty)
                                            sClaimServiceID = "0";
                                        if (sClaimServiceID != "0")
                                            SaveNoes(strMSSQLConString, _strPrevClaimNo, sFinalNotes, sCode, sPercentage.Trim(), Convert.ToInt32(sClaimServiceID), sClaimPercentage);
                                        //_strPrevClaimNo = "";
                                        sFinalNotes = "";
                                        sClaimServiceID = "";
                                        sCode = "";
                                        sPercentage = "";
                                        sClaimPercentage = "";
                                    }
                                    sClaimServiceID = sReturn[1].Replace("Line ", "").Trim(); ; //2
                                    sCaution = sReturn[2]; //Caution
                                    sCode = sReturn[3]; //mM51
                                    sDesc = sReturn[4]; // Medicare Modifier 51 Required
                                    if (sReturn.Length > 5)
                                        sOther = sReturn[5];
                                    sFinalNotes = sFinalNotes + Environment.NewLine;
                                    sFinalNotes = sFinalNotes + " Claim Service ID: " + sClaimServiceID + Environment.NewLine;
                                    sFinalNotes = sFinalNotes + " Caution: " + sCaution + Environment.NewLine;
                                    sFinalNotes = sFinalNotes + " Code: " + sCode + Environment.NewLine;
                                    sFinalNotes = sFinalNotes + " Explanation: " + sDesc + Environment.NewLine;
                                    sFinalNotes = sFinalNotes + sOther + Environment.NewLine;
                                    if (sDesc.IndexOf("%") > 0)
                                    {
                                        sPercentage = sDesc.Substring(sDesc.IndexOf("%") - 3, 4);
                                        sClaimServiceID = sReturn[1].Replace("Line ", "").Trim(); ; //2
                                    }
                                    if (sOther.IndexOf("%") > 0)
                                    {
                                        sPercentage = sOther.Substring(sOther.IndexOf("%") - 3, 4);
                                        //sClaimServiceID = sReturn[1];
                                        sClaimPercentage = sClaimPercentage + sClaimServiceID + "|" + sPercentage.Trim() + ";";
                                    }
                                    _strPrevClaimNo = sClaimNo;
                                    //if (_strPrevClaimNo != sClaimNo)
                                    //{
                                    //    SaveNoes(strMSSQLConString, sClaimNo, sFinalNotes, sCode, sPercentage.Trim(), Convert.ToInt32(sClaimServiceID));
                                    //}
                                    //else
                                    //{

                                    //}
                                }
                            }
                            else
                            {
                                if (sFinalNotes != string.Empty)
                                {
                                    if (sClaimServiceID == string.Empty)
                                        sClaimServiceID = "0";
                                    if (sClaimServiceID != "0")
                                        SaveNoes(strMSSQLConString, sClaimNo, sFinalNotes, sCode, sPercentage.Trim(), Convert.ToInt32(sClaimServiceID), sClaimPercentage);
                                    sClaimNo = "";
                                    sFinalNotes = "";
                                    sClaimServiceID = "";
                                    sCode = "";
                                    sPercentage = "";
                                    sClaimPercentage = "";
                                    _strPrevClaimNo = sClaimNo;
                                }
                                //************ Claim 2014080583700003 is clean ******** 
                                if (inputString.ToString().IndexOf(" ") > 0)
                                {
                                    sReturn = inputString.Split(' ');
                                    if (sReturn.Length >= 4)
                                    {
                                        sClaimNo = sReturn[2]; //line 475716
                                        if (_strPrevClaimNo == "") _strPrevClaimNo = sClaimNo;
                                        sDesc = sReturn[4];
                                        if (sDesc == "clean")
                                        {
                                            SaveNoes(strMSSQLConString, sClaimNo, "", "", "", 0, "");
                                            _strPrevClaimNo = sClaimNo;
                                            sClaimNo = "";
                                        }
                                    }

                                }

                            }
                        }
                        inputString = streamReader.ReadLine();
                    }
                    //if (sFinalNotes != string.Empty)
                    //{
                    //    if (sClaimServiceID == string.Empty)
                    //        sClaimServiceID = "0";
                    //    if (sClaimServiceID != "0")
                    //        SaveNoes(strMSSQLConString, sClaimNo, sFinalNotes, sCode, sPercentage.Trim(), Convert.ToInt32(sClaimServiceID), sClaimPercentage);
                    //    sClaimNo = "";
                    //    sFinalNotes = "";
                    //    sClaimServiceID = "";
                    //    sCode = "";
                    //    sPercentage = "";
                    //    sClaimPercentage = "";
                    //    _strPrevClaimNo = sClaimNo;
                    //}
                }

                //if (resultLabel.ToString().IndexOf("clean") > 0)
                //{
                //    // do nothing
                //    sFinalNotes = "";
                //}
                //else
                //{
                if (sFinalNotes != string.Empty)
                {
                    if (sClaimServiceID == string.Empty)
                        sClaimServiceID = "0";
                    if (sClaimServiceID != "0")
                        SaveNoes(strMSSQLConString, sClaimNo, sFinalNotes, sCode, sPercentage.Trim(), Convert.ToInt32(sClaimServiceID), sClaimPercentage);
                    sClaimNo = "";
                    sFinalNotes = "";
                    sClaimServiceID = "";
                    sCode = "";
                    sPercentage = "";
                    sClaimPercentage = "";
                    _strPrevClaimNo = sClaimNo;
                }
                //SqlDatabase sqlDB = new SqlDatabase(strMSSQLConString);

                //DbConnection connection = sqlDB.CreateConnection();
                //connection.Open();

                //DbCommand dbc = sqlDB.GetStoredProcCommand("usp_ClaimScrubSaveNotes");

                //sqlDB.AddInParameter(dbc, "@ClaimNo", DbType.String, sClaimNo.Trim().Replace("line ", ""));
                //sqlDB.AddInParameter(dbc, "@Notes", DbType.String, sFinalNotes);

                //try
                //{
                //    sqlDB.ExecuteNonQuery(dbc);
                //}
                //catch (Exception ex)
                //{
                //    Program.WriteLog(ex.Message);

                //}
                //finally
                //{
                //    connection.Close();
                //}
                //}
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);

            }
        }

        public void SaveNoes(string strMSSQLConString, string sClaimNo, string sFinalNotes, string Code, string Percentage, int ClaimServiceID, string ClamPer)
        {
            SqlDatabase sqlDB = new SqlDatabase(strMSSQLConString);

            DbConnection connection = sqlDB.CreateConnection();
            connection.Open();

            DbCommand dbc = sqlDB.GetStoredProcCommand("usp_ClaimScrubSaveNotes");

            sqlDB.AddInParameter(dbc, "@ClaimNo", DbType.String, sClaimNo.Trim().Replace("line ", ""));
            sqlDB.AddInParameter(dbc, "@Notes", DbType.String, sFinalNotes);
            sqlDB.AddInParameter(dbc, "@Code", DbType.String, Code);
            sqlDB.AddInParameter(dbc, "@ClaimServiceID", DbType.String, ClaimServiceID);
            sqlDB.AddInParameter(dbc, "@Percentage", DbType.String, Percentage);
            sqlDB.AddInParameter(dbc, "@ClaimNotesPercentage", DbType.String, ClamPer);
            try
            {
                sqlDB.ExecuteNonQuery(dbc);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
            }
            finally
            {
                connection.Close();
            }
        }


    } // class QuickCap
}
