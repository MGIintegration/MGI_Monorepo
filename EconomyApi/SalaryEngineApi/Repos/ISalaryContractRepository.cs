using SalaryEngineApi.Models;

namespace SalaryEngineApi.Repos;

public interface ISalaryContractRepository
{
    bool Upsert(SalaryContract contract);
    SalaryContract? Get(string playerId);
    bool Exists(string playerId);
}