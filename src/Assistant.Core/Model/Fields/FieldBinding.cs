using System;

/// <summary>
/// Maps a field alias to a specific location in the template.
/// 
/// Produced by parser (OpenXML).
/// 
/// Bridges:
/// Template (DOCX) ↔ Internal model.
/// </summary>
namespace Assistant.Core.Model.Fields
{
    public sealed class FieldBinding
    {
        public FieldAlias Alias { get; }
        public string? SdtId { get; }
        public int? OccurrenceIndex { get; }
        public string? LocationHint { get; }
        public FieldContentKind ContentKind { get; }

        public FieldBinding(
            FieldAlias alias,
            string? sdtId,
            int? occurrenceIndex,
            string? locationHint,
            FieldContentKind contentKind)
        {
            if (occurrenceIndex.HasValue && occurrenceIndex.Value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(occurrenceIndex), "Occurrence index must be null or non negative.");
            }

            Alias = alias;
            SdtId = sdtId;
            OccurrenceIndex = occurrenceIndex;
            LocationHint = locationHint;
            ContentKind = contentKind;
        }
    }
}
