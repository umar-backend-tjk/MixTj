using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Domain.DTOs.Video;

public class CreateVideoDto
{
    [Required]
    [StringLength(50), MinLength(5)]
    public required string Title { get; set; }
    [MaxLength(200)]
    public string Description { get; set; } = string.Empty;
    [Required]
    public required IFormFile VideoFile { get; set; }
}