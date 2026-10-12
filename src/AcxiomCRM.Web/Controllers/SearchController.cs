using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AcxiomCRM.Application.Interfaces;

namespace AcxiomCRM.Web.Controllers;

[Authorize]
public class SearchController : Controller
{
    private readonly ICustomerService _customerService;
    private readonly ILeadService _leadService;
    private readonly IOpportunityService _opportunityService;

    public SearchController(
        ICustomerService customerService,
        ILeadService leadService,
        IOpportunityService opportunityService)
    {
        _customerService = customerService;
        _leadService = leadService;
        _opportunityService = opportunityService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        ViewBag.Query = q;

        if (string.IsNullOrWhiteSpace(q))
        {
            return View();
        }

        var customers = await _customerService.GetCustomersAsync(q, null, 1, 5);
        var leads = await _leadService.GetLeadsAsync(q, null, null, 1, 5);
        var opps = await _opportunityService.GetOpportunitiesAsync(q, null, null, 1, 5);

        ViewBag.Customers = customers.Items;
        ViewBag.Leads = leads.Items;
        ViewBag.Opportunities = opps.Items;

        return View();
    }
}
