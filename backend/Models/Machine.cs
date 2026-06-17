using System.ComponentModel.DataAnnotations;

namespace backend.Models
{
    public class Machine
    {
        [Key]
        [StringLength(50, MinimumLength = 1, ErrorMessage = "MachineNo must be between 1 and 50 characters")]
        [Required(ErrorMessage = "MachineNo is required")]
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
}
