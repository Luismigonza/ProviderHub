using FluentValidation;
using ProviderHub.Domain.Common;

namespace ProviderHub.Application.Common;

/// <summary>
/// Bridges the two kinds of validation this application performs.
/// </summary>
public static class DomainRuleExtensions
{
    /// <summary>
    /// Validates a field by trying to build the value object it stands for, and reports the
    /// domain's own message when that fails.
    /// <para>
    /// The alternative would be to restate every rule here: a regular expression for e-mail in
    /// the validator and another one in <c>EmailAddress</c>, a NIT check digit implemented
    /// twice. Two copies of a rule are one copy too many, and the domain is the one that gets to
    /// decide. This way the model stays the single source of truth and the caller still gets a
    /// per-field <c>400</c> listing everything that is wrong, instead of an exception about
    /// whichever problem happened to be found first.
    /// </para>
    /// <para>
    /// <c>build</c> is the factory that produces the value object, or throws trying.
    /// </para>
    /// </summary>
    public static IRuleBuilderOptions<T, TProperty> MustBuildDomainValue<T, TProperty>(
        this IRuleBuilder<T, TProperty> ruleBuilder,
        Func<TProperty, object> build)
    {
        ArgumentNullException.ThrowIfNull(ruleBuilder);
        ArgumentNullException.ThrowIfNull(build);

        return ruleBuilder
            .Must((_, value, context) =>
            {
                try
                {
                    build(value);
                    return true;
                }
                catch (DomainException exception)
                {
                    context.MessageFormatter.AppendArgument("DomainError", exception.Message);
                    return false;
                }
            })
            .WithMessage("{DomainError}");
    }
}
