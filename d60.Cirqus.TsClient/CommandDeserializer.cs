using System;
using d60.Cirqus.Commands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace d60.Cirqus.TsClient
{
    /// <summary>
    /// Helps with deserialization of commands that have been created from TsClient proxies.
    /// </summary>
    public class CommandDeserializer
    {
        public Command GetCommand(string stringifiedCommand)
        {
            var commandJObject = GetJObject(stringifiedCommand);
            
            var typeProperty = GetTypeProperty(stringifiedCommand, commandJObject);
            
            var commandType = GetCommandType(typeProperty);
            
            return FinalDeserialize(stringifiedCommand, commandJObject, commandType);
        }

        static Type GetCommandType(JToken typeProperty)
        {
            var commandTypeName = typeProperty.ToString();
            var commandType = Type.GetType(commandTypeName);

            if (commandType == null)
            {
                throw new ArgumentException(string.Format("Could not load type from '$type' value '{0}'", commandTypeName));
            }
            return commandType;
        }

        static JToken GetTypeProperty(string stringifiedCommand, JObject commandJObject)
        {
            var typeProperty = commandJObject["$type"];
            if (typeProperty == null)
            {
                throw new ArgumentException(string.Format("Could not find the '$type' property on the JSON text: {0}",
                    stringifiedCommand));
            }
            return typeProperty;
        }

        static Command FinalDeserialize(string stringifiedCommand, JObject commandJObject, Type commandType)
        {
            try
            {
                var command = commandJObject.ToObject(commandType);

                return (Command) command;
            }
            catch (Exception exception)
            {
                throw new FormatException(
                    string.Format("An error occurred when attempting to do final deserialization of '{0}' into {1}",
                        stringifiedCommand, commandType), exception);
            }
        }

        static JObject GetJObject(string stringifiedCommand)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(stringifiedCommand);
            }
            catch (Exception exception)
            {
                throw new FormatException(string.Format("Could not parse '{0}' into a valid JSON object", stringifiedCommand), exception);
            }
        }
    }
}