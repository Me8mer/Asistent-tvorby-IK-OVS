using System;

namespace Assistant.Core.Model
{
    public sealed class FieldValue
    {
        public FieldAlias Alias { get; }
        public FieldStatus Status { get; }
        public object? Value { get; }
        public string? Source { get; }
        public double? Confidence { get; }
        public DateTimeOffset Timestamp { get; }

        public FieldValue(
            FieldAlias alias,
            FieldStatus status,
            object? value,
            string? source,
            double? confidence,
            DateTimeOffset timestamp)
        {
            Alias = alias;
            Status = status;
            Value = value;
            Source = source;
            Confidence = confidence;
            Timestamp = timestamp;
        }

        public static FieldValue CreateEmpty(FieldAlias alias)
        {
            return new FieldValue(
                alias,
                FieldStatus.Empty,
                null,
                source: null,
                confidence: null,
                timestamp: DateTimeOffset.UtcNow);
        }

        public FieldValue With(
            FieldStatus status,
            object? value,
            string? source,
            double? confidence)
        {
            return new FieldValue(
                Alias,
                status,
                value,
                source,
                confidence,
                DateTimeOffset.UtcNow);
        }
    }
}
