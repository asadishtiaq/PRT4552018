using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using eBae_MVC.Models;
using eBae_MVC.DAL;

namespace eBae_MVC.Controllers
{
    public class MsgController : Controller
    {
        private AuctionContext db = new AuctionContext();
        //
        // GET: /Msg/
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(string ListingID,string UserID)
        {
            var listings = db.Listings.Where(l => l.ListingID == Convert.ToInt32(ListingID));
            return null;
        }
	}
}