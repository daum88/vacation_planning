using FluentValidation;
using VacationRequestApi.Constants;
using VacationRequestApi.DTOs;

namespace VacationRequestApi.Validators
{
    /// <summary>
    /// Validator for RegisterDto
    /// </summary>
    public class RegisterDtoValidator : AbstractValidator<RegisterDto>
    {
        public RegisterDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .EmailAddress().WithMessage(ErrorMessages.InvalidEmail)
                .MaximumLength(200);

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MinimumLength(AppConstants.PasswordMinLength)
                    .WithMessage(string.Format(ErrorMessages.PasswordTooShort, AppConstants.PasswordMinLength))
                .MaximumLength(AppConstants.PasswordMaxLength);

            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(100);

            RuleFor(x => x.OrganizationId)
                .GreaterThan(0).WithMessage("Organisatsioon on kohustuslik");

            RuleFor(x => x.JoinMessage)
                .MaximumLength(500);
        }
    }

    /// <summary>
    /// Validator for CompleteProfileDto
    /// </summary>
    public class CompleteProfileDtoValidator : AbstractValidator<CompleteProfileDto>
    {
        public CompleteProfileDtoValidator()
        {
            RuleFor(x => x.FirstName)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(100);

            RuleFor(x => x.LastName)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(100);

            RuleFor(x => x.Department)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(100);

            RuleFor(x => x.Position)
                .MaximumLength(100);

            RuleFor(x => x.HireDate)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .LessThanOrEqualTo(DateTime.Today)
                    .WithMessage("Tööle asumise kuupäev ei saa olla tulevikus");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MinimumLength(AppConstants.PasswordMinLength)
                    .WithMessage(string.Format(ErrorMessages.PasswordTooShort, AppConstants.PasswordMinLength))
                .MaximumLength(AppConstants.PasswordMaxLength);
        }
    }

    /// <summary>
    /// Validator for InviteUserDto
    /// </summary>
    public class InviteUserDtoValidator : AbstractValidator<InviteUserDto>
    {
        public InviteUserDtoValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .EmailAddress().WithMessage(ErrorMessages.InvalidEmail)
                .MaximumLength(200);

            RuleFor(x => x.OrganizationId)
                .GreaterThan(0).WithMessage("Organisatsioon on kohustuslik");

            RuleFor(x => x.AnnualLeaveDays)
                .InclusiveBetween(AppConstants.MinAnnualLeaveDays, AppConstants.MaxAnnualLeaveDays)
                    .WithMessage($"Aastane puhkusepäevade arv peab olema vahemikus {AppConstants.MinAnnualLeaveDays}-{AppConstants.MaxAnnualLeaveDays}");
        }
    }

    /// <summary>
    /// Validator for ChangePasswordDto
    /// </summary>
    public class ChangePasswordDtoValidator : AbstractValidator<ChangePasswordDto>
    {
        public ChangePasswordDtoValidator()
        {
            RuleFor(x => x.CurrentPassword)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField);

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MinimumLength(AppConstants.PasswordMinLength)
                    .WithMessage(string.Format(ErrorMessages.PasswordTooShort, AppConstants.PasswordMinLength))
                .MaximumLength(AppConstants.PasswordMaxLength)
                .NotEqual(x => x.CurrentPassword)
                    .WithMessage("Uus parool ei saa olla sama kui praegune parool");
        }
    }

    /// <summary>
    /// Validator for OrganizationCreateDto
    /// </summary>
    public class OrganizationCreateDtoValidator : AbstractValidator<OrganizationCreateDto>
    {
        public OrganizationCreateDtoValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage(ErrorMessages.RequiredField)
                .MaximumLength(200);

            RuleFor(x => x.Description)
                .MaximumLength(1000);

            RuleFor(x => x.Address)
                .MaximumLength(300);

            RuleFor(x => x.ContactEmail)
                .EmailAddress().When(x => !string.IsNullOrEmpty(x.ContactEmail))
                    .WithMessage(ErrorMessages.InvalidEmail)
                .MaximumLength(200);

            RuleFor(x => x.ContactPhone)
                .MaximumLength(50);
        }
    }

    /// <summary>
    /// Validator for JoinRequestReviewDto
    /// </summary>
    public class JoinRequestReviewDtoValidator : AbstractValidator<JoinRequestReviewDto>
    {
        public JoinRequestReviewDtoValidator()
        {
            RuleFor(x => x.Note)
                .MaximumLength(500);

            When(x => x.Approve, () =>
            {
                RuleFor(x => x.Department)
                    .NotEmpty().WithMessage("Osakond on kohustuslik kinnitamisel")
                    .MaximumLength(100);

                RuleFor(x => x.Position)
                    .MaximumLength(100);

                RuleFor(x => x.AnnualLeaveDays)
                    .InclusiveBetween(AppConstants.MinAnnualLeaveDays, AppConstants.MaxAnnualLeaveDays)
                        .WithMessage($"Aastane puhkusepäevade arv peab olema vahemikus {AppConstants.MinAnnualLeaveDays}-{AppConstants.MaxAnnualLeaveDays}");
            });
        }
    }
}
