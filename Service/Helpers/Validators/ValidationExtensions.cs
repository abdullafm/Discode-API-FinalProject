using FluentValidation;
using Service.Helpers.Exceptions;

namespace Service.Helpers.Validators
{
    public static class ValidationExtensions
    {
        public static async Task EnsureValidAsync<T>(this IValidator<T> validator, T model)
        {
            var result = await validator.ValidateAsync(model);
            if (!result.IsValid)
            {
                throw new BadRequestException(result.Errors.Select(e => e.ErrorMessage).Distinct().ToArray());
            }
        }
    }
}
