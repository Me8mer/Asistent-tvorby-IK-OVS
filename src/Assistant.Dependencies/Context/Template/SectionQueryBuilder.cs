using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Assistant.Core.Model;

namespace Assistant.Dependencies.Context
{
    public sealed class SectionQueryBuilder
    {
        public sealed class Options
        {
            public int MaximumFieldTerms { get; init; } = 20;
            public int MaximumTotalCharacters { get; init; } = 1200;

            public Options()
            {
            }
        }

        private readonly Options options;

        public SectionQueryBuilder(Options? options = null)
        {
            this.options = options ?? new Options();
        }

        public SectionQuery BuildSectionQuery(
            SectionAlias sectionAlias,
            InternalModel internalModel)
        {
            if (internalModel is null)
                throw new ArgumentNullException(nameof(internalModel));

            IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias = internalModel.SectionsByAlias;

            if (!sectionsByAlias.TryGetValue(sectionAlias, out SectionDescriptor? section))
                throw new ArgumentException($"Section alias '{sectionAlias}' is not present in the provided model.", nameof(sectionAlias));

            var terms = new List<string>(capacity: 64);
            var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddSectionPathTerms(section, sectionsByAlias, terms, seenTerms);

            AddHintTerms(section.QueryHints, terms, seenTerms);

            AddFieldTerms(section.FieldAliases, internalModel, terms, seenTerms);

            string queryText = BuildQueryTextWithLimit(terms, options.MaximumTotalCharacters);

            return new SectionQuery(
                sectionAlias: section.SectionAlias,
                queryText: queryText,
                terms: terms);
        }

        private void AddSectionPathTerms(
            SectionDescriptor section,
            IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias,
            List<string> terms,
            HashSet<string> seenTerms)
        {
            IReadOnlyList<SectionDescriptor> pathFromRoot = BuildPathFromRoot(section, sectionsByAlias);

            foreach (SectionDescriptor pathSection in pathFromRoot)
            {
                string displayOrDerived = GetDisplayOrDerivedName(pathSection.DisplayName, pathSection.SectionAlias.Value);
                AddTerm(displayOrDerived, terms, seenTerms);

                if (string.IsNullOrWhiteSpace(pathSection.DisplayName))
                {
                    string aliasDerived = DeriveTermsFromAlias(pathSection.SectionAlias.Value);
                    AddTerm(aliasDerived, terms, seenTerms);
                }
            }
        }

        private static IReadOnlyList<SectionDescriptor> BuildPathFromRoot(
            SectionDescriptor leafSection,
            IReadOnlyDictionary<SectionAlias, SectionDescriptor> sectionsByAlias)
        {
            var reversed = new List<SectionDescriptor>();
            var visitedAliases = new HashSet<SectionAlias>();

            SectionDescriptor? current = leafSection;

            while (current != null)
            {
                if (!visitedAliases.Add(current.SectionAlias))
                    break;

                reversed.Add(current);

                if (current.ParentSectionAlias == null)
                    break;

                if (!sectionsByAlias.TryGetValue(current.ParentSectionAlias.Value, out SectionDescriptor? parent))
                    break;

                current = parent;
            }

            reversed.Reverse();
            return reversed;
        }

        private void AddFieldTerms(
            IReadOnlyList<FieldAlias> fieldAliases,
            InternalModel internalModel,
            List<string> terms,
            HashSet<string> seenTerms)
        {
            int addedFieldTerms = 0;

            foreach (FieldAlias fieldAlias in fieldAliases)
            {
                if (addedFieldTerms >= options.MaximumFieldTerms)
                    break;

                int termCountBefore = terms.Count;

                if (internalModel.TryGetField(fieldAlias, out FieldNode fieldNode) &&
                    fieldNode.Descriptor is FieldDescriptor descriptor)
                {
                    AddLocalizedFieldTerms(descriptor, terms, seenTerms);
                }
                else
                {
                    AddTerm(DeriveTermsFromAlias(fieldAlias.Value), terms, seenTerms);
                }

                if (terms.Count > termCountBefore)
                    addedFieldTerms++;
            }
        }

        private static void AddLocalizedFieldTerms(
            FieldDescriptor descriptor,
            List<string> terms,
            HashSet<string> seenTerms)
        {
            AddTerm(GetDisplayOrDerivedName(descriptor.DisplayName, descriptor.Alias.Value), terms, seenTerms);

            AddHintTerms(descriptor.QueryHints, terms, seenTerms);

            if (string.IsNullOrWhiteSpace(descriptor.DisplayName))
            {
                AddTerm(DeriveTermsFromAlias(descriptor.Alias.Value), terms, seenTerms);
            }
        }

        private static void AddHintTerms(
            IReadOnlyList<string>? queryHints,
            List<string> terms,
            HashSet<string> seenTerms)
        {
            if (queryHints == null || queryHints.Count == 0)
                return;

            foreach (string hint in queryHints)
            {
                AddTerm(hint, terms, seenTerms);
            }
        }

        private static bool AddTerm(string? term, List<string> terms, HashSet<string> seenTerms)
        {
            string normalized = NormalizeTerm(term);

            if (string.IsNullOrWhiteSpace(normalized))
                return false;

            if (!seenTerms.Add(normalized))
                return false;

            terms.Add(normalized);
            return true;
        }

        private static string NormalizeTerm(string? term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return string.Empty;

            string trimmed = term.Trim();

            while (trimmed.Contains("  ", StringComparison.Ordinal))
                trimmed = trimmed.Replace("  ", " ", StringComparison.Ordinal);

            return trimmed;
        }

        private static string GetDisplayOrDerivedName(string? displayName, string aliasValue)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName;

            return DeriveTermsFromAlias(aliasValue);
        }

        private static string DeriveTermsFromAlias(string aliasValue)
        {
            if (string.IsNullOrWhiteSpace(aliasValue))
                return string.Empty;

            int colonIndex = aliasValue.IndexOf(':');
            string basePart = colonIndex == -1 ? aliasValue : aliasValue[..colonIndex];

            if (basePart.StartsWith("IKOVS_", StringComparison.Ordinal))
                basePart = basePart["IKOVS_".Length..];

            string withSpaces = basePart
                .Replace('_', ' ')
                .Replace('-', ' ');

            return withSpaces;
        }

        private static string BuildQueryTextWithLimit(IReadOnlyList<string> terms, int maximumCharacters)
        {
            var builder = new StringBuilder(capacity: Math.Min(maximumCharacters, 2048));

            foreach (string term in terms)
            {
                if (string.IsNullOrWhiteSpace(term))
                    continue;

                int requiredLength = builder.Length == 0
                    ? term.Length
                    : term.Length + 1;

                if (builder.Length + requiredLength > maximumCharacters)
                    break;

                if (builder.Length > 0)
                    builder.Append(' ');

                builder.Append(term);
            }

            return builder.ToString();
        }
    }

    public sealed class SectionQuery
    {
        public SectionAlias SectionAlias { get; }
        public string QueryText { get; }
        public IReadOnlyList<string> Terms { get; }

        public SectionQuery(SectionAlias sectionAlias, string queryText, IReadOnlyList<string> terms)
        {
            SectionAlias = sectionAlias;
            QueryText = queryText ?? string.Empty;
            Terms = terms ?? Array.Empty<string>();
        }
    }
}
