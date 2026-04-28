using Microsoft.AspNetCore.Mvc;
using CompanyService.Services;
using Shared.Models;

namespace CompanyService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyService _companyService;

    public CompaniesController(ICompanyService companyService)
    {
        _companyService = companyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<Company>>> GetCompanies()
    {
        var companies = await _companyService.GetAllAsync();
        return Ok(companies);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Company>> GetCompany(int id)
    {
        var company = await _companyService.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound();
        }

        return Ok(company);
    }

    [HttpPost]
    public async Task<ActionResult<Company>> CreateCompany([FromBody] Company? company)
    {
        if (company == null)
        {
            return BadRequest(new { message = "Company data is required" });
        }

        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                .ToList();
            
            return BadRequest(new { message = "Validation failed", errors = errors });
        }

        if (string.IsNullOrWhiteSpace(company.Name))
        {
            return BadRequest(new { message = "Company name is required" });
        }

        var createdCompany = await _companyService.CreateAsync(company);
        return CreatedAtAction(nameof(GetCompany), new { id = createdCompany.Id }, createdCompany);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Company>> UpdateCompany(int id, [FromBody] Company company)
    {
        var updatedCompany = await _companyService.UpdateAsync(id, company);
        if (updatedCompany == null)
        {
            return NotFound();
        }

        return Ok(updatedCompany);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCompany(int id)
    {
        var deleted = await _companyService.DeleteAsync(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}


