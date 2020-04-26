using ChatCore.Models;
using ChatCore.SimpleJSON;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ChatCore.Config
{
    public class ObjectSerializer
    {
        private static readonly Regex _configRegex = new Regex(@"(?<Section>\[[a-zA-Z0-9\s]+\])|(?<Name>[^=\/\/#\s]+)\s*=[\t\p{Zs}]*(?<Value>"".+""|({(?:[^{}]|(?<Array>{)|(?<-Array>}))+(?(Array)(?!))})|\S+)?[\t\p{Zs}]*((\/{2,2}|[#])(?<Comment>.+)?)?", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly ConcurrentDictionary<Type, Func<FieldInfo, string, object>> ConvertFromString = new ConcurrentDictionary<Type, Func<FieldInfo, string, object>>();
        private static readonly ConcurrentDictionary<Type, Func<FieldInfo, object, string>> ConvertToString = new ConcurrentDictionary<Type, Func<FieldInfo, object, string>>();
        private readonly ConcurrentDictionary<string, string> _comments = new ConcurrentDictionary<string, string>();

        private static void InitTypeHandlers()
        {
            // String handlers
            ConvertFromString.TryAdd(typeof(string), (fieldInfo, value) => { return (value.StartsWith("\"") && value.EndsWith("\"") ? value.Substring(1, value.Length - 2) : value); });
            ConvertToString.TryAdd(typeof(string), (fieldInfo, obj) =>
            {
                string value = (string)obj.GetFieldValue(fieldInfo.Name);
                // If the value is an array, we don't need quotes
                if (value.StartsWith("{") && value.EndsWith("}"))
                    return value;
                return $"\"{value}\"";
            });

            // Bool handlers
            ConvertFromString.TryAdd(typeof(bool), (fieldInfo, value) => { return (value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || value.Equals("on", StringComparison.CurrentCultureIgnoreCase) || value.Equals("1")); });
            ConvertToString.TryAdd(typeof(bool), (fieldInfo, obj) => { return ((bool)obj.GetFieldValue(fieldInfo.Name)).ToString(); });

            // Enum handlers
            ConvertFromString.TryAdd(typeof(Enum), (fieldInfo, value) => { return Enum.Parse(fieldInfo.FieldType, value); });
            ConvertToString.TryAdd(typeof(Enum), (fieldInfo, obj) => { return obj.GetFieldValue(fieldInfo.Name).ToString(); });
        }

        private static bool CreateDynamicFieldConverter(FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;
            if (TryCreateFieldConverterFromParseFunction(fieldInfo))
            {
                return true;
            }

            if(fieldType.IsArray)
            {
                return false;
            }

            // If we got here, there's no nice convenient Parse function for this type... so try to convert each field individually.
            var fields = fieldInfo.FieldType.GetRuntimeFields();
            foreach (var field in fields)
            {
                if (!field.IsPrivate && !field.IsStatic && !TryCreateFieldConverterFromParseFunction(field))
                {
                    throw new Exception($"Unsupported type {fieldInfo.FieldType.Name} or one of the types it implements cannot be automatically converted by the ObjectSerializer!");
                }
            }

            ConvertFromString.TryAdd(fieldType, (fi, v) =>
            {
                object obj = Activator.CreateInstance(fi.FieldType);
                if (string.IsNullOrEmpty(v))
                {
                    return obj;
                }
                JSONNode json = JSON.Parse(v);
                foreach (var subFieldInfo in fi.FieldType.GetRuntimeFields())
                {
                    if (!subFieldInfo.IsPrivate && !subFieldInfo.IsStatic && json.HasKey(subFieldInfo.Name))
                    {
                        subFieldInfo.SetValue(obj, ConvertFromString[subFieldInfo.FieldType].Invoke(subFieldInfo, json[subFieldInfo.Name].Value));
                    }
                }
                return obj;
            });
            ConvertToString.TryAdd(fieldType, (fi, v) =>
            {
                JSONObject json = new JSONObject();

                // Grab the current field we're trying to convert off the parent object
                var currentField = v.GetFieldValue(fi.Name);

                foreach (var subFieldInfo in fi.FieldType.GetRuntimeFields())
                {
                    if (!subFieldInfo.IsPrivate && !subFieldInfo.IsStatic)
                    {
                        //var subFieldValue = currentField.GetField(subFieldInfo.Name);
                        string value = ConvertToString[subFieldInfo.FieldType].Invoke(subFieldInfo, currentField);
                        json.Add(subFieldInfo.Name, new JSONString(value));
                    }
                }
                return json.ToString();
            });
            return true;
        }

        private static bool TryCreateFieldConverterFromParseFunction(FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;
            if (ConvertFromString.ContainsKey(fieldType) && ConvertToString.ContainsKey(fieldType))
            {
                // Converters already exist for these types
                return true;
            }

            var functions = fieldType.GetRuntimeMethods();
            foreach (var func in functions)
            {
                switch (func.Name)
                {
                    case "Parse":
                        var parameters = func.GetParameters();
                        if (parameters.Count() != 1 || parameters[0].ParameterType != typeof(string))
                        {
                            // If the function doesn't have only a single parameter of type string, don't use this function as a field converter.
                            continue;
                        }

                        if (func.ReturnType != fieldType)
                        {
                            // If the function doesn't return the type of the field we're creating the converter for, don't use this function as a field converter.
                            continue;
                        }

                        ConvertFromString.TryAdd(fieldType, (fi, v) => { return func.Invoke(null, new object[] { v }); });
                        ConvertToString.TryAdd(fieldType, (fi, v) => { return v.GetFieldValue(fi.Name).ToString(); });

                        return true;
                }
            }
            return false;
        }
        
        public void Load(object obj, string path)
        {
            if (ConvertFromString.Count == 0)
                InitTypeHandlers();

            string backupPath = path + ".bak";
            if (File.Exists(backupPath) && !File.Exists(path))
            {
                File.Move(backupPath, path);
            }

            if (File.Exists(path))
            {
                var matches = _configRegex.Matches(File.ReadAllText(path));
                string currentSection = null;
                foreach (Match match in matches)
                {
                    if(match.Groups["Section"].Success)
                    {
                        currentSection = match.Groups["Section"].Value;
                        continue;
                    }

                    // Grab the name, which has to exist or the regex wouldn't have matched
                    var name = match.Groups["Name"].Value;

                    // Check if any comments existed
                    if (match.Groups["Comment"].Success)
                    {
                        // Then store them in memory so we can write them back later on
                        _comments[name] = match.Groups["Comment"].Value.TrimEnd(new char[] { '\n', '\r' });
                    }

                    // If there's no value, continue on at this point
                    if (!match.Groups["Value"].Success)
                        continue;
                    var value = match.Groups["Value"].Value;

                    // Otherwise, read the value in with the appropriate handler
                    var fieldInfo = obj.GetType().GetField(name.Replace(".","_"));

                    if(fieldInfo == null)
                    {
                        // Skip missing fields, incase one was changed or removed.
                        continue;
                    }

                    // If the fieldType is an enum, replace it with the generic Enum type
                    Type fieldType = fieldInfo.FieldType.IsEnum ? typeof(Enum) : fieldInfo.FieldType;

                    // Invoke our ConvertFromString method if it exists
                    if (!ConvertFromString.TryGetValue(fieldType, out var convertFromString))
                    {
                        if (CreateDynamicFieldConverter(fieldInfo))
                        {
                            convertFromString = ConvertFromString[fieldType];
                        }
                    }
                    try
                    {
                        object converted = convertFromString.Invoke(fieldInfo, value);
                        fieldInfo.SetValue(obj, converted);
                    }
                    catch (Exception ex)
                    {
                        //Plugin.Log($"Failed to parse field {name} with value {value} as type {fieldInfo.FieldType.Name}. {ex.ToString()}");
                    }
                }
            }
        }

        public void Save(object obj, string path)
        {
            if (ConvertToString.Count == 0)
                InitTypeHandlers();

            string backupPath = path + ".bak";
            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }
                File.Move(path, backupPath);
            }

            string lastConfigSection = null;
            List<string> serializedClass = new List<string>();

            var configHeader = (ConfigHeader)obj.GetType().GetCustomAttribute(typeof(ConfigHeader));
            if (configHeader != null)
            {
                foreach(string comment in configHeader.Comment)
                {
                    serializedClass.Add(string.IsNullOrWhiteSpace(comment) ? comment : $"// {comment}");
                }
            }

            foreach (var fieldInfo in obj.GetType().GetFields())
            {
                // If the fieldType is an enum, replace it with the generic Enum type
                Type fieldType = fieldInfo.FieldType.IsEnum ? typeof(Enum) : fieldInfo.FieldType;

                // Invoke our convertFromString method if it exists
                if (!ConvertToString.TryGetValue(fieldType, out var convertToString))
                {
                    if (CreateDynamicFieldConverter(fieldInfo))
                    {
                        convertToString = ConvertToString[fieldType];
                    }
                }

                var configSection = (ConfigSection)fieldInfo.GetCustomAttribute(typeof(ConfigSection));
                if (configSection != null && !string.IsNullOrEmpty(configSection.Name))
                {
                    if (lastConfigSection != null && configSection.Name != lastConfigSection)
                    {
                        serializedClass.Add("");
                    }
                    serializedClass.Add($"[{configSection.Name}]");
                    lastConfigSection = configSection.Name;
                }

                var configMeta = (ConfigMeta)fieldInfo.GetCustomAttribute(typeof(ConfigMeta));
                string valueStr = "";
                try
                {
                    string comment = null;
                    if (!_comments.TryGetValue(fieldInfo.Name, out comment))
                    {
                        // If the user hasn't entered any of their own comments, use the default one of it exists
                        if (configMeta != null && !string.IsNullOrEmpty(configMeta.Comment))
                        {
                            comment = configMeta.Comment;
                        }
                    }
                    valueStr = $"{convertToString.Invoke(fieldInfo, obj)}{(comment != null ? " //" + comment : "")}";
                }
                catch (Exception ex)
                {
                    //throw;
                    //Plugin.Log($"Failed to convert field {fieldInfo.Name} to string! Value type is {fieldInfo.FieldType.Name}. {ex.ToString()}");
                }
                serializedClass.Add($"{fieldInfo.Name.Replace("_", ".")}={valueStr}");
            }
            if (path != string.Empty && serializedClass.Count > 0)
            {
                string tmpPath = $"{path}.tmp";
                if (!Directory.Exists(Path.GetDirectoryName(path)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                }
                File.WriteAllLines(tmpPath, serializedClass.ToArray());
                if (File.Exists(path))
                    File.Delete(path);
                File.Move(tmpPath, path);
            }
        }

        /// <summary>
        /// Returns a dictionary containing HTML representing each config section.
        /// </summary>
        /// <param name="obj">The object to serialize into HTML</param>
        /// <returns></returns>
        public Dictionary<string, string> GetSettingsAsHTML(object obj)
        {
            if (ConvertToString.Count == 0)
                InitTypeHandlers();

            string lastConfigSection = null;
            Dictionary<string, string> sectionHtml = new Dictionary<string, string>();
            List<string> currentSectionHtml = new List<string>();
            string currentSectionName = "";
            //currentSectionHtml.Add("<div class=\"panel-body\">");

            // TODO: serialize comments as tooltips?
            //var configHeader = (ConfigHeader)obj.GetType().GetCustomAttribute(typeof(ConfigHeader));
            //if (configHeader != null)
            //{
            //    foreach (string comment in configHeader.Comment)
            //    {
            //        serializedClass.Add(string.IsNullOrWhiteSpace(comment) ? comment : $"// {comment}");
            //    }
            //}

            foreach (var fieldInfo in obj.GetType().GetFields())
            {
                // If the fieldType is an enum, replace it with the generic Enum type
                Type fieldType = fieldInfo.FieldType.IsEnum ? typeof(Enum) : fieldInfo.FieldType;

                // Invoke our convertFromString method if it exists
                if (!ConvertToString.TryGetValue(fieldType, out var convertToString))
                {
                    if (CreateDynamicFieldConverter(fieldInfo))
                    {
                        convertToString = ConvertToString[fieldType];
                    }
                }

                var configSection = (ConfigSection)fieldInfo.GetCustomAttribute(typeof(ConfigSection));
                if (configSection != null && !string.IsNullOrEmpty(configSection.Name))
                {
                    currentSectionName = configSection.Name;
                    if (lastConfigSection != null && currentSectionName != lastConfigSection)
                    {
                        // End the previous section and start a new one
                        //currentSectionHtml.Add("</div>");
                        sectionHtml[lastConfigSection] = string.Join(Environment.NewLine, currentSectionHtml);
                        currentSectionHtml = new List<string>();
                        //currentSectionHtml.Add("<div class=\"panel-body\">");
                    }
                    currentSectionHtml.Add($"<label class=\"form-label\">{currentSectionName.Uncamelcase()}</label>");
                    lastConfigSection = currentSectionName;
                }

                if (fieldInfo.GetCustomAttribute(typeof(HTMLIgnore)) != null)
                {
                    // Skip any fields with the HTMLIgnore attribute
                    continue;
                }

                var configMeta = (ConfigMeta)fieldInfo.GetCustomAttribute(typeof(ConfigMeta));
                string valueStr = "";
                try
                {
                    string comment = null;
                    if (!_comments.TryGetValue(fieldInfo.Name, out comment))
                    {
                        // If the user hasn't entered any of their own comments, use the default one of it exists
                        if (configMeta != null && !string.IsNullOrEmpty(configMeta.Comment))
                        {
                            comment = configMeta.Comment;
                        }
                    }

                    currentSectionHtml.Add(obj.GetFieldValue(fieldInfo.Name) switch
                    {
                        bool b => BuildSwitchHTML(fieldInfo.Name, b),
                        int i => BuildNumberHTML(fieldInfo.Name, i),
                        string s => BuildStringHTML(fieldInfo.Name, s),
                        _ => BuildUnknownHTML(fieldInfo.Name, $"{convertToString.Invoke(fieldInfo, obj)}{(comment != null ? " //" + comment : "")}"),
                    });
                }
                catch (Exception ex)
                {
                    //throw;
                    //Plugin.Log($"Failed to convert field {fieldInfo.Name} to string! Value type is {fieldInfo.FieldType.Name}. {ex.ToString()}");
                }
            }
            //currentSectionHtml.Add("</div>");
            sectionHtml[lastConfigSection] = string.Join(Environment.NewLine, currentSectionHtml);
            return sectionHtml;
        }

        /// <summary>
        /// Sets class values from a web post request
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="postData"></param>
        public void SetFromDictionary(object obj, Dictionary<string, string> postData)
        {
            if (ConvertFromString.Count == 0)
                InitTypeHandlers();

            foreach (KeyValuePair<string, string> kvp in postData)
            {
                // Otherwise, read the value in with the appropriate handler
                var fieldInfo = obj.GetType().GetField(kvp.Key.Replace(".", "_"));

                if (fieldInfo == null)
                {
                    // Skip missing fields, incase one was changed or removed.
                    continue;
                }

                // If the fieldType is an enum, replace it with the generic Enum type
                Type fieldType = fieldInfo.FieldType.IsEnum ? typeof(Enum) : fieldInfo.FieldType;

                // Invoke our ConvertFromString method if it exists
                if (!ConvertFromString.TryGetValue(fieldType, out var convertFromString))
                {
                    if (CreateDynamicFieldConverter(fieldInfo))
                    {
                        convertFromString = ConvertFromString[fieldType];
                    }
                }
                try
                {
                    object converted = convertFromString.Invoke(fieldInfo, kvp.Value);
                    fieldInfo.SetValue(obj, converted);
                }
                catch (Exception ex)
                {
                    //Plugin.Log($"Failed to parse field {name} with value {value} as type {fieldInfo.FieldType.Name}. {ex.ToString()}");
                }
            }
        }

        private string BuildSwitchHTML(string name, bool b)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<label class=\"form-switch\">" + Environment.NewLine);
            sb.Append($"\t<input type=\"hidden\" value=\"off\" name=\"{name}\">" + Environment.NewLine);
            sb.Append($"\t<input name=\"{name}\" type=\"checkbox\" {(b ? "checked" : "")}>" + Environment.NewLine);
            sb.Append($"\t<i class=\"form-icon\"></i> {name.Uncamelcase()}" + Environment.NewLine);
            sb.Append($"</label>");
            return sb.ToString();
        }

        private string BuildNumberHTML(string name, int i)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<label class=\"form-label\">" + Environment.NewLine);
            sb.Append($"\t<i class=\"form-icon\"></i> {name.Uncamelcase()}" + Environment.NewLine);
            sb.Append($"<input name=\"{name}\" class=\"form-input\" type=\"number\" placeholder=\"00\" value=\"{i.ToString()}\">" + Environment.NewLine);
            sb.Append($"</label>");
            return sb.ToString();
        }

        private string BuildStringHTML(string name, string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<label class=\"form-label\">" + Environment.NewLine);
            sb.Append($"\t<i class=\"form-icon\"></i> {name.Uncamelcase()}" + Environment.NewLine);
            sb.Append($"<input name=\"{name}\" class=\"form-input\" type=\"text\" placeholder=\"00\" value=\"{s}\">" + Environment.NewLine);
            sb.Append($"</label>");
            return sb.ToString();
        }

        private string BuildUnknownHTML(string name, string s)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"<label class=\"form-label\">" + Environment.NewLine);
            sb.Append($"\t<i class=\"form-icon\"></i> {name.Uncamelcase()}" + Environment.NewLine);
            sb.Append($"<textarea name=\"name\" class=\"form-input\" placeholder=\"...\" rows=\"3\">{s}</textarea>" + Environment.NewLine);
            sb.Append($"</label>");
            return sb.ToString();
        }
    }
}
