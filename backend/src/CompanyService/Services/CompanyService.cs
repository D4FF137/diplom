using Microsoft.EntityFrameworkCore;
using Shared.Models;
using CompanyService.Data;

namespace CompanyService.Services;

public class CompanyService : ICompanyService
{
    private readonly CompanyDbContext _context;

    public CompanyService(CompanyDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        return await _context.Companies.FindAsync(id);
    }

    public async Task<List<Company>> GetAllAsync()
    {
        return await _context.Companies.ToListAsync();
    }

    public async Task<Company> CreateAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<Company?> UpdateAsync(int id, Company company)
    {
        var existingCompany = await GetByIdAsync(id);
        if (existingCompany == null) return null;

        existingCompany.Name = company.Name;
        await _context.SaveChangesAsync();
        return existingCompany;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var company = await GetByIdAsync(id);
        if (company == null) return false;

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Companies.AnyAsync(c => c.Id == id);
    }
}


