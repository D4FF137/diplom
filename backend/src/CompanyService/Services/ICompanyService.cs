using Shared.Models;

namespace CompanyService.Services;

public interface ICompanyService
{
    Task<Company?> GetByIdAsync(int id);
    Task<List<Company>> GetAllAsync();
    Task<Company> CreateAsync(Company company);
    Task<Company?> UpdateAsync(int id, Company company);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}


