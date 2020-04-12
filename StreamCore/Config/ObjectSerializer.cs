using StreamCore.Models;
using StreamCore.SimpleJSON;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace StreamCore.Config
{
    public class ObjectSerializer
    {
        private static readonly Regex _configRegex = new Regex(@"(?<Name>[^=\/\/#\s]+)\s*=[\t\p{Zs}]*(?<Value>"".+""|({(?:[^{}]|(?<Array>{)|(?<-Array>}))+(?(Array)(?!))})|\S+)?[\t\p{Zs}]*((\/{2,2}|[#])(?<Comment>.+)?)?", RegexOptions.Compiled | RegexOptions.Multiline);
        private static readonly ConcurrentDictionary<Type, Func<FieldInfo, string, object>> ConvertFromString = new ConcurrentDictionary<Type, Func<FieldInfo, string, object>>();
        private static readonly ConcurrentDictionary<Type, Func<FieldInfo, object, string>> ConvertToString = new ConcurrentDictionary<Type, Func<FieldInfo, object, string>>();
        private readonly ConcurrentDictionary<string, string> _comments = new ConcurrentDictionary<string, string>();
        private object _obj;
        public ObjectSerializer(object obj)
        {
            _obj = obj;
        }
        private static void InitTypeHandlers()
        {
            // String handlers
            ConvertFromString.TryAdd(typeof(string), (fieldInfo, value) => { return (value.StartsWith("\"") && value.EndsWith("\"") ? value.Substring(1, value.Length - 2) : value); });
            ConvertToString.TryAdd(typeof(string), (fieldInfo, obj) =>
            {
                string value = (string)obj.GetField(fieldInfo.Name);
                // If the value is an array, we don't need quotes
                if (value.StartsWith("{") && value.EndsWith("}"))
                    return value;
                return $"\"{value}\"";
            });

            // Bool handlers
            ConvertFromString.TryAdd(typeof(bool), (fieldInfo, value) => { return (value.Equals("true", StringComparison.CurrentCultureIgnoreCase) || value.Equals("1")); });
            ConvertToString.TryAdd(typeof(bool), (fieldInfo, obj) => { return ((bool)obj.GetField(fieldInfo.Name)).ToString(); });

            // Enum handlers
            ConvertFromString.TryAdd(typeof(Enum), (fieldInfo, value) => { return Enum.Parse(fieldInfo.FieldType, value); });
            ConvertToString.TryAdd(typeof(Enum), (fieldInfo, obj) => { return obj.GetField(fieldInfo.Name).ToString(); });
        }

        private static bool CreateDynamicFieldConverter(FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;
            if (TryCreateFieldConverterFromParseFunction(fieldInfo))
            {
                return true;
            }

            // If we got here, there's no nice convenient Parse function for this type... so try to convert each field individually.
            var fields = fieldInfo.FieldType.GetRuntimeFields();
            foreach (var field in fields)
            {
                if (!field.IsPrivate && !TryCreateFieldConverterFromParseFunction(field))
                {
                    throw new Exception($"Unsupported type {fieldInfo.FieldType.Name} or one of the types it implements cannot be automatically converted by the ObjectSerializer!");
                }
            }

            ConvertFromString.TryAdd(fieldType, (fi, v) =>
            {
                try
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
                }
                catch (Exception ex)
                {
                    throw;
                }
            });
            ConvertToString.TryAdd(fieldType, (fi, v) =>
            {
                try
                {
                    JSONObject json = new JSONObject();

                    // Grab the current field we're trying to convert off the parent object
                    var currentField = v.GetField(fi.Name);

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
                }
                catch (Exception ex)
                {
                    throw;
                }
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
            var functions = fieldInfo.FieldType.GetRuntimeMethods();
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
                        ConvertToString.TryAdd(fieldType, (fi, v) => { return v.GetField(fi.Name).ToString(); });
                        return true;
                }
            }
            return false;
        }

        public void Load(string path)
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
                foreach (Match match in matches)
                {
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
                    var fieldInfo = _obj.GetType().GetField(name);

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
                        fieldInfo.SetValue(_obj, converted);
                    }
                    catch (Exception ex)
                    {
                        //Plugin.Log($"Failed to parse field {name} with value {value} as type {fieldInfo.FieldType.Name}. {ex.ToString()}");
                    }
                }
            }
        }

        public void Save(string path)
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
            foreach (var fieldInfo in _obj.GetType().GetFields())
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
                    valueStr = $"{convertToString.Invoke(fieldInfo, _obj)}{(comment != null ? " //" + comment : "")}";
                }
                catch (Exception ex)
                {
                    //Plugin.Log($"Failed to convert field {fieldInfo.Name} to string! Value type is {fieldInfo.FieldType.Name}. {ex.ToString()}");
                }
                serializedClass.Add($"{fieldInfo.Name}={valueStr}");
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
    }
}
