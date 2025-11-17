using System.ComponentModel.DataAnnotations;
using Domain.Enums;
using Domain.Validations;

namespace Domain.DTOs.News;

public class CreateNewsDto
{
    [Required]
    [MinLength(5)]
    public required string Title { get; set; }
    [Required]
    [MinLength(100)]
    public required string Content { get; set; }
    [Required]
    public Category Category { get; set; }
    [TagsValidation]
    public string[]? Tags { get; set; }
}