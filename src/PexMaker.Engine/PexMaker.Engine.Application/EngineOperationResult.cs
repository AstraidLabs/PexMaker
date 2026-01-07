using System.Collections.Generic;
using PexMaker.Engine.Domain;

namespace PexMaker.Engine.Application;

/// <summary>
/// Represents the outcome of an engine operation with validation details.
/// </summary>
/// <typeparam name="T">The returned value type.</typeparam>
public sealed record EngineOperationResult<T>(T? Value, ProjectValidationResult ValidationResult)
{
    public bool IsSuccess => ValidationResult.IsValid;

    public IReadOnlyList<ValidationError> Issues => ValidationResult.Issues;
}
