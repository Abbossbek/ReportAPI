using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;

using MessagePack;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using ReportAPI.Models;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Http;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace ReportAPI.Controllers
{
    public class ConvertController : ApiController
    {
        [System.Web.Http.HttpGet]
        [System.Web.Http.Route("api/GetFile")]
        public HttpResponseMessage GetFile(string fileName)
        {
            fileName = Uri.UnescapeDataString(fileName);
            //Create HTTP Response.
            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);

            //Set the File Path.
            string filePath = HttpContext.Current.Server.MapPath("~/Files/") + fileName;

            //Check whether File exists.
            if (!File.Exists(filePath))
            {
                //Throw 404 (Not Found) exception if File not found.
                response.StatusCode = HttpStatusCode.NotFound;
                response.ReasonPhrase = string.Format("File not found: {0} .", fileName);
                throw new HttpResponseException(response);
            }

            //Read the File into a Byte Array.
            byte[] bytes = File.ReadAllBytes(filePath);

            //Set the Response Content.
            response.Content = new ByteArrayContent(bytes);

            //Set the Response Content Length.
            response.Content.Headers.ContentLength = bytes.LongLength;

            //Set the Content Disposition Header Value and FileName.
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = fileName;

            //Set the File Content Type.
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(MimeMapping.GetMimeMapping(fileName));
            return response;
        }
        //// POST: api/Convert
        public JsonResult<ConvertResult> Post()
        {
            string data = Request.Content.ReadAsStringAsync().Result;
            var client = new HttpClient();
            byte[] byteArray = MessagePackSerializer.ConvertFromJson($"{{ \"fileId\": \"{JObject.Parse(data)["fileId"]}\"}}");
            HttpContent httpContent = new ByteArrayContent(byteArray);
            client.DefaultRequestHeaders.Add("Authorization", Request.Headers.GetValues("Authorization")); //Crypto.Decrypt(Properties.Settings.Default.UserData, "pass"));
            var res = client.PostAsync("https://adm.ijro.uz/api/files.download", httpContent).Result;
            var arr = res.Content.ReadAsByteArrayAsync().Result;
            var json = MessagePackSerializer.ConvertToJson(arr);
            var file = JsonConvert.DeserializeObject<ReportAPI.Models.IjroFile>(json);
            file.Name = Path.GetFileNameWithoutExtension(file.Name) + ".pdf";
            var fileBytes = Convert.FromBase64String(file.Content);
            using (RichEditDocumentServer wordProcessor = new RichEditDocumentServer())
            {
                try
                {
                    string path = AppDomain.CurrentDomain.BaseDirectory+"/Files";
                    wordProcessor.LoadDocument(fileBytes);
                    Document document = wordProcessor.Document;
                    foreach (var field in document.Fields)
                    {
                        string fieldCode = document.GetText(field.CodeRange).Trim();
                        if (fieldCode.ToUpper().StartsWith("TOC"))
                        {
                            field.Locked = true;
                            ReadOnlyHyperlinkCollection tocLinks = document.Hyperlinks.Get(field.ResultRange);
                            foreach (Hyperlink tocLink in tocLinks)
                            {
                                CharacterProperties cp = document.BeginUpdateCharacters(tocLink.Range);
                                cp.Style = document.CharacterStyles["Default Paragraph Font"];
                                document.EndUpdateCharacters(cp);
                            }
                        }
                    }
                    document.Fields.Update();
                    foreach (Field field in document.Fields)
                    {
                        string fieldCode = document.GetText(field.CodeRange).Trim();
                        if (fieldCode.ToUpper().StartsWith("TOC"))
                        {
                            CharacterProperties cp = document.BeginUpdateCharacters(field.ResultRange);
                            cp.Style = document.CharacterStyles["Default Paragraph Font"];
                            document.EndUpdateCharacters(cp);
                        }
                    }
                    using (var ms = new MemoryStream())
                    {
                    wordProcessor.ExportToPdf(ms);
                        using (var fs = File.Open(path + "/" + file.Name, FileMode.OpenOrCreate))
                        {
                            ms.Position = 0;
                            ms.CopyTo(fs);
                        }
                        return Json(new ConvertResult(){ FileUrl = string.Format(@"http://{0}/ReportApi/api/GetFile?fileName={1}", "185.74.6.63", Uri.EscapeDataString(file.Name)) });
                    }
                }
                catch (Exception ex)
                {
                    return null;
                    // Your code to handle cancellation
                }
            }
        }
        public static string GetServerIP()
        {
            //gets the ipaddress of the machine hitting your production server              
            string ipAddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (ipAddress == "" || ipAddress == null)
            {
                //gets the ipaddress of your local server(localhost) during development phase                                                                         
                ipAddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }

            return ipAddress;
        }
    }
}
