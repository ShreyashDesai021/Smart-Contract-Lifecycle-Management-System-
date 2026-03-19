using System.Security.Claims;
using System.Text.Json;
using AutoMapper;
using CLM.Core.DTOs.Contract;
using CLM.Core.Entities;
using CLM.Core.Enums;
using CLM.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CLM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ContractsController : ControllerBase
{
    private readonly IContractRepository _repo;
    private readonly IFileStorageService _fileStorage;
    private readonly IAuditService _audit;

    public ContractsController(IContractRepository repo, IFileStorageService fileStorage, IAuditService audit)
    {
        _repo = repo;
        _fileStorage = fileStorage;
        _audit = audit;
    }

    private int GetUserId() =>
        int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
    private string GetUserRole() =>
        User.FindFirstValue(ClaimTypes.Role) ?? "User";

    /// <summary>Get paginated, filtered list of contracts</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ContractFilterRequest filter)
    {
        var result = await _repo.GetAllAsync(filter);
        var mapped = result.Items.Select(c => MapToResponse(c)).ToList();
        return Ok(new PagedResult<ContractResponse>
        {
            Items = mapped,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        });
    }

    /// <summary>Get single contract by ID</summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null) return NotFound();
        return Ok(MapToResponse(contract));
    }

    /// <summary>Create a new contract (User, Manager, Admin)</summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractRequest request)
    {
        var userId = GetUserId();
        var contract = new Contract
        {
            Title = request.Title,
            Parties = request.Parties,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Value = request.Value,
            Description = request.Description,
            Status = ContractStatus.Draft,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created = await _repo.CreateAsync(contract);

        // Save initial version
        await SaveVersion(created.Id, userId, "Initial version");
        await _audit.LogAsync(userId, "Create", "Contract", created.Id, $"Contract '{contract.Title}' created");

        var full = await _repo.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, MapToResponse(full!));
    }

    /// <summary>Update a contract (only Draft status)</summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateContractRequest request)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (contract.Status != ContractStatus.Draft)
            return BadRequest(new { message = "Only Draft contracts can be edited" });

        var userId = GetUserId();

        contract.Title = request.Title;
        contract.Parties = request.Parties;
        contract.StartDate = request.StartDate;
        contract.EndDate = request.EndDate;
        contract.Value = request.Value;
        contract.Description = request.Description;

        await _repo.UpdateAsync(contract);
        await SaveVersion(id, userId, request.ChangeNotes ?? "Updated");
        await _audit.LogAsync(userId, "Update", "Contract", id, $"Contract '{contract.Title}' updated");

        var full = await _repo.GetByIdAsync(id);
        return Ok(MapToResponse(full!));
    }

    /// <summary>Upload file for a contract</summary>
    [HttpPost("{id}/upload")]
    public async Task<IActionResult> UploadFile(int id, IFormFile file)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        var allowed = new[] { ".pdf", ".doc", ".docx" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowed.Contains(ext))
            return BadRequest(new { message = "Only PDF, DOC, DOCX files are allowed" });

        if (file.Length > 10 * 1024 * 1024)
            return BadRequest(new { message = "File size must not exceed 10MB" });

        // Delete old file if exists
        if (!string.IsNullOrEmpty(contract.FilePath))
            await _fileStorage.DeleteFileAsync(contract.FilePath);

        await using var stream = file.OpenReadStream();
        var path = await _fileStorage.SaveFileAsync(stream, file.FileName, file.ContentType);
        contract.FilePath = path;

        await _repo.UpdateAsync(contract);
        await _audit.LogAsync(GetUserId(), "UploadFile", "Contract", id,
            $"File '{file.FileName}' uploaded to contract '{contract.Title}'");

        return Ok(new { filePath = path });
    }

    /// <summary>Download a contract file</summary>
    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null || string.IsNullOrEmpty(contract.FilePath))
            return NotFound(new { message = "No file attached to this contract" });

        var (stream, contentType) = await _fileStorage.GetFileAsync(contract.FilePath);
        return File(stream, contentType, contract.FilePath);
    }

    /// <summary>Delete a contract (Admin only)</summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        if (!string.IsNullOrEmpty(contract.FilePath))
            await _fileStorage.DeleteFileAsync(contract.FilePath);

        await _repo.DeleteAsync(id);
        await _audit.LogAsync(GetUserId(), "Delete", "Contract", id, $"Contract '{contract.Title}' deleted");
        return NoContent();
    }

    /// <summary>Get version history of a contract</summary>
    [HttpGet("{id}/versions")]
    public async Task<IActionResult> GetVersions(int id)
    {
        var contract = await _repo.GetByIdAsync(id);
        if (contract == null) return NotFound();

        var versions = contract.Versions
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new
            {
                v.Id,
                v.VersionNumber,
                v.ChangeNotes,
                v.ChangedAt,
                ChangedBy = v.ChangedBy?.Name
            });

        return Ok(versions);
    }

    private async Task SaveVersion(int contractId, int userId, string notes)
    {
        var contract = await _repo.GetByIdAsync(contractId);
        if (contract == null) return;

        var versionNum = await _repo.GetNextVersionNumberAsync(contractId);
        var snapshot = JsonSerializer.Serialize(new
        {
            contract.Title,
            contract.Parties,
            contract.StartDate,
            contract.EndDate,
            contract.Value,
            contract.Description,
            Status = contract.Status.ToString()
        });

        await _repo.SaveVersionAsync(new ContractVersion
        {
            ContractId = contractId,
            VersionNumber = versionNum,
            Snapshot = snapshot,
            ChangeNotes = notes,
            ChangedByUserId = userId,
            ChangedAt = DateTime.UtcNow
        });
    }

    private static ContractResponse MapToResponse(Contract c) => new()
    {
        Id = c.Id,
        Title = c.Title,
        Parties = c.Parties,
        StartDate = c.StartDate,
        EndDate = c.EndDate,
        Value = c.Value,
        Status = c.Status.ToString(),
        FilePath = c.FilePath,
        Description = c.Description,
        CreatedByUserId = c.CreatedByUserId,
        CreatedByName = c.CreatedBy?.Name ?? "",
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt,
        VersionCount = c.Versions?.Count ?? 0
    };
}
