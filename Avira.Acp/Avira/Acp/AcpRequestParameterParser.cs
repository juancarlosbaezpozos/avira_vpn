using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Avira.Acp
{
    public static class AcpRequestParameterParser
    {
        public static List<AcpRequestParameter> Parse(string parametersString)
        {
            List<AcpRequestParameter> list = new List<AcpRequestParameter>();
            if (string.IsNullOrEmpty(parametersString))
            {
                return list;
            }

            string[] array = parametersString.Split('&');
            for (int i = 0; i < array.Length; i++)
            {
                AcpRequestParameter acpRequestParameter = ParseParameter(array[i]);
                if (acpRequestParameter != null && acpRequestParameter.IsValid())
                {
                    list.Add(acpRequestParameter);
                }
            }

            return list;
        }

        private static AcpRequestParameter ParseParameter(string parameterString)
        {
            Match match = Regex.Match(parameterString, "(filter|page|fields)\\[([^\\]]+)\\](<=|>=|=|!=|<|>)(.+)");
            if (!match.Success && match.Groups.Count < 3)
            {
                return null;
            }

            return new AcpRequestParameter
            {
                Type = ParseType(match.Groups[1].Value),
                Property = match.Groups[2].Value,
                Operator = AcpRequestParameter.ParseOperator(match.Groups[3].Value),
                Value = match.Groups[4].Value
            };
        }

        private static AcpRequestParameter.ParameterType ParseType(string typeString)
        {
            return (AcpRequestParameter.ParameterType)Enum.Parse(typeof(AcpRequestParameter.ParameterType), typeString,
                ignoreCase: true);
        }
    }
}