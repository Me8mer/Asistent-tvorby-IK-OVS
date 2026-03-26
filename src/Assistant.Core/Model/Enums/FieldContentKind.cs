/// <summary>
/// Describes what kind of document element this field binds to.
/// 
/// Used by parser/binding layer to interpret SDTs correctly.
/// </summary>
namespace Assistant.Core.Model.Enums
{
    public enum FieldContentKind
    {
        Unknown = 0,
        PlainText = 1,
        TableCell = 2,
        RepeatingSection = 3
    }
}
