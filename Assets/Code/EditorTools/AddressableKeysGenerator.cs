#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEngine;

namespace Code.EditorTools
{
    public static class AddressableKeysGenerator
    {
        private const string OutputFolder = "Assets/Code/Generated/AddressablesGroups";
        private const string NamespaceName = "Code.Generated.Addressables";
        private const string MainClassName = "ResourceIdsContainer";
        
        [MenuItem("Tools/Addressables/Print All Addresses")]
        public static void PrintAddresses()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var group in settings.groups)
            {
                foreach (var entry in group.entries)
                {
                    Debug.Log($"Group: {group.Name} → Address: {entry.address}");
                }
            }
        }

        [MenuItem("Tools/Addressables/Generate Addressable Groups (CamelCase)")]
        public static void Generate()
        {
            CleanOldFiles();

            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings == null)
            {
                Debug.LogError("AddressableAssetSettings not found.");
                return;
            }

            if (!Directory.Exists(OutputFolder))
            {
                Directory.CreateDirectory(OutputFolder);
            }

            var groupClassNames = new List<string>();

            foreach (var group in settings.groups)
            {
                if (group == null || group.entries == null)
                {
                    continue;
                }

                var entries = group.entries
                    .Where(e => e != null && !string.IsNullOrEmpty(e.address))
                    .Distinct()
                    .ToList();

                var className = ToCamelCase(group.Name, true);
                var filePath = Path.Combine(OutputFolder, $"{className}.cs");

                // Build hierarchical structure based on "/" in addresses
                var rootNode = BuildHierarchy(entries);

                var sb = new StringBuilder();
                sb.AppendLine("using System;");
                sb.AppendLine();
                sb.AppendLine($"namespace {NamespaceName}");
                sb.AppendLine("{");
                sb.AppendLine($"    public class {className}");
                sb.AppendLine("    {");

                // Generate nested classes and fields
                GenerateNodeContent(sb, rootNode, 2);

                sb.AppendLine("    }");
                sb.AppendLine("}");

                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                Debug.Log($"Generated {className}.cs for group '{group.Name}' at {filePath}");

                groupClassNames.Add(className);
            }

            GenerateMainContainer(groupClassNames);
            AssetDatabase.Refresh();
        }

        // Helper class to represent hierarchical structure
        private class AddressNode
        {
            public string Name { get; set; }
            public string FullAddress { get; set; }
            public Dictionary<string, AddressNode> Children { get; set; } = new Dictionary<string, AddressNode>();
            public List<string> DirectAddresses { get; set; } = new List<string>();
        }

        private static AddressNode BuildHierarchy(List<UnityEditor.AddressableAssets.Settings.AddressableAssetEntry> entries)
        {
            var root = new AddressNode { Name = "Root" };

            foreach (var entry in entries)
            {
                var address = entry.address;
                var parts = address.Split('/');
                
                var currentNode = root;
                var currentPath = "";

                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var part = parts[i];
                    currentPath = i == 0 ? part : currentPath + "/" + part;
                    
                    if (!currentNode.Children.ContainsKey(part))
                    {
                        currentNode.Children[part] = new AddressNode 
                        { 
                            Name = part,
                            FullAddress = currentPath
                        };
                    }
                    
                    currentNode = currentNode.Children[part];
                }

                // Add the final part as a direct address
                currentNode.DirectAddresses.Add(address);
            }

            return root;
        }

        private static void GenerateNodeContent(StringBuilder sb, AddressNode node, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            var isRootLevel = indentLevel == 2; // Root level for group class
            
            // First, generate all direct addresses as fields
            foreach (var address in node.DirectAddresses)
            {
                var fieldName = GetFieldNameFromAddress(address);
                sb.AppendLine($"{indent}public string {fieldName} = \"{address}\";");
            }

            // Add empty line between fields and nested classes if both exist
            if (node.DirectAddresses.Count > 0 && node.Children.Count > 0)
            {
                sb.AppendLine();
            }

            // Then, generate nested classes for each child
            var generatedClasses = new List<string>();
            foreach (var child in node.Children)
            {
                var className = ToCamelCase(child.Key, true);
                generatedClasses.Add(className);
                
                // Generate the nested class
                sb.AppendLine($"{indent}public class {className}");
                sb.AppendLine($"{indent}{{");
                
                GenerateNodeContent(sb, child.Value, indentLevel + 1);
                
                sb.AppendLine($"{indent}}}");
                sb.AppendLine();
            }
            
            // Generate instance fields for nested classes at root level
            if (isRootLevel && generatedClasses.Count > 0)
            {
                foreach (var className in generatedClasses)
                {
                    sb.AppendLine($"{indent}public readonly {className} {className} = new {className}();");
                }
            }
        }

        private static string GetFieldNameFromAddress(string address)
        {
            // Get the last part after the last "/"
            var lastSlashIndex = address.LastIndexOf('/');
            var fieldPart = lastSlashIndex >= 0 ? address.Substring(lastSlashIndex + 1) : address;
            
            return ToCamelCaseIdentifier(fieldPart);
        }

        private static void GenerateMainContainer(List<string> groupClassNames)
        {
            var filePath = Path.Combine(OutputFolder, $"{MainClassName}.cs");

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine($"using {NamespaceName};");
            sb.AppendLine();
            sb.AppendLine($"namespace {NamespaceName}");
            sb.AppendLine("{");
            sb.AppendLine($"    public static class {MainClassName}");
            sb.AppendLine("    {");

            foreach (var className in groupClassNames)
            {
                // Каждый класс группы становится readonly полем
                sb.AppendLine($"        public static readonly {className} {className} = new {className}();");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
            Debug.Log($"Generated main container {MainClassName}.cs with {groupClassNames.Count} groups at {filePath}");
        }

        private static void CleanOldFiles()
        {
            if (Directory.Exists(OutputFolder))
            {
                var existingFiles = Directory.GetFiles(OutputFolder, "*.cs", SearchOption.TopDirectoryOnly);
                foreach (var f in existingFiles)
                {
                    File.Delete(f);
                }
            }
        }

        private static string ToCamelCaseIdentifier(string address)
        {
            var cleaned = ReplaceDelimitersWithSpace(address);

            cleaned = EnsureStartsWithLetter(cleaned);

            return JoinPartsToCamelCase(cleaned);
        }

        private static string ToCamelCase(string input, bool isGroupName = false)
        {
            var cleaned = ReplaceDelimitersWithSpace(input);

            cleaned = EnsureStartsWithLetter(cleaned);

            return JoinPartsToCamelCase(cleaned);
        }

        private static string ReplaceDelimitersWithSpace(string input)
        {
            // All delimiters including brackets are replaced with spaces for valid C# identifiers
            char[] delimiters = { ' ', '_', '-', '.', '/', '\\', '(', ')', '[', ']', '{', '}' };
            var sb = new StringBuilder(input.Length);
            foreach (var c in input)
            {
                if (delimiters.Contains(c))
                {
                    sb.Append(' ');
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Trim();
        }

        private static string EnsureStartsWithLetter(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return "Id";
            }

            var parts = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            if (parts.Count == 0)
            {
                return "Id";
            }

            if (!char.IsLetter(parts[0], 0))
            {
                // Добавим новую часть "Id" в начало
                parts.Insert(0, "Id");
            }

            return string.Join(" ", parts);
        }

        private static string JoinPartsToCamelCase(string input)
        {
            var parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = CapitalizeFirstLetter(parts[i]);
            }

            return string.Join(string.Empty, parts);
        }

        private static string CapitalizeFirstLetter(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return s;
            }

            if (s.Length == 1)
            {
                return s.ToUpperInvariant();
            }

            return char.ToUpperInvariant(s[0]) + s.Substring(1);
        }
    }
}
#endif