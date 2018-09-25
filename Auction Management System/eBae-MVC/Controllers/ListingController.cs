using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using eBae_MVC.Models;
using eBae_MVC.DAL;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using System.Collections.Generic;

namespace eBae_MVC.Controllers
{
    public class ListingController : Controller
    {
        private AuctionContext db = new AuctionContext();

        //
        // GET: /Listing/

        public ActionResult Index()
        {
            var listings = db.Listings.Include(l => l.User);
            ViewBag.ImageURL = Url.Content("~/Content/Images/");
            ViewBag.JPG = ".jpg";
            ViewBag.Now = DateTime.Now;
            return View(listings.ToList());
        }

        //
        // GET: /Listing/Details/5

        public ActionResult Details(int id = 0)
        {
            Listing listing = db.Listings.Find(id);
            if (listing == null)
            {
                return HttpNotFound();
            }
            var UserID = Convert.ToInt32(Session["CurrentUserID"]);
            List<Msg> MLsit = new List<Msg>();
            if (listing.UserID == UserID)
            {
                MLsit = db.Msg.Where(m => m.ListingID == listing.ListingID).ToList();
            }
            else
            {
                List<Msg> MLsit1 = db.Msg.Where(m => m.ListingID == listing.ListingID && (m.MsgTo == listing.UserID && m.MsgFrom == listing.UserID)).ToList();
                List<Msg> Mlist2 = db.Msg.Where(m => m.ListingID == listing.ListingID && (m.MsgTo == listing.UserID && m.MsgFrom == UserID)).ToList();
                MLsit = MLsit1.Union(Mlist2).OrderBy(o=>o.MsgID).ToList(); 
            }
            if (MLsit != null)
            {
                var str = string.Empty;
                foreach(Msg m in MLsit)
                {
                    str += m.MsgText + "\n";
                }
                ViewBag.MsgText = str.Replace("\n", "<br>").Replace("\r", "<br>");
            }
            Bid latestBid = null;
            foreach (var b in listing.Bids.OrderByDescending(l => l.Timestamp).Take(1))
                latestBid = b;

            if (latestBid == null) 
            {
                ViewBag.CurrentPrice = listing.StartingPrice;
            } else {
                ViewBag.CurrentPrice = latestBid.Amount;
            }
            Session["CurrentListingID"] = id;
            return View(listing);
        }

        //
        // POST: /Listing/Details/5

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Details(Bid bid)
        {
            
            int currentListingID = Convert.ToInt32(Session["CurrentListingID"]);
            Listing currentListingOwner = db.Listings.FirstOrDefault(l => l.ListingID == currentListingID);
            int currentListingOwnerID = currentListingOwner.UserID;

            // Check if the link is valid
            if (ModelState.IsValid)
            {
                // Check if the user's different from the listing owner
                if (currentListingOwnerID != Convert.ToInt32(Session["CurrentUserID"]))
                {
                    // Check for consecutive bids
                    Listing listing = db.Listings.Find(currentListingID);
                    // Dirty workaround
                    Bid latestBid = null;
                    foreach (var b in listing.Bids.OrderByDescending(l => l.Timestamp).Take(1))
                        latestBid = b;
                    // Can't bid on finished auctions                   
                    if (listing.EndTimestamp.Subtract(DateTime.Now).Seconds > 0)
                    {
                        // Can't bid on the same auction twice
                        if (latestBid == null || latestBid.UserID != Convert.ToInt32(Session["CurrentUserID"]))
                        {
                            // Amount must be greater than the last bid and starting big
                            if ((latestBid == null && bid.Amount >= listing.StartingPrice) ||
                                (latestBid != null && bid.Amount > latestBid.Amount))
                            {

                                if (latestBid == null)
                                {
                                    ViewBag.CurrentPrice = listing.StartingPrice;
                                }
                                else
                                {
                                    ViewBag.CurrentPrice = bid.Amount;
                                }


                                bid.UserID = Convert.ToInt32(Session["CurrentUserID"]);
                                bid.User = db.Users.FirstOrDefault(u => u.UserID == bid.UserID);
                                bid.ListingID = Convert.ToInt32(Session["CurrentListingID"]);
                                bid.Listing = db.Listings.FirstOrDefault(l => l.ListingID == bid.ListingID);
                                bid.Timestamp = DateTime.Now;

                                db.Bids.Add(bid);
                                db.SaveChanges();

                                DefaultHubManager hd = new DefaultHubManager(GlobalHost.DependencyResolver);
                                var context = GlobalHost.ConnectionManager.GetHubContext<AuctionHub>();
                                context.Clients.All.addBidToPage(bid.User.Username, bid.Amount.ToString(), bid.Timestamp.ToString(), bid.ListingID.ToString());
                                return View(listing);
                            }
                            else
                            {
                                return RedirectToAction("Error", new { ErrorID = 5 });
                            }
                        }
                        else
                        {
                            return RedirectToAction("Error", new { ErrorID = 4 });
                        }
                    }
                    else
                    {
                        return RedirectToAction("Error", new { ErrorID = 3 });
                    }
                }
                else
                {
                    return RedirectToAction("Error", new { ErrorID = 2 });
                }
            }
            
            return RedirectToAction("Error", new { ErrorID = 1 });
            
        }

        protected override void Dispose(bool disposing)
        {
            db.Dispose();
            base.Dispose(disposing);
        }

        [HttpPost]
        public ActionResult Chat(string ListingID, string UserID,string MsgText)
        {
            var listingID = Convert.ToInt32(ListingID);
            var listings = db.Listings.FirstOrDefault(l => l.ListingID == listingID);
            var UserIDs = Convert.ToInt32(UserID);
            var User = db.Users.FirstOrDefault(u => u.UserID == UserIDs);
            var M = new Msg();
            //db.Msg.All(m => m.MsgTo == listings.UserID && m.ListingID == listings.ListingID && m.MsgFrom == UserIDs);
            //if(M == null)
            //{
                M.MsgFrom = Convert.ToInt32(UserID);
                M.MsgTo = listings.UserID;
                M.MsgText = "<span style=color:red>" + User.Username+ ": </span>" + MsgText.Trim();
                M.ListingID = listings.ListingID;
                db.Msg.Add(M);
            //}
            //else
            //{
            //    M.MsgText = "<span style=color:red>" + User.Username + ": &nbsp;</span>" + MsgText.Trim();
            //}
            db.SaveChanges();
            return View();
        }

        public ActionResult Error(int ErrorID)
        {
            ViewBag.CurrentListingID = Convert.ToInt32(Session["CurrentListingID"]);

            switch (ErrorID)
            {
                case 1:
                    ViewBag.ErrorText = "Invalid input. Please try again.";
                    break;
                case 2:
                    ViewBag.ErrorText = "You cannot bid on your own listing.";
                    break;
                case 3:
                    ViewBag.ErrorText = "This auction has already ended.";
                    break;
                case 4:
                    ViewBag.ErrorText = "You cannot bid on the same listing twice in a row.";
                    break;
                case 5:
                    ViewBag.ErrorText = "Your bid amount must be higher than the previous bid or starting price.";
                    break;
                default:
                    ViewBag.ErrorText = "Something went wrong.";
                    break;
            }
            return View();
        }
    }
}