using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Routing;
using System.Web.Mvc;
using System.Text;
using MvcForum.Models;

namespace MvcForum.Helpers
{
    public class AccessDeniedViewResult : ViewResult
    {
        public AccessDeniedViewResult(string viewName, object Model = null) : base()
        {
            ViewName = viewName;
            ViewData.Model = Model;
        }

        public override void ExecuteResult(ControllerContext context)
        {
            var response = context.HttpContext.Response;
            
            response.StatusCode = 403;
            base.ExecuteResult(context);
        }
    }

    public static class AuthenticationHelper
    {
        public static ActionResult AccessDeniedView(MasterViewModel ControllerModel, string ErrorMessage = "")
        {
            var Model = ControllerModel.DeriveTo<AccessDeniedViewModel>();
            Model.ErrorMessage = ErrorMessage;
            return new AccessDeniedViewResult("AccessDenied", Model);
        }
    }
}
