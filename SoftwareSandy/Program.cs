//#define RUN_IN_DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using System.Data;
using System.IO;
using System.Net;
using System.Xml.XPath;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web;
using System.Diagnostics;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Configuration;

using System.Threading.Tasks;



namespace SoftwareSandy
{
    //class Program : ServiceBase
    class Program 
    {

        static void Main(string[] args)
        {
            // ServiceBase.Run(new Program());

            // SoftwareSandy.Program p = new Program();
            log = new LogLogger("Application", "SoftwareSandy Claims Scrubber");


            //p.ParseFiles();
            ParseFiles();

        }

        static LogLogger log;

#if RUN_AS_SERVICE

                StateObjClass StateObj;
        protected override void OnStart(string[] args)
        {
            //log = new LogLogger("Application", "SoftwareSandy Claims Scrubber");
            log.LogInformation("Starting");
            RunTimer();
            base.OnStart(args);
        }


        protected override void OnStop()
        {
            StateObj.TimerCanceled = true;
            base.OnStop();
        }

        public Program()
        {
            this.ServiceName = "SoftwareSandy Claims Scrubber";
        }
        

        public void RunTimer()
        {
            claimLinesList = new Dictionary<string, ClaimLinesIndex>();
            StateObj = new StateObjClass();
            StateObj.TimerCanceled = false;
            StateObj.TimerBusy = 0;
            StateObj.SomeValue = 1;
            System.Threading.TimerCallback TimerDelegate =
                new System.Threading.TimerCallback(TimerTask);

            // Create a timer that calls a procedure every 2 seconds. 
            // Note: There is no Start method; the timer starts running as soon as  
            // the instance is created.
            System.Threading.Timer TimerItem =
                  new System.Threading.Timer(TimerDelegate, StateObj, 10000, 10800000); // Changed timer to every 3 hours
            // Save a reference for Dispose.
            StateObj.TimerReference = TimerItem;
        }

        private void TimerTask(object StateObj)
        {
            try
            {
                StateObjClass State = (StateObjClass)StateObj;
                // Use the interlocked class to increment the counter variable.
                System.Threading.Interlocked.Increment(ref State.SomeValue);
                log.LogInformation("Launched new thread  " + DateTime.Now.ToString());
                //System.Diagnostics.Debug.WriteLine("Launched new thread  " + DateTime.Now.ToString());
                if (State.TimerCanceled)
                // Dispose Requested.
                {
                    State.TimerReference.Dispose();
                    log.LogInformation("Done  " + DateTime.Now.ToString());
                    //System.Diagnostics.Debug.WriteLine("Done  " + DateTime.Now.ToString());
                }
                else
                {
                    log.LogInformation("Looking to Parse files");
                    if (State.TimerBusy == 0)
                    {
                        System.Threading.Interlocked.Add(ref State.TimerBusy, 1);
                        log.LogInformation(String.Format("Before Parsing Files {0}", State.TimerBusy));
                        if (!IsProcessRunning())
                        {
                            log.LogInformation(String.Format("Locking {0}", State.TimerBusy));
                            //if (LockProcess())
                              ParseFiles(); // Only SoftwareSandy is running now
                            log.LogInformation(String.Format("Releasing the lock {0}", State.TimerBusy));
                            //ReleaseLock();
                        }
                        System.Threading.Interlocked.Add(ref State.TimerBusy, -1);
                        log.LogInformation(String.Format("After Parsing Files {0}", State.TimerBusy));
                    }
                }
                claimLinesList.Clear();
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("In Timer Task {0}", ex.Message));
            }

        }

        static bool IsProcessRunning()
        {
            string fileLockName = ConfigurationManager.AppSettings["NetworksLocation"] + @"\claims.lck";
            if (File.Exists(fileLockName))
                return true;
            else
                return false;
        }

        static bool LockProcess()
        {
            string fileLockName = ConfigurationManager.AppSettings["NetworksLocation"] + @"\claims.lck";
            if (File.Exists(fileLockName))
                return false;
            else
            {
                try
                {
                    StreamWriter sw = File.CreateText(fileLockName);
                    sw.Close();
                }
                catch (Exception ex)
                {
                    log.LogError(String.Format("In LockProcess {0}", ex.Message));
                }

                return true;
            }
        }

        static void ReleaseLock()
        {
            string fileLockName = ConfigurationManager.AppSettings["NetworksLocation"] + @"\claims.lck";
            try
            {
                if (File.Exists(fileLockName))
                    File.Delete(fileLockName);
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("In ReleaseLock {0}",ex.Message));
            }
        }

                private class StateObjClass
        {
            // Used to hold parameters for calls to TimerTask. 
            public int SomeValue;
            public System.Threading.Timer TimerReference;
            public bool TimerCanceled;
            public int TimerBusy;
        }


#endif



        static Dictionary<string, ClaimLinesIndex> claimLinesList;

        struct ClaimLinesIndex
        {
            public string claim;
            public string key;
            public string lineNo;
            public string claimIndex;
            public bool hasDU;
            public string percentage;
            public string cpt;
            public string diag;
        };

        struct Findings
        {
            public string id;
            public string claimIndex;
            public string editType;
            public string mnemonic;
            public string editConflit;
            public string description;
            public string lineNo;
            public string claimNo;
        };

        static string gCompany = "";

        static List<Findings> optumFindings;

        static public void ParseFiles()
        {
            QuickCap qc = new QuickCap();
            qc.log = log;




            log = new LogLogger("Application", "SoftwareSandy Claims Scrubber");
            log.LogInformation("Starting");
            claimLinesList = new Dictionary<string, ClaimLinesIndex>();
            qc.ClaimScrubRead();

            //log.LogInformation("Parsing Files");
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings["SQLConnection"];
            SqlConnection con = new SqlConnection(settings.ConnectionString);
            using (con)
            {
                try
                {
                    con.Open();
                    GetCPTExclusions(con);
                }
                catch (SqlException sex)
                {
                    log.LogError(String.Format("Cannot connect {0}",sex.Message));
                }
                string _directories = ConfigurationManager.AppSettings["NetworksLocation"];
                string[] networks = getNetworks(_directories);
                for (int i = 0; i < networks.Length; i++)
                {
                    gCompany = networks[i].Substring(_directories.Length + 1);
                    FixPercentages();
                    log.LogInformation("Deliting previous working files");
                    string[] files = Directory.GetFiles(networks[i] + @"\WORKING");
                    if (files.Length > 0)
                        for (int k = 0; k < files.Length; k++)
                            File.Delete(files[k]);
                    files = Directory.GetFiles(networks[i] + @"\IN");
                    // Looking for files in the "IN" directory
                    if (files.Length > 0)
                    {
                        log.LogInformation("Spliting files");
                        for (int j = 0; j < files.Length; j++)
                        {
                            try
                            {
                                SplitFiles(files[j], networks[i], con);
                            }
                            catch(Exception ex)
                            {
                                log.LogError(String.Format("Error Spliting Files {0}", ex.Message));
                            }
                        }

                        for (int j = 0; j < files.Length; j++)
                        {

                            try
                            {
                                if (File.Exists(files[j].Replace(@"\IN", @"\PROCESS")))
                                {
                                    log.LogInformation(String.Format("Deleting file {0}", files[j].Replace(@"\IN", @"\PROCESS")));
                                    File.Delete(files[j].Replace(@"\IN", @"\PROCESS"));
                                }

                                File.Move(files[j], files[j].Replace(@"\IN", @"\PROCESS"));
                            }
                            catch (Exception ef)
                            {
                                log.LogError(String.Format("Moving files {0} {1}", files[j], ef.Message));
                            }
                        }

                        files = Directory.GetFiles(networks[i] + @"\WORKING");
                        for (int j = 0; j < files.Length; j++)
                        {
                            log.LogInformation("Sending File: " + files[j]); // had bug using the wrong index
                            try
                            {
                                SendFiles(files[j], networks[i]);
                            }
                            catch (Exception ex)
                            {
                                log.LogError(String.Format("Outside SendFiles {0}", ex.Message));
                            }
                        }

                        if (claimLinesList.Count() > 0)
                            claimLinesList.Clear();
                    }
                }
            }
            qc.ClaimScrubWrite();
        }

        static void MergeFindings(Dictionary<string, Findings> _dic, Dictionary<string, ClaimLinesIndex> _claimLinesList, List<string> _thisRun )
        {
            string lastClaim = String.Empty;
            int errLine = 0;
            foreach (var pair in _claimLinesList)
            {
                ClaimLinesIndex _cli = pair.Value;
                if (!_thisRun.Contains(_cli.claim))
                    continue;
                if (_cli.claim.CompareTo(lastClaim) != 0)
                    errLine = 0;
                if (_cli.percentage != null)
                {
                    Findings findings = new Findings();
                    findings.id = _cli.claim;
                    findings.claimIndex = _cli.claimIndex;
                    findings.editType = "Review";
                    findings.mnemonic = "mMP"; 
                    findings.editConflit = "mMP";
                    findings.description = "PNS4 Multiple Procedure Reduction " + _cli.percentage;
                    try
                    {
                        _dic.Add(String.Format("{0}{1}{2}", _cli.claim, _cli.claimIndex, (++errLine).ToString()), findings);
                    }catch(Exception mex)
                    {
                        log.LogError("MergeFindings " + mex.Message);
                    }

                }
            }
        }

        

        static void SendFiles(string _fileName, string _dir)
        {
            TextWriter tw = null;

            var dic = new Dictionary<string, Findings>();
            if (optumFindings == null)
                optumFindings = new List<Findings>();
            else
                optumFindings.Clear();


            List<string> thisRun = new List<string>();
            int loop = 0;
            int lineCounter = 0;
            try
            {
                log.LogInformation(String.Format("In SendFiles {0} {1}", _fileName, _dir));
                string user = "ProviderNetworkSolutions";
                string password = "PezJVHC3{";
                string claimsDotXml = _fileName;
                WebRequest req = WebRequest.Create("https://realtimeecontent.com/ws/claims5x");

                req.Method = "POST";
                req.ContentType = "application/xml";
                req.Credentials = new NetworkCredential(user, password);
                FileStream fs = new FileStream(claimsDotXml, FileMode.Open);
                string outFile = String.Format(@"{0}\OUT\{1}.out", _dir, Path.GetFileName(_fileName));
                string outFile2 = String.Format(@"{0}\OUT\{1}2.out", _dir, Path.GetFileName(_fileName));

                if (File.Exists(outFile))
                {
                    File.Delete(outFile);
                }

                log.LogInformation(String.Format("Creating File {0} ", outFile));
                tw = File.CreateText(outFile);


                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                using (Stream reqStream = req.GetRequestStream())
                {
                    byte[] buffer = new byte[1024];
                    int bytesRead = 0;
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        reqStream.Write(buffer, 0, bytesRead);
                    }
                    fs.Close();
                }

                System.Net.HttpWebResponse resp = req.GetResponse() as System.Net.HttpWebResponse;
                if (resp.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    System.Xml.XPath.XPathDocument xmlDoc = new System.Xml.XPath.XPathDocument(resp.GetResponseStream());
                    System.Xml.XPath.XPathNavigator nav = xmlDoc.CreateNavigator();
                    //Console.WriteLine("Claim A5 had {0} Edits.", nav.Evaluate("count(/claim-responses/claim-response[@claim-id='43']/edit)"));
                    //Console.WriteLine(nav.Evaluate("/claim-responses/claim-response/edit"));
                    lineCounter++;
                    loop = 1;
                    XPathNodeIterator xPathIterator = nav.Select("/claim-responses//claim-response");
                    int errLine = 0;
                    int lastConflictFound = 0, iClaimLine = 0;
                    string lastClaim = "";
                    foreach (XPathNavigator claimResponse in xPathIterator)
                    {
                        XPathNavigator onav = claimResponse.SelectSingleNode("@claim-id");
                        string id = onav == null ? string.Empty : onav.Value;

                        try {
                            thisRun.Add(id);
                        }catch(Exception mex)
                        {
                            log.LogError(mex.Message);
                        }

                        if (id.CompareTo("20170309T33K00018") == 0)
                            errLine = 0;

                        XPathNodeIterator ixPathIterator = claimResponse.Select("edit");
                        bool hasEdits = false;
                        int noOfEdits = 0;
                        foreach (XPathNavigator editLines in ixPathIterator)
                        {
                            hasEdits = true;
                            noOfEdits++;
                            onav = editLines.SelectSingleNode("@line");
                            string claimLine = onav == null ? string.Empty : onav.Value;
                            string key = id.Trim() + "|" + claimLine.Trim();

                            ClaimLinesIndex _cli;
                            try
                            {
                                iClaimLine = int.Parse(claimLine);
                            }
                            catch  (Exception ex)
                            {
                                iClaimLine = 0;
                            }
                            if( iClaimLine > lastConflictFound)
                            {
                                
                                string myId="";
                                lastConflictFound = iClaimLine;
                            }

                            XPathNavigator inav = editLines.SelectSingleNode("description");
                            string description = editLines == null ? string.Empty : inav.Value;

                            //if (description.Contains("[Pattern 10372]"))
                            //    hasEdits = false;

                            //inav = editLines.SelectSingleNode("edit-conflict");
                            string editConflict = ""; // editLines == null ? string.Empty : inav.Value;

                            inav = editLines.SelectSingleNode("edit-type");
                            string editType = editLines == null ? string.Empty : inav.Value;

                            inav = editLines.SelectSingleNode("mnemonic");
                            string mnemonic = editLines == null ? string.Empty : inav.Value;


                            Findings opFind = new Findings();
                            opFind.id = id;
                            opFind.lineNo = claimLine;
                            opFind.editType = editType;
                            opFind.mnemonic = mnemonic;
                            opFind.editConflit = editConflict;
                            opFind.description = description;
                            optumFindings.Add(opFind);


                            if (!claimLinesList.ContainsKey(key))
                            {
                                log.LogError(String.Format("The claim does not contain {0}", key));
                                _cli = new ClaimLinesIndex();
                                _cli.claim = id;
                                _cli.hasDU = true;
                                _cli.lineNo = "-99";
                                _cli.claimIndex = "-99";
                            }
                            else
                                _cli = claimLinesList[key];
                            if( !_cli.hasDU )
                            {
                                if (!description.Contains("50.0%")) // Ignore Optum 
                                {
                                    Findings findings = new Findings();
                                    findings.id = id;
                                    findings.claimIndex = _cli.claimIndex;
                                    findings.editType = editType;
                                    findings.mnemonic = mnemonic;
                                    findings.editConflit = editConflict;
                                    findings.description = description;
                                    try {
                                        dic.Add(String.Format("{0}{1}{2}", id, _cli.claimIndex, (++errLine).ToString()), findings);
                                    }catch(Exception mex)
                                    {
                                        log.LogError(mex.Message);
                                    }
                                }else
                                {
                                    if (noOfEdits == 1) // this is the only edit for the claim and it's a wrong one mMP
                                        hasEdits = false;
                                    noOfEdits--;
                                }
                            }
                        }
                        if( hasEdits == false)
                        { // claim does not have any edits
                            //string percentage = "";
                            //ClaimLinesIndex _cli;
                            //int found = 0;

                            //if (found == 0)
                            //{
                            if (IsCleanClaim(id, claimLinesList))
                            {
                                Findings findings = new Findings();
                                findings.id = id;
                                findings.claimIndex = "0";
                                findings.editType = "";
                                findings.mnemonic = "";
                                findings.editConflit = "";
                                findings.description = "";
                                try {
                                    dic.Add(String.Format("{0}{1}{2}", id, findings.claimIndex, (++errLine).ToString()), findings);
                                }catch(Exception mex)
                                {
                                    log.LogError(mex.Message);
                                }
                            }

                                //tw.WriteLine("************ Claim {0} is clean ********", id);
                            //}
                        }
                    }
                    //FinishTheEdits(lastConflictFound - 1, lastClaim, claimLinesList, dic, errLine);
                }
                ReviewClaimLineList(tw);
                /*
                //
                // I may need to discard the claims that are not in this batch as I am adding every result again
                //
                loop = 2;
                MergeFindings(dic, claimLinesList, thisRun);
                var list = dic.Keys.ToList();
                list.Sort();
                lineCounter = 0;
                
                int pattern10372 = 0;
                int numberOfEditLines = 0;
                string lastClaimInLoop = string.Empty;

                // Loop through keys.
                foreach (var key in list)
                {
                    lineCounter++;
                    Findings findings = dic[key];

                    if(findings.id.CompareTo(lastClaimInLoop) != 0 && numberOfEditLines == pattern10372 && numberOfEditLines > 0)
                    {
                        tw.WriteLine("************ Claim {0} is clean ********", lastClaimInLoop);
                    }
                    if (findings.id.CompareTo(lastClaimInLoop) != 0)
                    {
                        pattern10372 = 0;
                        numberOfEditLines = 0;
                    }

                    if (findings.claimIndex.Equals("0") && IsCleanClaim(findings.id, claimLinesList) ) 
                    {
                        tw.WriteLine("************ Claim {0} is clean ********", findings.id);
                        numberOfEditLines = 0;
                        pattern10372 = 0;
                    }
                    else
                    if (!findings.claimIndex.Equals("0"))
                    {
                        string mMp = findings.editType;
                        bool writeRecord = true;
                        //if (findings.description.Contains("multiple procedure reduction") || findings.description.Contains("50%"))
                        if ( findings.description.Contains("50%") && !findings.description.Contains("150%") )
                        {
                            mMp = "mMP";
                            if (!findings.description.StartsWith("PNS"))
                                writeRecord = false;
                            //findings.description += " 50%";
                        }
                        numberOfEditLines++;
                        if (findings.description.Contains("[Pattern 10372]"))
                            pattern10372++;

                        if (writeRecord && !findings.description.Contains("[Pattern 10372]") )
                            tw.WriteLine("id {0} : Line {1} : {2} : {3} : {4} : {5}", findings.id,
                                findings.claimIndex, findings.editType, mMp, findings.editConflit, findings.description);
                    }
                    
                    lastClaimInLoop = findings.id;
                }
                if ( numberOfEditLines == pattern10372  )
                    tw.WriteLine("************ Claim {0} is clean ********", lastClaimInLoop );
                    */
                optumFindings.Clear();
                dic.Clear();
                dic = null;
                tw.Close();
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("Sending Files {0}", ex.Message));
            }
            finally
            {
                //claimLinesList.Clear();
                thisRun.Clear();
            }

        }

        static bool IsCleanClaim ( string _claimNo, Dictionary<string, ClaimLinesIndex> _claimLinesList)
        {
            bool findings = true;
            //string _claimNo = _findings.id;

            string startId = (_claimNo + "|0").ToString();
            string endId = _claimNo + "|z";
            int results = 0;
            if (_claimNo.Equals("20170309T33K00018"))
                results = 0;
            foreach (var pair in _claimLinesList)
                if (pair.Key.CompareTo(_claimNo) >= 0 && pair.Key.CompareTo(endId) < 0)
                {
                    ClaimLinesIndex _cli = pair.Value;
                    // if (!_cli.claimIndex.Equals("0") )
                    //    results++;
                    if (_cli.hasDU)
                        findings = false;
                }
            if ( results > 1 )
                findings = false;
            return  findings;
        }

        static void FinishTheEdits(int _lastConflict, string _claimNo, Dictionary<string, ClaimLinesIndex> _claimLinesList, Dictionary<string, Findings> _dic, int errLine)
        {
            string startId = _claimNo + "|" + (_lastConflict + 1 ).ToString();
            string endId = _claimNo + "|z";
            foreach (var pair in _claimLinesList)
                if (pair.Key.CompareTo(startId) >= 0   && pair.Key.CompareTo(endId) < 0)
                {
                    ClaimLinesIndex _cli = pair.Value;
                    if (_cli.hasDU)
                    {
                        Findings findings = new Findings();
                        findings.id = _claimNo;
                        findings.claimIndex = _cli.claimIndex;
                        findings.editType = "Review";
                        findings.mnemonic = "mMP";
                        findings.editConflit = "mMP";
                        findings.description = "PNS3 Multiple Procedure Reduction " + _cli.percentage;
                        try
                        {
                            _dic.Add(String.Format("{0}{1}{2}", _claimNo, _cli.claimIndex, (++errLine).ToString()), findings);
                        }catch(Exception mex)
                        {
                            log.LogError("In FinishTheEdits " + mex.Message);
                        }
                    }

                }
        }

        static string p2 = "50%", p3 = "50%", p4 = "50%", p5 = "50%", p6 = "25%";

        static void FixPercentages()
        {
            switch (gCompany)
            {
                case "DNSAVMED":
                case "DNSBCBS":
                case "DNSMAGELLAN":
                case "DNSF":
                case "DNSHUMANA":
                case "DNSWC":
                case "PHN":
                case "PMA": p2 = "50%"; p3 = "50%"; p4 = "50%"; p5 = "50%"; p6 = "25%";
                    break;
                default:    p2 = "50%"; p3 = "25%"; p4 = "25%"; p5 = "25%"; p6 = "25%";
                    break;
            }
        }
        static void SplitFiles(string _file, string _network, SqlConnection _con)
        {
            TextReader tr = File.OpenText(_file);
            string fileName = Path.GetFileName(_file);
            int fileCount = 1;
            log.LogInformation(String.Format("Spliting File {0}", _file));
            string workingFile = String.Format(@"{0}\WORKING\{1}.{2}", _network, fileName, fileCount.ToString("000"));
            TextWriter tw = null;
            try
            {
                tw = File.CreateText(workingFile);
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("SplitFiles Create File {0}", ex.Message));
            }
            string line = "";
            int lineNo = 0;
            int claimCount = 0;
            ClaimLinesIndex _cl;
            _cl = new ClaimLinesIndex();
            _cl.claim = null;
            _cl.hasDU = false;
            string diagnosis = "";
            string procedure = "";
            string description = "";
            int icdVersion = 9;
            log.LogInformation(String.Format("Working file {0}", workingFile));
            int surgicalCount = 0;
            string surguryStarts = "123456";
            string lastServiceDate = "";

            try
            {

                while ((line = tr.ReadLine()) != null)
                {
                    line = line.TrimStart();
                    if (line.StartsWith(@"<?xml") || line.StartsWith(@"<claims"))
                        tw.WriteLine(line);
                    else
                    {
                        if (line.StartsWith(@"<icd9v1></icd9v1>") || line.StartsWith(@"<modifier></modifier>") || line.StartsWith(@"<icd10cm></icd10cm") )
                        {
                            continue;
                        }
                        if (!line.StartsWith("<lineno"))
                            tw.WriteLine(line);

                        if (line.Contains(@"icd9v1") || line.Contains(@"icd10cm"))
                        {
                            if (line.Contains(@"icd10cm"))
                                icdVersion = 10;
                            else
                                icdVersion = 9;

                            //diagnosis = GetValue(line);
                        }
                        if( line.StartsWith("<service-begin-date>") )
                            if( lastServiceDate.CompareTo(GetValue(line)) != 0)
                            {
                                lastServiceDate = GetValue(line);
                                if( surgicalCount > 0)
                                    surgicalCount = 0;
                            }
                        if (line.StartsWith("<diagnostic-code>"))
                        {
                            diagnosis = GetValue(line);
                            _cl.diag = diagnosis;
                        }

                        if (line.StartsWith(@"<procedure-code"))
                        {
                            procedure = GetValue(line);
                            _cl.cpt = procedure;
                            _cl.hasDU = false;

                            // this is a surgery
                            if ( surguryStarts.IndexOf(procedure.Substring(0,1))  >= 0  )
                            {
                                if( !gCPTExclusions.Contains(procedure) )
                                {
                                    surgicalCount++;
                                    if( surgicalCount > 1)
                                    {
                                        _cl.hasDU = true;
                                        _cl.percentage = "";
                                        switch (surgicalCount)
                                        {
                                            case 1: /* doing nothing here to avoid default */
                                                break;
                                            case 2: _cl.percentage = p2;
                                                break;
                                            case 3: _cl.percentage = p3;
                                                break;
                                            case 4: _cl.percentage = p4;
                                                break;
                                            case 5: _cl.percentage = p5;
                                                break;
                                            default: _cl.percentage = p6;
                                                break;
                                        }
                                    }
                                }
                            }

                        }

                        if (line.StartsWith(@"<claim id="))
                        {
                            _cl = new ClaimLinesIndex();
                            _cl.claim = GetClaimID(line);
                            if (_cl.claim.CompareTo("20170309T33K00018") == 0)
                                _cl.hasDU = false;
                            _cl.hasDU = false;
                            _cl.percentage = null;
                            lineNo = 0;
                            claimCount++;
                            surgicalCount = 0;
                            lastServiceDate = "";
                        }

                        if (line.StartsWith("<claim-line"))
                        {
                            diagnosis = "";
                            procedure = "";
                            description = "";
                            lineNo++;
                        }


                        if (line.StartsWith("<lineno"))
                        {
                            _cl.claimIndex = GetClaimIndex(line);
                            _cl.lineNo = lineNo.ToString();
                            _cl.key = String.Format("{0}|{1}", _cl.claim, _cl.lineNo);
                            _cl.hasDU = false;
                            _cl.percentage = null;
                            continue;
                        }

                        if (line.StartsWith(@"</claim-line"))
                            claimLinesList.Add(_cl.key, _cl);




                        if (claimCount == 100 && line.StartsWith(@"</claim>"))
                        {
                            tw.WriteLine(@"</claims>");
                            tw.Flush();
                            tw.Close();
                            workingFile = String.Format(@"{0}\WORKING\{1}.{2}", _network, fileName, (++fileCount).ToString("000"));
                            tw = File.CreateText(workingFile);
                            tw.WriteLine(@"<?xml version='1.0' encoding='UTF-8'?>");
                            tw.WriteLine(@"<claims>");
                            claimCount = 0;
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("Exception in SplitFiles {0}", ex.Message));
            }
            tw.Close();
            tr.Close();
        }

        static string GetValue(string _lineICD)
        {
            string[] a = _lineICD.Split('>');
            if (a.Length < 2)
                log.LogError(String.Format("In GetValue {0}", _lineICD));
            else
                a = a[1].Split('<');
            return a[0];
        }

        private static void AddCrossCheckToDB(string _ctp, string _description, string _taxonomy, SqlConnection con1)
        {
            try
            {
                SqlCommand cmdAdd = new SqlCommand("p_InsertCrossCode", con1);
                cmdAdd.CommandType = CommandType.StoredProcedure;
                cmdAdd.Parameters.Add("@Taxonomy", SqlDbType.VarChar, 20).Value = _taxonomy;
                cmdAdd.Parameters.Add("@CPT", SqlDbType.VarChar, 20).Value = _ctp; ;
                cmdAdd.Parameters.Add("@ICDS", SqlDbType.VarChar, 1024).Value = _description;
                cmdAdd.CommandType = CommandType.StoredProcedure;
                cmdAdd.ExecuteNonQuery();
            }
            catch (SqlException sex)
            {
                log.LogError(sex.Message);
            }
        }

        static string GetClaimID(string _claimtag)
        {
            string[] a = _claimtag.Split('=');
            if (a.Length < 2)
                log.LogError(String.Format("In GeetClaimID {0}", _claimtag));
            return a[1].Replace("\"", "").Replace(">", "");
        }

        static string GetClaimIndex(string _indexLine)
        {
            string[] a = _indexLine.Split('>');
            if( a.Length < 2 )
                log.LogError(String.Format("In GetClaimIndex {0}", _indexLine));
            a = a[1].Split('<');
            return a[0];
        }

        static string[] getNetworks(string _dir)
        {
            return Directory.GetDirectories(_dir);
        }

        static string gCPTExclusions = "";

        private static void GetCPTExclusions(SqlConnection _con)
        {
            try
            {
                SqlCommand cmdGet = new SqlCommand("SELECT CPT FROM callcenter..CPTExclusions", _con);
                SqlDataReader dr = cmdGet.ExecuteReader();
                while (dr.Read())
                    gCPTExclusions += dr.GetString(0) + "|";
                dr.Close();
                dr.Dispose();
                cmdGet.Dispose();
            }
            catch (SqlException sex)
            {
                log.LogError(sex.Message);
            }
        }

        private static string CrossCheckDB(string _cpt, SqlConnection _con)
        {
            string description = "";
            try
            {
                SqlCommand cmdGet = new SqlCommand("p_GetCrossCode", _con);
                cmdGet.CommandType = CommandType.StoredProcedure;
                cmdGet.Parameters.Add("@CPT", SqlDbType.VarChar, 20).Value = _cpt;
                SqlDataReader dr = cmdGet.ExecuteReader();
                if (dr.Read())
                    if (!dr.IsDBNull(0))
                        description = dr.GetString(0);
                dr.Close();
            }
            catch (SqlException sex)
            {
                log.LogError(sex.Message);
            }
            return description;
        }


        private static string CrossCheckWeb(string _cpt, int _icdVersion)
        {
            //Uri uri = new Uri("https://eprotest.uhc.com/ws/codetype/cpt/" + cptcode + "/properties?data=desc-full");
            Uri uri;
            if (_icdVersion == 9 )
                uri = new Uri("https://realtimeecontent.com/ws/codetype/cpt/" + _cpt + "/icd9v1");
            else
                uri = new Uri("https://realtimeecontent.com/ws/codetype/cpt/" + _cpt + "/icd10cm");
            string user = "ProviderNetworkSolutions";
            string password = "PezJVHC3{";
            //string claimsDotXml = tbFileName.Text.Replace(".txt", ".xml");
            string description = "";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Credentials = new NetworkCredential(user, password);
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                XPathDocument xml = new XPathDocument(response.GetResponseStream());
                XPathNavigator navigator = xml.CreateNavigator();
                XPathNodeIterator nodeIter = navigator.Select("//link");
                while (nodeIter.MoveNext())
                {
                    XPathItem node = nodeIter.Current;
                    description += node.Value + "|";
                    // Console.WriteLine(description);

                }
            }
            catch (Exception ex)
            {
                log.LogError(String.Format("In CrossCheckWeb {0} {1}", ex.Message, uri));
            }
            return description;

        }

        private static void ReviewClaimLineList(TextWriter tw)
        {

            var completeFindings = (from claims in claimLinesList
                                   join findings in optumFindings
                         on 
                            new { p1= (string) claims.Value.claim, p2 = (string) claims.Value.lineNo } 
                         equals 
                            new { p1 = (string) findings.id, p2 = (string) findings.lineNo } into gj
                                    from subFinding in gj.DefaultIfEmpty()
                         select new { claims, subFinding.description, subFinding.editConflit,subFinding.editType, subFinding.claimIndex
                         }).ToList();

            string previousClaim = "";
            bool recordWritten = false;
            string lastKey = "";
            foreach (var item in completeFindings.OrderBy( k => k.claims.Key) )
            {
                if (previousClaim == "")
                    previousClaim = item.claims.Value.claim;

                // If the previous claim did not have any edits/findings
                // then write it as a clean claim
                if (previousClaim.CompareTo(item.claims.Value.claim) != 0)
                {
                    if (recordWritten == false)
                        tw.WriteLine("************ Claim {0} is clean ********", previousClaim);
                    recordWritten = false;
                    previousClaim = item.claims.Value.claim;
                }
                // thesea re the findings from optum
                if (item.description != null)
                {
                    tw.WriteLine("id {0} : Line {1} : {2} : {3} : {4} : {5}", item.claims.Value.claim,
                    item.claims.Value.claimIndex, item.editType, "", item.editConflit, item.description.Replace("%","") );
                    recordWritten = true;
                }
                // these are my findings
                if (item.claims.Value.percentage != null && lastKey.CompareTo( item.claims.Key) != 0)
                {
                    tw.WriteLine("id {0} : Line {1} : {2} : {3} : {4} : {5}", item.claims.Value.claim,
                    item.claims.Value.claimIndex, "Review", "mMP", "mMP", "PNS4 Multiple Procedure Reduction " + item.claims.Value.percentage);
                    recordWritten = true;
                }
                lastKey = item.claims.Key;
            }
            // last claim make sure we write it as clean if there were no findings
            if( recordWritten == false)
            {
                tw.WriteLine("************ Claim {0} is clean ********", previousClaim);
            }
        }

    }
}
