using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace eBae_MVC.Models
{
    public class Msg
    {
        public int MsgID { get; set; }
        public int MsgFrom { get; set; }
        public int MsgTo { get; set; }
        public string MsgText { get; set; }
        public int ListingID { get; set; }

    }
}