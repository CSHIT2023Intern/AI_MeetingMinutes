using azureMVC.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace azureMVC.Controllers
{
    public class SampleController : Controller
    {
        // GET: Sample
        [HttpGet]
        public ActionResult Sample()
        {
            //讀取資料夾
            string[] fpaths = Directory.GetFiles(Server.MapPath("~/VoiceSignature"));
            List<FileModel> files = new List<FileModel>();
            foreach (string fpath in fpaths)
            {
                files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
            }
            return View(files);

        }
        //上傳檔案到資料夾
        [HttpPost]
        public ActionResult Sample(HttpPostedFileBase audioFile)
        {
            if (audioFile == null)
            {
                return View();
            }
            else if (audioFile.ContentLength > 0)
            {
                //檢查檔案類型是否為wav, mp3 ,m4a
                string[] allowedExtensions = { ".wav", ".mp3", ".m4a" };
                string fileExtension = Path.GetExtension(audioFile.FileName).ToLower();
                if (!allowedExtensions.Contains(fileExtension))
                {
                    ViewBag.mess = "只能上傳wav、mp3、m4a檔";
                    return View();
                }
                string FileName = Path.GetFileName(audioFile.FileName);
                string FilePath = Path.Combine(Server.MapPath("~/VoiceSignature"), FileName);
                audioFile.SaveAs(FilePath);
                string[] fpaths = Directory.GetFiles(Server.MapPath("~/VoiceSignature"));
                List<FileModel> files = new List<FileModel>();
                foreach (string fpath in fpaths)
                {
                    files.Add(new FileModel { Filename = Path.GetFileName(fpath) });
                }
            }
            return RedirectToAction("Sample"); //導向 Upload() function           
        }
    }
}