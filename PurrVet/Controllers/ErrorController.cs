using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PurrVet.Models;

namespace PurrVet.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode?}")]
        public IActionResult HandleError(int? statusCode)
        {
            if (statusCode == 404 || !statusCode.HasValue)
                return View("NotFound");

            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };
            return View("Error", model);
        }

        [Route("Error/Error")]
        public IActionResult GeneralError()
        {
            var model = new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            };

            return View("Error", model);
        }
    }
}