using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using TransportationSystemApi.Controllers;

namespace TransportationSystemApi.Services;

// Global authorization filter: a mobile-driver-app token (token_type =
// "driver-app") is only ever valid on DriverAppController. Any such token
// hitting another controller is forbidden, so the scoped driver surface
// cannot be used to reach staff endpoints.
public class StaffOnlyFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isDriverToken = context.HttpContext.User.HasClaim("token_type", "driver-app");
        if (!isDriverToken) return;

        var isDriverAppController = context.ActionDescriptor is ControllerActionDescriptor cad
            && cad.ControllerTypeInfo.AsType() == typeof(DriverAppController);

        if (!isDriverAppController)
            context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
    }
}
