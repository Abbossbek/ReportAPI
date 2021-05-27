using DevExpress.DataAccess.Json;
using DevExpress.XtraReports.UI;

using ReportAPI.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Mvc;

namespace ReportAPI.Controllers
{
    public class ReportController : ApiController
    {
        // POST: api/Report
        public FileResult Post([FromBody]Report report)
        {
            if (report != null)
            {
                XtraReport xtraReport = new XtraReport();
                switch (report.Type)
                {
                    case "type1":
                        xtraReport.LoadLayout(System.Web.Hosting.HostingEnvironment.MapPath(@"~/TypeFiles/type1.repx"));
                        break;
                    default:
                        break;
                }

                var jsonDataSource = new JsonDataSource();
                jsonDataSource.JsonSource = new CustomJsonSource(report.Data);
                // Populate the data source with data.
                jsonDataSource.Fill();

                xtraReport.ComponentStorage.AddRange(new System.ComponentModel.IComponent[] {
            jsonDataSource});
                xtraReport.DataSource = jsonDataSource;
                MemoryStream memoryStream = new MemoryStream();
                xtraReport.SaveDocument(memoryStream);
                return new FileStreamResult(memoryStream, "application/prnx") { FileDownloadName="report.prnx"};
            }
            else
            {
                return null;
            }
        }

    }
}
