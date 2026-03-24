using System;

namespace Assistant.Core.Model
{
    /// <summary>
    /// Defines known context sources that can be used to retrieve supplemental
    /// information for a field or section. Sources correspond to entries in
    /// the docs/sources.md document and may include multiple categories such
    /// as registries, portals or office websites. The names of the enum
    /// values match the aliases specified in the definitions YAML files.
    ///
    /// New sources can be added here without breaking existing consumers.
    /// </summary>
    public enum ContextSource
    {
        /// <summary>
        /// The default office website associated with the current entity. This
        /// source uses the office identifier provided at runtime and is always
        /// included if no specific sources are configured on a field.
        /// </summary>
        WebOffice,

        /// <summary>
        /// The public Registry of Rights and Duties (Registr práv a povinností).
        /// Contains agendas, services and competency information about public
        /// offices.  The base URL for this source is https://rpp-portal.gov.cz.
        /// </summary>
        Rpp,

        /// <summary>
        /// The Archi Portal – a knowledge base and repository of eGovernment
        /// architecture assets hosted at https://archi.gov.cz.
        /// </summary>
        ArchiPortal,

        /// <summary>
        /// The national information concept repository (IKCR) located at
        /// https://digitalnicesko.gov.cz.  This source provides strategic
        /// frameworks and methodological guidance.
        /// </summary>
        Ikcr,

        /// <summary>
        /// A placeholder for future template-based context retrieval.  When
        /// specified on a field, context may be derived from the document
        /// template itself.  Currently this source is a stub and does not
        /// contribute any context.
        /// </summary>
        Template
    }
}