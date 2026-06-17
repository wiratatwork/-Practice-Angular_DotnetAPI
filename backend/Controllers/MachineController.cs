using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachineController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MachineController> _logger;

        public MachineController(AppDbContext context, ILogger<MachineController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// ดึงข้อมูลเครื่องจักรทั้งหมด
        /// GET: api/machine
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Machine>>> GetAllMachines()
        {
            try
            {
                var machines = await _context.Machines.ToListAsync();
                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all machines");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// ดึงข้อมูลเครื่องจักรตาม MachineNo (Primary Key)
        /// GET: api/machine/{machineNo}
        /// </summary>
        [HttpGet("{machineNo}")]
        public async Task<ActionResult<Machine>> GetMachineByNo(string machineNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineNo))
                {
                    return BadRequest(new { message = "MachineNo is required" });
                }

                var machine = await _context.Machines.FirstOrDefaultAsync(m => m.MachineNo == machineNo);
                if (machine == null)
                {
                    return NotFound(new { message = "Machine not found" });
                }
                return Ok(machine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by MachineNo");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// ค้นหาเครื่องจักรตาม MachineNo หรือ MachineName (trim และ lowercase)
        /// GET: api/machine/search/{searchTerm}
        /// </summary>
        [HttpGet("search/{searchTerm}")]
        public async Task<ActionResult<IEnumerable<Machine>>> SearchMachines(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new { message = "Search term cannot be empty" });
                }

                var normalizedSearchTerm = searchTerm.Trim().ToLower();

                var machines = await _context.Machines
                    .Where(m =>
                        m.MachineNo.ToLower().Contains(normalizedSearchTerm) ||
                        (m.MachineName != null && m.MachineName.ToLower().Contains(normalizedSearchTerm))
                    )
                    .ToListAsync();

                return Ok(machines);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching machines");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// สร้างเครื่องจักรใหม่
        /// POST: api/machine
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Machine>> CreateMachine([FromBody] CreateMachineDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return BadRequest(new { message = "Machine data is required" });
                }

                // Validate DTO
                var context = new ValidationContext(dto, serviceProvider: null, items: null);
                var results = new List<ValidationResult>();
                if (!Validator.TryValidateObject(dto, context, results, validateAllProperties: true))
                {
                    var errors = results.Select(r => r.ErrorMessage).ToList();
                    return BadRequest(new { message = "Validation failed", errors });
                }

                // Normalize MachineNo: trim and lowercase for comparison
                var normalizedMachineNo = dto.MachineNo.Trim().ToLower();

                // Check if MachineNo already exists (case-insensitive comparison)
                var existingMachine = await _context.Machines
                    .FirstOrDefaultAsync(m => m.MachineNo.ToLower() == normalizedMachineNo);
                if (existingMachine != null)
                {
                    return BadRequest(new { 
                        message = "MachineNo already exists", 
                        code = "MACHINE_NO_DUPLICATE",
                        existingMachineNo = existingMachine.MachineNo 
                    });
                }

                var machine = new Machine
                {
                    MachineNo = dto.MachineNo.Trim(),
                    MachineName = dto.MachineName?.Trim(),
                    Plant = dto.Plant?.Trim(),
                    Status = dto.Status?.Trim()
                };

                _context.Machines.Add(machine);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetMachineByNo), new { machineNo = machine.MachineNo }, machine);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error creating machine");
                return StatusCode(500, new { message = "Database error", error = ex.InnerException?.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating machine");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// อัปเดตข้อมูลเครื่องจักร (แบบ partial update)
        /// PATCH: api/machine/{machineNo}
        /// </summary>
        [HttpPatch("{machineNo}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Machine>> UpdateMachine(string machineNo, [FromBody] UpdateMachineDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineNo))
                {
                    return BadRequest(new { message = "MachineNo is required" });
                }

                if (dto == null)
                {
                    return BadRequest(new { message = "Machine data is required" });
                }

                var machine = await _context.Machines.FirstOrDefaultAsync(m => m.MachineNo == machineNo);
                if (machine == null)
                {
                    return NotFound(new { message = "Machine not found" });
                }

                // Update only provided fields
                if (!string.IsNullOrWhiteSpace(dto.MachineName))
                    machine.MachineName = dto.MachineName.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Plant))
                    machine.Plant = dto.Plant.Trim();

                if (!string.IsNullOrWhiteSpace(dto.Status))
                    machine.Status = dto.Status.Trim();

                _context.Machines.Update(machine);
                await _context.SaveChangesAsync();

                return Ok(machine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating machine");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// ตรวจสอบว่า MachineName ซ้ำหรือไม่ (trim และ lowercase, exact match)
        /// GET: api/machine/checkDuplicateName/{machineName}
        /// </summary>
        [HttpGet("checkDuplicateName/{machineName}")]
        public async Task<ActionResult> CheckDuplicateName(string machineName, [FromQuery] string? excludeMachineNo = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineName))
                {
                    return BadRequest(new { message = "MachineName is required" });
                }

                var normalizedName = machineName.Trim().ToLower();

                var query = _context.Machines.AsQueryable();

                // ถ้ามี excludeMachineNo ให้ exclude record นั้นออก (กรณีแก้ไข)
                if (!string.IsNullOrWhiteSpace(excludeMachineNo))
                {
                    query = query.Where(m => m.MachineNo != excludeMachineNo);
                }

                var isDuplicate = await query
                    .AnyAsync(m => m.MachineName.Trim().ToLower() == normalizedName);

                return Ok(new { isDuplicate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking duplicate MachineName");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }

        /// <summary>
        /// ลบเครื่องจักร
        /// DELETE: api/machine/{machineNo}
        /// </summary>
        [HttpDelete("{machineNo}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteMachine(string machineNo)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(machineNo))
                {
                    return BadRequest(new { message = "MachineNo is required" });
                }

                var machine = await _context.Machines.FirstOrDefaultAsync(m => m.MachineNo == machineNo);
                if (machine == null)
                {
                    return NotFound(new { message = "Machine not found" });
                }

                _context.Machines.Remove(machine);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting machine");
                return StatusCode(500, new { message = "Internal server error", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// DTO สำหรับสร้างเครื่องจักรใหม่
    /// </summary>
    public class CreateMachineDto
    {
        [Required(ErrorMessage = "MachineNo is required")]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "MachineNo must be between 1 and 50 characters")]
        public string MachineNo { get; set; } = string.Empty;

        [Required(ErrorMessage = "MachineName is required")]
        [StringLength(50, ErrorMessage = "MachineName cannot exceed 50 characters")]
        public string MachineName { get; set; }

        [Required(ErrorMessage = "Plant is required")]
        [StringLength(10, ErrorMessage = "Plant cannot exceed 10 characters")]
        public string Plant { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(10, ErrorMessage = "Status cannot exceed 10 characters")]
        public string Status { get; set; }
    }

    /// <summary>
    /// DTO สำหรับอัปเดตเครื่องจักร (partial update)
    /// </summary>
    public class UpdateMachineDto
    {
        [Required(ErrorMessage = "MachineName is required")]
        [StringLength(50, ErrorMessage = "MachineName cannot exceed 50 characters")]
        public string MachineName { get; set; }

        [Required(ErrorMessage = "Plant is required")]
        [StringLength(10, ErrorMessage = "Plant cannot exceed 10 characters")]
        public string Plant { get; set; }

        [Required(ErrorMessage = "Status is required")]
        [StringLength(10, ErrorMessage = "Status cannot exceed 10 characters")]
        public string Status { get; set; }
    }
}
