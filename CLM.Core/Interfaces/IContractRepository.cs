using CLM.Core.DTOs.Contract;

namespace CLM.Core.Interfaces;

public interface IContractRepository
{
    Task<Entities.Contract?> GetByIdAsync(int id);
    Task<PagedResult<Entities.Contract>> GetAllAsync(ContractFilterRequest filter);
    Task<Entities.Contract> CreateAsync(Entities.Contract contract);
    Task<Entities.Contract> UpdateAsync(Entities.Contract contract);
    Task<bool> DeleteAsync(int id);
    Task<List<Entities.Contract>> GetExpiringContractsAsync(int days);
    Task<List<Entities.Contract>> GetByStatusAsync(Core.Enums.ContractStatus status);
    Task<int> GetNextVersionNumberAsync(int contractId);
    Task SaveVersionAsync(Entities.ContractVersion version);
}
