using System.Collections.Generic;
using System.Linq;

namespace Avira.Acp
{
    public class AcpRequestParameter
    {
        public enum ParameterType
        {
            Filter,
            Page,
            Fields
        }

        public enum OperatorType
        {
            Unknown,
            Equals,
            NotEquals,
            GreaterThan,
            LessThan,
            GreaterEquals,
            LessEquals
        }

        private static readonly Dictionary<OperatorType, string> OperatorMap = new Dictionary<OperatorType, string>
        {
            {
                OperatorType.Equals,
                "="
            },
            {
                OperatorType.NotEquals,
                "!="
            },
            {
                OperatorType.GreaterThan,
                ">"
            },
            {
                OperatorType.LessThan,
                "<"
            },
            {
                OperatorType.GreaterEquals,
                ">="
            },
            {
                OperatorType.LessEquals,
                "<="
            }
        };

        public ParameterType Type { get; set; }

        public string Property { get; set; }

        public string Value { get; set; }

        public OperatorType Operator { get; set; }

        public static OperatorType ParseOperator(string operatorString)
        {
            return OperatorMap.FirstOrDefault((KeyValuePair<OperatorType, string> o) => o.Value == operatorString).Key;
        }

        public static string GetOperatorString(OperatorType @operator)
        {
            return OperatorMap[@operator];
        }

        public bool IsValid()
        {
            if (Type == ParameterType.Fields && Operator != OperatorType.Equals)
            {
                return false;
            }

            return true;
        }
    }
}