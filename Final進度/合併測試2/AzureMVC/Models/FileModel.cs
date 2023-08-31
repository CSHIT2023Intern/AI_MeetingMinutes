using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace AzureMVC.Models
{
    public class FileModel
    {
        [Required]
        [Display(Name="檔名")]
        public string Filename { get; set; }
    }
}