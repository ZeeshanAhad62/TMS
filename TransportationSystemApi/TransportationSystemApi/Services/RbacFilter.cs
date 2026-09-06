using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using TransportationSystemApi.Models;

namespace TransportationSystemApi.Services;

// Role-based write authorization, applied globally so no per-controller
// attributes are needed:
//   Viewer       -> read-only (any GET/HEAD/OPTIONS); every mutation is 403.
//   FleetManager -> read + write on operational controllers; blocked from
//                   mutating admin-only controllers (Users, CompanyProfile).
//   Admin        -> everything.
// Requests without a role claim (mobile-driver-app tokens) are left to
// StaffOnlyFilter; [AllowAnonymous] endpoints (login) are skipped.
public class RbacFilter : IAuthorizationFilter
{
    private static readonly HashSet<string> ReadMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        "GET", "HEAD", "OPTIONS"
    };

    private static readonly HashSet<string> AdminOnlyControllers = new(StringComparer.Ordinal)
    {
        "UsersController", "CompanyProfileController"
    };

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any()) return;

        var user = context.HttpContext.User;
        var roleValue = user.FindFirst(ClaimTypes.Role)?.Value;
        if (string.IsNullOrEmpty(roleValue)) return; // not a staff token -- other filters decide

        if (!Enum.TryParse<UserRole>(roleValue, out var role))
        {
            context.Result = new ForbidResult();
            return;
        }

        if (role == UserRole.Admin) return;

        var isWrite = !ReadMethods.Contains(context.HttpContext.Request.Method);
        if (!isWrite) return; // any staff role may read

        if (role == UserRole.Viewer)
        {
            context.Result = Deny("Your account is read-only (Viewer role).");
            return;
        }

        // FleetManager: allowed to write everywhere except the admin-only controllers.
        var controllerName = (context.ActionDescriptor as ControllerActionDescriptor)?.ControllerTypeInfo.Name;
        if (controllerName is not null && AdminOnlyControllers.Contains(controllerName))
        {
            context.Result = Deny("Only an Admin can change users or the company profile.");
        }
    }

    private static ObjectResult Deny(string detail) => new(new ProblemDetails
    {
        Status = StatusCodes.Status403Forbidden,
        Title = "Forbidden",
        Detail = detail
    })
    { StatusCode = StatusCodes.Status403Forbidden };
}
