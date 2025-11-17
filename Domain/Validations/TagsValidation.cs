using System.ComponentModel.DataAnnotations;

namespace Domain.Validations;

public class TagsValidationAttribute()
    : ValidationAttribute("Each tag must be at least 5 characters long, and there can be a maximum of 5 tags.")
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var tags = value as string[];
        if (tags is null)
            return ValidationResult.Success;

        if (tags.Length > 5)
        {
            return new ValidationResult("You can specify up to 5 tags.");
        }

        if (tags.Any(tag => tag.Length < 4))
        {
            return new ValidationResult("Each tag must be at least 4 characters long.");
        }
        
        return ValidationResult.Success;
    }
}