using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Learning.Cqrs;
using Microsoft.AspNetCore.Mvc;
using Learning.EventStore.Sample.Web.Models;
using Learning.EventStore.Sample.Web.Models.ReadModel.Queries;
using Learning.EventStore.Sample.Web.Models.WriteModel.Commands;

namespace Learning.EventStore.Sample.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHub _hub;

        public HomeController(IHub hub)
        {
            _hub = hub;
        }

        public IActionResult Index()
        {
            ViewData.Model = _hub.Query(new GetInventoryItems());
            return View();
        }

        public ActionResult Details(string id)
        {
            ViewData.Model = _hub.Query(new GetInventoryItemDetails(id));
            return View();
        }

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Add(string name, CancellationToken cancellationToken)
        {
            await _hub.CommandAsync(new CreateInventoryItem(name));
            return RedirectToAction("Index");
        }

        public ActionResult CheckIn(string id)
        {
            ViewData.Model = _hub.Query(new GetInventoryItemDetails(id));
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> CheckIn(string id, int number, CancellationToken cancellationToken)
        {
            await _hub.CommandAsync(new CheckInItemsToInventory(id, number));
            return RedirectToAction("Index");
        }

        public ActionResult Remove(string id)
        {
            ViewData.Model = _hub.Query(new GetInventoryItemDetails(id));
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Remove(string id, int number, CancellationToken cancellationToken)
        {
            await _hub.CommandAsync(new RemoveItemsFromInventory(id, number));
            return RedirectToAction("Index");
        }

        public ActionResult ChangeName(string id)
        {
            ViewData.Model = _hub.Query(new GetInventoryItemDetails(id));
            return View();
        }

        [HttpPost]
        public async Task<ActionResult> ChangeName(string id, string name, CancellationToken cancellationToken)
        {
            await _hub.CommandAsync(new RenameInventoryItem(id, name));
            return RedirectToAction("Index");
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
