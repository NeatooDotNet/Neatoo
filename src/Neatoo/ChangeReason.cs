namespace Neatoo;

/// <summary>
/// Specifies the reason a property value was changed.
/// </summary>
/// <remarks>
/// <para>
/// This enum is used in <see cref="NeatooPropertyChangedEventArgs"/> to indicate
/// whether a change came from user editing or from loading data.
/// </para>
/// <para>
/// The reason affects how <see cref="ValidateBase{T}"/> handles the change:
/// <list type="bullet">
/// <item><description><see cref="UserEdit"/>: Runs rules and bubbles events up the hierarchy</description></item>
/// <item><description><see cref="Load"/>: Only establishes parent-child relationships, skips rules</description></item>
/// </list>
/// </para>
/// </remarks>
public enum ChangeReason
{
    /// <summary>
    /// Normal property assignment via setter. Triggers full rule execution and event bubbling.
    /// </summary>
    UserEdit,

    /// <summary>
    /// Loading data via <see cref="IValidateProperty.LoadValue"/>.
    /// Establishes structural relationships (SetParent) but skips rules and event bubbling.
    /// </summary>
    Load
}
