using System;
using System.IO;
using System.Reflection;

namespace Assistant.AI.Prompting
{
    internal static class SchemaLoader
    {
        public static string LoadSchemaText(string schemaFileName)
        {
            string baseDirectory = AppContext.BaseDirectory;
            string schemaPath = Path.Combine(baseDirectory, "Schemas", schemaFileName);

            if (!File.Exists(schemaPath))
            {
                throw new FileNotFoundException(
                    $"Schema file '{schemaFileName}' not found at '{schemaPath}'.");
            }

            return File.ReadAllText(schemaPath);
        }
    }
}
